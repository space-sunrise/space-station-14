using System.Collections.Generic;
using Content.Shared._Sunrise.Records;
using Robust.Shared.Localization;

namespace Content.Client._Sunrise.Records;

public static class RecordViewBuilder
{
    public static RecordViewData Medical(RecordIdentityData identity, string storage, ILocalizationManager loc)
    {
        var record = StructuredCharacterRecords.ReadMedical(storage);
        var sections = new List<RecordViewSection>
        {
            IdentitySection(identity, loc),
            new(loc.GetString("records-view-medical-directions"),
            [
                Author("records-postmortem", record.PostmortemInstructions, "records-value-not-applicable", loc, true,
                    !string.IsNullOrWhiteSpace(record.PostmortemInstructions)),
                Author("records-emergency-contact", record.EmergencyContact, "records-value-not-applicable", loc),
            ]),
        };

        sections.Add(AutomaticLongSection("records-medical-restrictions", identity.Restrictions, "records-medical-restrictions-none", loc));
        sections.Add(LongSection("records-physiological-notes", record.PhysiologicalNotes, "records-value-no-data", loc));
        sections.Add(LongSection("records-psychological-notes", record.PsychologicalNotes, "records-value-no-data", loc));
        sections.Add(LongSection("records-notes", record.Notes, "records-value-no-data", loc));
        sections.Add(ServiceSection(record.LastUpdated, loc));
        return new RecordViewData(RecordViewKind.Medical, identity, sections);
    }

    public static RecordViewData Security(RecordIdentityData identity, string storage, ILocalizationManager loc)
    {
        var record = StructuredCharacterRecords.ReadSecurity(storage);
        var sections = new List<RecordViewSection>
        {
            IdentitySection(identity, loc),
            new(loc.GetString("records-view-personal-details"),
            [
                Author("records-residence-registration", ResidenceValue(record.Residence, loc), "records-value-no-data", loc, true),
                Author("records-identifying-features", record.IdentifyingFeatures, "records-value-no-data", loc, true),
                AuthorValue("records-marital-status", loc.GetString($"records-marital-{record.MaritalStatus.ToString().ToLowerInvariant()}"), loc),
                Author("records-close-relatives", record.CloseRelatives, "records-value-no-data", loc),
                Author("records-emergency-contact", record.EmergencyContact, "records-value-no-data", loc),
            ]),
            new(loc.GetString("records-view-security-status"),
            [
                AuthorValue("records-security-supervision-short",
                    loc.GetString(record.UnderSecuritySupervision ? "records-value-yes" : "records-value-no"), loc,
                    record.UnderSecuritySupervision),
            ]),
            LongSection("records-arrest-history", record.ArrestHistory, "records-value-no-data", loc,
                !string.IsNullOrWhiteSpace(record.ArrestHistory)),
            LongSection("records-imprisonment-history", record.ImprisonmentHistory, "records-value-no-data", loc,
                !string.IsNullOrWhiteSpace(record.ImprisonmentHistory)),
            LongSection("records-notes", record.Notes, "records-value-no-data", loc),
            ServiceSection(record.LastUpdated, loc),
        };
        return new RecordViewData(RecordViewKind.Security, identity, sections);
    }

