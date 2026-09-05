using System;
using System.Text;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Records;

/// <summary>
/// Собирает читаемый текст для печати структурированных досье на бумаге.
/// Локализация передаётся через делегат, чтобы форматирование можно было тестировать без IoC.
/// </summary>
public static class StructuredRecordFormatter
{
    public static string FormatMedical(string? storage, Func<string, string> loc, string heightText, string weightText, string restrictionsText)
    {
        var record = StructuredCharacterRecords.ReadMedical(storage);
        var builder = new StringBuilder();

        AppendLine(builder, "records-height-label", heightText, loc);
        AppendLine(builder, "records-weight", weightText, loc);
        AppendLine(builder, "records-medical-restrictions", restrictionsText, loc);
        AppendLine(builder, "records-postmortem", record.PostmortemInstructions, loc);
        AppendLine(builder, "records-emergency-contact", record.EmergencyContact, loc);

        AppendBlock(builder, "records-physiological-notes", record.PhysiologicalNotes, loc);
        AppendBlock(builder, "records-psychological-notes", record.PsychologicalNotes, loc);
        AppendBlock(builder, "records-notes", record.Notes, loc);
        AppendLine(builder, "records-last-updated", record.LastUpdated, loc);

        return builder.ToString().TrimEnd();
    }

    public static string FormatSecurity(string? storage, Func<string, string> loc, string heightText, string weightText)
    {
        var record = StructuredCharacterRecords.ReadSecurity(storage);
        var residence = StructuredCharacterRecords.ReadResidence(record.Residence);
        var builder = new StringBuilder();

        AppendLine(builder, "records-height-label", heightText, loc);
        AppendLine(builder, "records-weight", weightText, loc);
        AppendLine(builder, "records-residence-region", residence.Region, loc);
        AppendLine(builder, "records-residence-planet", residence.Planet, loc);
        AppendLine(builder, "records-residence-street", residence.Street, loc);
        AppendLine(builder, "records-residence-unit", residence.Unit, loc);
        AppendLine(builder, "records-residence-details", residence.Details, loc);
        AppendLine(builder, "records-identifying-features", record.IdentifyingFeatures, loc);
        AppendLine(builder, "records-marital-status", loc($"records-marital-{record.MaritalStatus.ToString().ToLowerInvariant()}"), loc);
        AppendLine(builder, "records-close-relatives", record.CloseRelatives, loc);
        AppendLine(builder, "records-emergency-contact", record.EmergencyContact, loc);
        AppendLine(builder, "records-security-supervision-short", YesNo(record.UnderSecuritySupervision, loc), loc);
        AppendBlock(builder, "records-arrest-history", record.ArrestHistory, loc);
        AppendBlock(builder, "records-imprisonment-history", record.ImprisonmentHistory, loc);
        AppendBlock(builder, "records-notes", record.Notes, loc);
        AppendLine(builder, "records-last-updated", record.LastUpdated, loc);

        return builder.ToString().TrimEnd();
    }

    public static string FormatEmployment(string? storage, Func<string, string> loc, string heightText, string weightText)
    {
        var record = StructuredCharacterRecords.ReadEmployment(storage);
        var builder = new StringBuilder();

        AppendLine(builder, "records-height-label", heightText, loc);
        AppendLine(builder, "records-weight", weightText, loc);

        if (record.Education.Count == 0)
        {
            AppendLine(builder, "records-education", string.Empty, loc);
        }
        else
        {
            for (var i = 0; i < record.Education.Count; i++)
            {
                var education = record.Education[i];
                builder.Append("[bold]").Append(Escape(loc("records-education"))).Append(' ').Append(i + 1).AppendLine("[/bold]");
                AppendLine(builder, "records-specialty", education.Specialty, loc);
                AppendLine(builder, "records-degree", loc($"records-degree-{education.Degree.ToString().ToLowerInvariant()}"), loc);
                AppendLine(builder, "records-institution", education.Institution, loc);
                AppendLine(builder, "records-diploma-date", education.DiplomaDate, loc);
            }
        }

        AppendLine(builder, "records-academic-title-short", loc($"records-academic-title-value-{record.AcademicTitle.ToString().ToLowerInvariant()}"), loc);
        if (record.AcademicTitle != RecordAcademicTitle.NotApplicable)
        {
            AppendLine(builder, "records-academic-title-field", record.AcademicTitleField, loc);
            AppendLine(builder, "records-academic-title-date", record.AcademicTitleDate, loc);
        }

        AppendBlock(builder, "records-licenses", record.Licenses, loc);
        AppendBlock(builder, "records-employment-history", record.EmploymentHistory, loc);
        AppendBlock(builder, "records-notes", record.Notes, loc);
        AppendLine(builder, "records-last-updated", record.LastUpdated, loc);

        return builder.ToString().TrimEnd();
    }

    private static string YesNo(bool value, Func<string, string> loc) => loc(value ? "records-value-yes" : "records-value-no");

    private static void AppendLine(StringBuilder builder, string labelKey, string value, Func<string, string> loc)
    {
        var text = string.IsNullOrWhiteSpace(value) ? loc("records-value-no-data") : value.Trim();
        builder.Append("[bold]").Append(Escape(loc(labelKey))).Append("[/bold] ").AppendLine(Escape(text));
    }

    private static void AppendBlock(StringBuilder builder, string labelKey, string value, Func<string, string> loc)
    {
        builder.AppendLine();
        builder.Append("[bold]").Append(Escape(loc(labelKey))).AppendLine("[/bold]");
        var text = string.IsNullOrWhiteSpace(value) ? loc("records-value-no-data") : value.Trim();
        builder.AppendLine(Escape(text));
    }

    private static string Escape(string value) => FormattedMessage.EscapeText(value);
}

