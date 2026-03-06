using Hl7ToCcda.Core.Exceptions;
using Hl7ToCcda.Core.Fhir;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Hl7ToCcda.Core.Ccda;

internal sealed class CcdaDocumentBuilder
{
    public CcdaDocument Build(
        JObject bundle,
        Hl7ToCcdaConversionRequest request,
        IList<ConversionWarning> warnings)
    {
        var navigator = new FhirBundleNavigator(bundle);
        JObject? patientResource = navigator.First("Patient");

        if (patientResource is null)
        {
            throw new InsufficientClinicalContentException("The intermediate FHIR bundle did not contain a Patient resource.");
        }

        var patient = BuildPatient(patientResource);
        var organization = BuildOrganization(navigator.First("Organization"), warnings);
        var author = BuildAuthor(navigator.First("Practitioner"), organization, warnings);
        var encounter = BuildEncounter(navigator.First("Encounter"));
        var sections = BuildSections(navigator);

        if (sections.Count == 0)
        {
            throw new InsufficientClinicalContentException("The intermediate FHIR bundle did not contain any supported clinical sections.");
        }

        return new CcdaDocument(
            IdRoot: CreateDocumentId(request, patient),
            Title: request.DocumentTitle?.Trim() is { Length: > 0 } title ? title : "Continuity of Care Document",
            EffectiveTime: request.EffectiveTime ?? DateTimeOffset.UtcNow,
            Patient: patient,
            Author: author,
            Custodian: organization,
            Encounter: encounter,
            Sections: sections);
    }

    private static CcdaPatient BuildPatient(JObject patientResource)
    {
        return new CcdaPatient(
            Identifier: FhirText.FirstIdentifier(patientResource) ?? "UNKNOWN",
            Name: FhirText.HumanName(patientResource["name"]) ?? "Unknown Patient",
            GenderCode: FhirText.GenderToAdministrativeCode(patientResource),
            BirthDate: FhirText.CdaBirthDate(patientResource),
            Address: FhirText.Address(patientResource["address"]),
            Telecom: FhirText.Telecom(patientResource["telecom"]));
    }

    private static CcdaOrganization? BuildOrganization(JObject? organizationResource, IList<ConversionWarning> warnings)
    {
        if (organizationResource is null)
        {
            warnings.Add(new ConversionWarning("MissingCustodian", "No Organization resource was available for the CCD custodian."));
            return null;
        }

        return new CcdaOrganization(
            Identifier: FhirText.FirstIdentifier(organizationResource) ?? "UNKNOWN",
            Name: FhirText.FirstMeaningful(
                organizationResource["name"]?.Value<string>(),
                organizationResource["id"]?.Value<string>()) ?? "Unknown Organization");
    }

    private static CcdaAuthor? BuildAuthor(JObject? practitionerResource, CcdaOrganization? organization, IList<ConversionWarning> warnings)
    {
        if (practitionerResource is null)
        {
            warnings.Add(new ConversionWarning("MissingAuthor", "No Practitioner resource was available for the CCD author."));
            return null;
        }

        return new CcdaAuthor(
            Identifier: FhirText.FirstIdentifier(practitionerResource) ?? "UNKNOWN",
            Name: FhirText.HumanName(practitionerResource["name"]) ?? "Unknown Practitioner",
            Organization: organization);
    }

    private static CcdaEncounterSummary? BuildEncounter(JObject? encounterResource)
    {
        if (encounterResource is null)
        {
            return null;
        }

        return new CcdaEncounterSummary(
            Display: FhirText.FirstMeaningful(
                FhirText.CodeableConcept(encounterResource["type"]?.FirstOrDefault()),
                encounterResource["class"]?["display"]?.Value<string>(),
                encounterResource["status"]?.Value<string>()),
            Period: FhirText.Period(encounterResource["period"]));
    }