    public static RecordViewData Employment(RecordIdentityData identity, string storage, ILocalizationManager loc)
    {
        var record = StructuredCharacterRecords.ReadEmployment(storage);
        var sections = new List<RecordViewSection> { IdentitySection(identity, loc) };

        if (record.Education.Count == 0)
        {
            sections.Add(new RecordViewSection(loc.GetString("records-education"),
                [Placeholder("records-education", "records-value-no-data", loc)]));
        }
        else
        {
            for (var i = 0; i < record.Education.Count; i++)
            {
                var education = record.Education[i];
                sections.Add(new RecordViewSection(
                    $"{loc.GetString("records-education")} {i + 1}",
                    [
                        Author("records-specialty", education.Specialty, "records-value-no-data", loc),
                        AuthorValue("records-degree", loc.GetString($"records-degree-{education.Degree.ToString().ToLowerInvariant()}"), loc),
                        Author("records-institution", education.Institution, "records-value-no-data", loc),
                        Author("records-diploma-date", education.DiplomaDate, "records-value-no-data", loc),
                    ]));
            }
        }

        var academicTitleFields = new List<RecordViewField>
        {
            AuthorValue("records-academic-title-short", loc.GetString($"records-academic-title-value-{record.AcademicTitle.ToString().ToLowerInvariant()}"), loc),
        };
        if (record.AcademicTitle != RecordAcademicTitle.NotApplicable)
        {
            academicTitleFields.Add(Author("records-academic-title-field", record.AcademicTitleField, "records-value-not-applicable", loc));
            academicTitleFields.Add(Author("records-academic-title-date", record.AcademicTitleDate, "records-value-no-data", loc));
        }

        sections.AddRange(
        [
            new RecordViewSection(loc.GetString("records-academic-title-section"), academicTitleFields),
            LongSection("records-licenses", record.Licenses, "records-value-no-data", loc),
            LongSection("records-employment-history", record.EmploymentHistory, "records-value-no-data", loc),
            LongSection("records-notes", record.Notes, "records-value-no-data", loc),
            ServiceSection(record.LastUpdated, loc),
        ]);
        return new RecordViewData(RecordViewKind.Employment, identity, sections);
    }

    private static RecordViewSection IdentitySection(RecordIdentityData identity, ILocalizationManager loc)
    {
        var noData = loc.GetString("records-value-no-data");
        return new RecordViewSection(loc.GetString("records-view-basic-details"),
        [
            Automatic("records-full-name-edit", identity.FullName, noData, loc),
            new RecordViewField(identity.DateLabel, Value(identity.Date, noData), RecordValueSource.Automatic),
            Automatic("records-gender", identity.Gender, noData, loc),
            Automatic("records-species", identity.Species, noData, loc),
            Automatic("records-height-label", identity.Height, noData, loc),
            Automatic("records-weight", identity.Weight, noData, loc),
        ]);
    }

    private static RecordViewSection ServiceSection(string lastUpdated, ILocalizationManager loc) =>
        new(loc.GetString("records-view-service-details"),
            [Author("records-last-updated", lastUpdated, "records-value-no-data", loc)]);

    private static RecordViewSection LongSection(string key, string value, string fallback, ILocalizationManager loc, bool warning = false) =>
        new(loc.GetString(key), [Author(key, value, fallback, loc, true, warning)]);

    private static RecordViewSection AutomaticLongSection(string key, string value, string fallback, ILocalizationManager loc) =>
        new(loc.GetString(key), [Automatic(key, value, fallback, loc, true)]);

    private static RecordViewField Automatic(string key, string value, string fallback, ILocalizationManager loc, bool longValue = false) =>
        new(loc.GetString(key), Value(value, fallback), RecordValueSource.Automatic, longValue);

    private static RecordViewField Author(string key, string value, string fallbackKey, ILocalizationManager loc,
        bool longValue = false, bool warning = false) => string.IsNullOrWhiteSpace(value)
        ? Placeholder(key, fallbackKey, loc)
        : new RecordViewField(loc.GetString(key), value.Trim(), RecordValueSource.Author, longValue, warning);

    private static RecordViewField AuthorValue(string key, string value, ILocalizationManager loc, bool warning = false) =>
        new(loc.GetString(key), value, RecordValueSource.Author, false, warning);

    private static RecordViewField Placeholder(string key, string fallbackKey, ILocalizationManager loc) =>
        new(loc.GetString(key), loc.GetString(fallbackKey), RecordValueSource.Placeholder);

    private static string Value(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string ResidenceValue(string storage, ILocalizationManager loc)
    {
        var residence = StructuredCharacterRecords.ReadResidence(storage);
        var lines = new List<string>();
        AddResidenceLine(lines, loc.GetString("records-residence-region"), residence.Region);
        AddResidenceLine(lines, loc.GetString("records-residence-planet"), residence.Planet);
        AddResidenceLine(lines, loc.GetString("records-residence-street"), residence.Street);
        AddResidenceLine(lines, loc.GetString("records-residence-unit"), residence.Unit);
        AddResidenceLine(lines, loc.GetString("records-residence-details"), residence.Details);
        return string.Join('\n', lines);
    }

    private static void AddResidenceLine(List<string> lines, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            lines.Add($"{label} {value.Trim()}");
    }
}
