using Hl7ToCcda.Core.Fhir;
using System.Xml;

namespace Hl7ToCcda.Core.Ccda;

internal sealed class CcdaXmlWriter
{
    private const string CdaNamespace = "urn:hl7-org:v3";
    private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

    public string Write(CcdaDocument document)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            OmitXmlDeclaration = false,
        };

        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);

        xmlWriter.WriteStartDocument();
        xmlWriter.WriteStartElement("ClinicalDocument", CdaNamespace);
        xmlWriter.WriteAttributeString("xmlns", "xsi", null, XsiNamespace);

        xmlWriter.WriteStartElement("realmCode", CdaNamespace);
        xmlWriter.WriteAttributeString("code", "US");
        xmlWriter.WriteEndElement();

        WriteElementWithAttributes(xmlWriter, "typeId", [("root", "2.16.840.1.113883.1.3"), ("extension", "POCD_HD000040")]);
        WriteElementWithAttributes(xmlWriter, "templateId", [("root", "2.16.840.1.113883.10.20.22.1.2"), ("extension", "2015-08-01")]);
        WriteElementWithAttributes(xmlWriter, "id", [("root", document.IdRoot)]);
        WriteElementWithAttributes(xmlWriter, "code", [
            ("code", "34133-9"),
            ("codeSystem", "2.16.840.1.113883.6.1"),
            ("displayName", "Summarization of episode note"),
        ]);
        WriteTextElement(xmlWriter, "title", document.Title);
        WriteElementWithAttributes(xmlWriter, "effectiveTime", [("value", FhirText.CdaTimestamp(document.EffectiveTime))]);
        WriteElementWithAttributes(xmlWriter, "confidentialityCode", [("code", "N"), ("codeSystem", "2.16.840.1.113883.5.25")]);
        WriteElementWithAttributes(xmlWriter, "languageCode", [("code", "en-US")]);

        WriteRecordTarget(xmlWriter, document.Patient);
        WriteAuthor(xmlWriter, document.Author, document.EffectiveTime);
        WriteCustodian(xmlWriter, document.Custodian);
        WriteDocumentationOf(xmlWriter, document.Encounter);
        WriteStructuredBody(xmlWriter, document.Sections);

        xmlWriter.WriteEndElement();
        xmlWriter.WriteEndDocument();
        xmlWriter.Flush();

        return stringWriter.ToString();
    }

    private static void WriteRecordTarget(XmlWriter writer, CcdaPatient patient)
    {
        writer.WriteStartElement("recordTarget", CdaNamespace);
        writer.WriteStartElement("patientRole", CdaNamespace);
        WriteElementWithAttributes(writer, "id", [("root", "2.16.840.1.113883.19.5"), ("extension", patient.Identifier)]);

        if (!string.IsNullOrWhiteSpace(patient.Address))
        {
            WriteTextElement(writer, "addr", patient.Address);
        }

        if (!string.IsNullOrWhiteSpace(patient.Telecom))
        {
            WriteElementWithAttributes(writer, "telecom", [("value", patient.Telecom)]);
        }

        writer.WriteStartElement("patient", CdaNamespace);
        WriteTextElement(writer, "name", patient.Name);

        if (!string.IsNullOrWhiteSpace(patient.GenderCode))
        {
            WriteElementWithAttributes(writer, "administrativeGenderCode", [("code", patient.GenderCode!)]);
        }

        if (!string.IsNullOrWhiteSpace(patient.BirthDate))
        {
            WriteElementWithAttributes(writer, "birthTime", [("value", patient.BirthDate!)]);
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteAuthor(XmlWriter writer, CcdaAuthor? author, DateTimeOffset effectiveTime)
    {
        if (author is null)
        {
            return;
        }

        writer.WriteStartElement("author", CdaNamespace);
        WriteElementWithAttributes(writer, "time", [("value", FhirText.CdaTimestamp(effectiveTime))]);
        writer.WriteStartElement("assignedAuthor", CdaNamespace);
        WriteElementWithAttributes(writer, "id", [("root", "2.16.840.1.113883.4.6"), ("extension", author.Identifier)]);
        writer.WriteStartElement("assignedPerson", CdaNamespace);
        WriteTextElement(writer, "name", author.Name);
        writer.WriteEndElement();

        if (author.Organization is not null)
        {
            writer.WriteStartElement("representedOrganization", CdaNamespace);
            WriteElementWithAttributes(writer, "id", [("root", "2.16.840.1.113883.19.5"), ("extension", author.Organization.Identifier)]);
            WriteTextElement(writer, "name", author.Organization.Name);
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteCustodian(XmlWriter writer, CcdaOrganization? organization)
    {
        if (organization is null)
        {
            return;
        }

        writer.WriteStartElement("custodian", CdaNamespace);
        writer.WriteStartElement("assignedCustodian", CdaNamespace);
        writer.WriteStartElement("representedCustodianOrganization", CdaNamespace);
        WriteElementWithAttributes(writer, "id", [("root", "2.16.840.1.113883.19.5"), ("extension", organization.Identifier)]);
        WriteTextElement(writer, "name", organization.Name);
        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteDocumentationOf(XmlWriter writer, CcdaEncounterSummary? encounter)
    {
        if (encounter is null)
        {
            return;
        }

        writer.WriteStartElement("documentationOf", CdaNamespace);
        writer.WriteStartElement("serviceEvent", CdaNamespace);
        WriteElementWithAttributes(writer, "code", [("displayName", encounter.Display ?? "Encounter")]);

        if (!string.IsNullOrWhiteSpace(encounter.Period))
        {
            WriteElementWithAttributes(writer, "effectiveTime", [("value", encounter.Period!)]);
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteStructuredBody(XmlWriter writer, IEnumerable<CcdaSection> sections)
    {
        writer.WriteStartElement("component", CdaNamespace);
        writer.WriteStartElement("structuredBody", CdaNamespace);

        foreach (CcdaSection section in sections)
        {
            writer.WriteStartElement("component", CdaNamespace);
            writer.WriteStartElement("section", CdaNamespace);
            WriteElementWithAttributes(writer, "code", [
                ("code", section.Code),
                ("codeSystem", "2.16.840.1.113883.6.1"),
                ("displayName", section.DisplayName),
            ]);
            WriteTextElement(writer, "title", section.Title);

            writer.WriteStartElement("text", CdaNamespace);
            writer.WriteStartElement("list", CdaNamespace);

            foreach (string item in section.Items)
            {
                WriteTextElement(writer, "item", item);
            }

            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
    }

    private static void WriteTextElement(XmlWriter writer, string elementName, string value)
    {
        writer.WriteStartElement(elementName, CdaNamespace);
        writer.WriteString(value);
        writer.WriteEndElement();
    }

    private static void WriteElementWithAttributes(XmlWriter writer, string elementName, IEnumerable<(string Name, string Value)> attributes)
    {
        writer.WriteStartElement(elementName, CdaNamespace);

        foreach ((string name, string value) in attributes)
        {
            writer.WriteAttributeString(name, value);
        }

        writer.WriteEndElement();
    }
}