    private static List<CcdaSection> BuildSections(FhirBundleNavigator navigator)
    {
        List<CcdaSection> sections = [];

        AddSectionIfNotEmpty(sections, "11450-4", "Problem List", "Problems", navigator.GetResources("Condition")
            .Select(condition => FhirText.JoinNonEmpty([
                FhirText.CodeableConcept(condition["code"]),
                condition["clinicalStatus"]?["coding"]?.FirstOrDefault()?["code"]?.Value<string>(),
                FhirText.DateOrDateTime(condition["onsetDateTime"]),
            ])));

        AddSectionIfNotEmpty(sections, "48765-2", "Allergies and adverse reactions", "Allergies", navigator.GetResources("AllergyIntolerance")
            .Select(allergy => FhirText.JoinNonEmpty([
                FhirText.CodeableConcept(allergy["code"]),
                allergy["criticality"]?.Value<string>(),
                allergy["clinicalStatus"]?["coding"]?.FirstOrDefault()?["code"]?.Value<string>(),
            ])));

        IEnumerable<string> medicationItems = navigator.GetResources("MedicationRequest")
            .Select(request => FhirText.JoinNonEmpty([
                FhirText.CodeableConcept(request["medicationCodeableConcept"]),
                request["status"]?.Value<string>(),
                FhirText.DateOrDateTime(request["authoredOn"]),
            ]))
            .Concat(navigator.GetResources("MedicationStatement")
                .Select(statement => FhirText.JoinNonEmpty([
                    FhirText.CodeableConcept(statement["medicationCodeableConcept"]),
                    statement["status"]?.Value<string>(),
                    FhirText.Period(statement["effectivePeriod"]),
                ])));

        AddSectionIfNotEmpty(sections, "10160-0", "History of Medication Use", "Medications", medicationItems);

        IEnumerable<string> resultItems = navigator.GetResources("DiagnosticReport")
            .Select(report => FhirText.JoinNonEmpty([
                FhirText.CodeableConcept(report["code"]),
                report["conclusion"]?.Value<string>(),
                FhirText.DateOrDateTime(report["effectiveDateTime"]),
            ]))
            .Concat(navigator.GetResources("Observation")
                .Select(observation => FhirText.JoinNonEmpty([
                    FhirText.CodeableConcept(observation["code"]),
                    DescribeObservationValue(observation),
                    FhirText.DateOrDateTime(observation["effectiveDateTime"]),
                ])));

        AddSectionIfNotEmpty(sections, "30954-2", "Relevant diagnostic tests and/or laboratory data", "Results", resultItems);

        AddSectionIfNotEmpty(sections, "47519-4", "History of Procedures", "Procedures", navigator.GetResources("Procedure")
            .Select(procedure => FhirText.JoinNonEmpty([
                FhirText.CodeableConcept(procedure["code"]),
                procedure["status"]?.Value<string>(),
                FhirText.DateOrDateTime(procedure["performedDateTime"]) ?? FhirText.Period(procedure["performedPeriod"]),
            ]))
            .Concat(navigator.GetResources("ServiceRequest")
                .Select(request => FhirText.JoinNonEmpty([
                    FhirText.CodeableConcept(request["code"]),
                    request["status"]?.Value<string>(),
                    FhirText.DateOrDateTime(request["occurrenceDateTime"]) ?? FhirText.Period(request["occurrencePeriod"]),
                ]))));

        AddSectionIfNotEmpty(sections, "11369-6", "History of immunizations", "Immunizations", navigator.GetResources("Immunization")
            .Select(immunization => FhirText.JoinNonEmpty([
                FhirText.CodeableConcept(immunization["vaccineCode"]),
                immunization["status"]?.Value<string>(),
                FhirText.DateOrDateTime(immunization["occurrenceDateTime"]),
            ])));

        AddSectionIfNotEmpty(sections, "46240-8", "History of encounters", "Encounters", navigator.GetResources("Encounter")
            .Select(encounter => FhirText.JoinNonEmpty([
                FhirText.CodeableConcept(encounter["type"]?.FirstOrDefault()),
                encounter["class"]?["display"]?.Value<string>(),
                FhirText.Period(encounter["period"]),
            ]))
            .Concat(navigator.GetResources("Appointment")
                .Select(appointment => FhirText.JoinNonEmpty([
                    appointment["description"]?.Value<string>(),
                    appointment["status"]?.Value<string>(),
                    FhirText.DateOrDateTime(appointment["start"]),
                ]))));

        return sections;
    }

    private static string? DescribeObservationValue(JObject observation)
    {
        return FhirText.FirstMeaningful(
            FhirText.Quantity(observation["valueQuantity"]),
            observation["valueString"]?.Value<string>(),
            FhirText.CodeableConcept(observation["valueCodeableConcept"]),
            observation["valueBoolean"]?.Value<bool?>() is bool booleanValue ? booleanValue.ToString() : null,
            observation["valueInteger"]?.Value<int?>()?.ToString(),
            observation["valueDecimal"]?.Value<decimal?>()?.ToString());
    }

    private static void AddSectionIfNotEmpty(
        ICollection<CcdaSection> sections,
        string code,
        string displayName,
        string title,
        IEnumerable<string> items)
    {
        List<string> normalizedItems = items
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalizedItems.Count == 0)
        {
            return;
        }

        sections.Add(new CcdaSection(code, displayName, title, normalizedItems));
    }

    private static string CreateDocumentId(Hl7ToCcdaConversionRequest request, CcdaPatient patient)
    {
        string seed = $"{patient.Identifier}|{request.DocumentTitle}|{request.EffectiveTime?.ToUniversalTime():O}";
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes(seed));
        return new Guid(hash).ToString().ToUpperInvariant();
    }
}
