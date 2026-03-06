namespace Hl7ToCcda.Core.Ccda;

internal sealed record CcdaDocument(
    string IdRoot,
    string Title,
    DateTimeOffset EffectiveTime,
    CcdaPatient Patient,
    CcdaAuthor? Author,
    CcdaOrganization? Custodian,
    CcdaEncounterSummary? Encounter,
    IReadOnlyList<CcdaSection> Sections);

internal sealed record CcdaPatient(
    string Identifier,
    string Name,
    string? GenderCode,
    string? BirthDate,
    string? Address,
    string? Telecom);

internal sealed record CcdaAuthor(
    string Identifier,
    string Name,
    CcdaOrganization? Organization);

internal sealed record CcdaOrganization(
    string Identifier,
    string Name);

internal sealed record CcdaEncounterSummary(
    string? Display,
    string? Period);

internal sealed record CcdaSection(
    string Code,
    string DisplayName,
    string Title,
    IReadOnlyList<string> Items);
