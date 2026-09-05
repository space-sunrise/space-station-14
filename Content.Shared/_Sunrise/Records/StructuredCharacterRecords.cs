using System.Text;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Records;

// Sunrise added start — структурированные досье персонажа (мед/охрана/трудовая)
// Портировано и адаптировано из corvax-team/ss14-wl#792: убраны конфедерации/регионы
// (в Sunrise нет такой концепции), фиксированный лорный календарь и WL-специфичные
// каталоги специальностей. Данные хранятся в тех же текстовых полях профиля/досье,
// что и раньше (MedicalRecord/SecurityRecord/EmploymentRecord), поэтому изменения
// формата не требуют миграции базы данных или новых DataField.

public enum RecordMaritalStatus
{
    NotApplicable,
    Single,
    Married,
    Widowed,
}

public enum RecordAcademicDegree
{
    NotApplicable,
    Qualificate,
    Bachelor,
    Master,
    Candidate,
    Doctor,
}

public enum RecordAcademicTitle
{
    NotApplicable,
    Assistant,
    AssociateProfessor,
    Professor,
}

public sealed class MedicalRecordData
{
    public string PostmortemInstructions { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public string PhysiologicalNotes { get; set; } = string.Empty;
    public string PsychologicalNotes { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string LastUpdated { get; set; } = string.Empty;
}

public sealed class ResidenceRecordData
{
    public string Region { get; set; } = string.Empty;
    public string Planet { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}

public sealed class SecurityRecordData
{
    public string Residence { get; set; } = string.Empty;
    public string IdentifyingFeatures { get; set; } = string.Empty;
    public RecordMaritalStatus MaritalStatus { get; set; }
    public string CloseRelatives { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public bool UnderSecuritySupervision { get; set; }
    public string ArrestHistory { get; set; } = string.Empty;
    public string ImprisonmentHistory { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string LastUpdated { get; set; } = string.Empty;
}

public sealed class EducationRecordData
{
    public string Specialty { get; set; } = string.Empty;
    public RecordAcademicDegree Degree { get; set; }
    public string Institution { get; set; } = string.Empty;
    public string DiplomaDate { get; set; } = string.Empty;
}

public sealed class EmploymentRecordData
{
    public List<EducationRecordData> Education { get; set; } = new();
    public RecordAcademicTitle AcademicTitle { get; set; }
    public string AcademicTitleField { get; set; } = string.Empty;
    public string AcademicTitleDate { get; set; } = string.Empty;
    public string Licenses { get; set; } = string.Empty;
    public string EmploymentHistory { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string LastUpdated { get; set; } = string.Empty;
}

/// <summary>
/// Хранит фиксированные формы досье персонажа в тех же текстовых колонках, что и раньше.
/// Старый свободный текст (написанный до внедрения структурированных досье) сохраняется
/// в соответствующем поле "Заметки" и не теряется.
/// </summary>
public static class StructuredCharacterRecords
{
    public const int MaxEducationEntries = 8;
    public const int MaxShortTextLength = 256;
    public const int MaxLongTextLength = 2048;
    public const int MaxNotesTextLength = 4096;

    private const string MedicalV1Prefix = "SUNRISE_MEDICAL_V1:";
    private const string MedicalV2Prefix = "SUNRISE_MEDICAL_V2:";
    // V3 — вес больше не хранится как свободный текст, он всегда берётся из показателей
    // телосложения в редакторе персонажа (см. Content.Shared._Sunrise.Humanoid.HumanoidBodyMetrics)
    private const string MedicalV3Prefix = "SUNRISE_MEDICAL_V3:";
    private const string SecurityV1Prefix = "SUNRISE_SECURITY_V1:";
    // V2 — поле "Разрешения" убрано: игроки писали в нём произвольный текст вида
    // "могу носить контрабанду", выдавая себя за официально уполномоченных
    private const string SecurityV2Prefix = "SUNRISE_SECURITY_V2:";
    private const string EmploymentV1Prefix = "SUNRISE_EMPLOYMENT_V1:";
    // V2 — добавлено поле LastUpdated: раньше WriteEmployment его не сохранял вообще,
    // поэтому после сохранения профиля дата всегда терялась и заменялась на "нет данных"
    private const string EmploymentV2Prefix = "SUNRISE_EMPLOYMENT_V2:";
    private const string ResidencePrefix = "SUNRISE_ADDRESS_V1:";

    public static MedicalRecordData ReadMedical(string? storage)
    {
        if (TryReadFields(storage, MedicalV3Prefix, 6, out var fieldsV3))
        {
            return new MedicalRecordData
            {
                PostmortemInstructions = ClampShort(fieldsV3[0]),
                EmergencyContact = ClampShort(fieldsV3[1]),
                PhysiologicalNotes = ClampLong(fieldsV3[2]),
                PsychologicalNotes = ClampLong(fieldsV3[3]),
                Notes = ClampNotes(fieldsV3[4]),
                LastUpdated = ClampShort(fieldsV3[5]),
            };
        }

        if (TryReadFields(storage, MedicalV2Prefix, 7, out var fields))
        {
            return new MedicalRecordData
            {
                PostmortemInstructions = ClampShort(fields[1]),
                EmergencyContact = ClampShort(fields[2]),
                PhysiologicalNotes = ClampLong(fields[3]),
                PsychologicalNotes = ClampLong(fields[4]),
                Notes = ClampNotes(fields[5]),
                LastUpdated = ClampShort(fields[6]),
            };
        }

        if (TryReadFields(storage, MedicalV1Prefix, 11, out fields))
        {
            return new MedicalRecordData
            {
                PostmortemInstructions = ClampShort(fields[1]),
                EmergencyContact = ClampShort(fields[4]),
                PhysiologicalNotes = ClampLong(fields[7]),
                PsychologicalNotes = ClampLong(fields[8]),
                Notes = ClampNotes(fields[9]),
                LastUpdated = ClampShort(fields[10]),
            };
        }

        return new MedicalRecordData { Notes = ClampNotes(storage) };
    }

    public static string WriteMedical(MedicalRecordData record)
    {
        return WriteFields(MedicalV3Prefix, new[]
        {
            ClampShort(record.PostmortemInstructions),
            ClampShort(record.EmergencyContact),
            ClampLong(record.PhysiologicalNotes),
            ClampLong(record.PsychologicalNotes),
            ClampNotes(record.Notes),
            ClampShort(record.LastUpdated),
        });
    }

    public static SecurityRecordData ReadSecurity(string? storage)
    {
        if (TryReadFields(storage, SecurityV2Prefix, 10, out var fieldsV2))
        {
            return new SecurityRecordData
            {
                Residence = ClampSerialized(fieldsV2[0], MaxLongTextLength),
                IdentifyingFeatures = ClampShort(fieldsV2[1]),
                MaritalStatus = ReadEnum<RecordMaritalStatus>(fieldsV2[2]),
                CloseRelatives = ClampShort(fieldsV2[3]),
                EmergencyContact = ClampShort(fieldsV2[4]),
                UnderSecuritySupervision = ReadBool(fieldsV2[5]),
                ArrestHistory = ClampLong(fieldsV2[6]),
                ImprisonmentHistory = ClampLong(fieldsV2[7]),
                Notes = ClampNotes(fieldsV2[8]),
                LastUpdated = ClampShort(fieldsV2[9]),
            };
        }

        if (!TryReadFields(storage, SecurityV1Prefix, 11, out var fields))
            return new SecurityRecordData { Notes = ClampNotes(storage) };

        return new SecurityRecordData
        {
            Residence = ClampSerialized(fields[0], MaxLongTextLength),
            IdentifyingFeatures = ClampShort(fields[1]),
            MaritalStatus = ReadEnum<RecordMaritalStatus>(fields[2]),
            CloseRelatives = ClampShort(fields[3]),
            EmergencyContact = ClampShort(fields[4]),
            // fields[5] — устаревшее поле "Разрешения", отбрасывается
            UnderSecuritySupervision = ReadBool(fields[6]),
            ArrestHistory = ClampLong(fields[7]),
            ImprisonmentHistory = ClampLong(fields[8]),
            Notes = ClampNotes(fields[9]),
            LastUpdated = ClampShort(fields[10]),
        };
    }

    public static string WriteSecurity(SecurityRecordData record)
    {
        return WriteFields(SecurityV2Prefix, new[]
        {
            ClampSerialized(record.Residence, MaxLongTextLength),
            ClampShort(record.IdentifyingFeatures),
            record.MaritalStatus.ToString(),
            ClampShort(record.CloseRelatives),
            ClampShort(record.EmergencyContact),
            WriteBool(record.UnderSecuritySupervision),
            ClampLong(record.ArrestHistory),
            ClampLong(record.ImprisonmentHistory),
            ClampNotes(record.Notes),
            ClampShort(record.LastUpdated),
        });
    }

    public static EmploymentRecordData ReadEmployment(string? storage)
    {
        const int educationFieldCount = 4;
        const int baseFieldCountV2 = 8;

        if (TryReadFields(storage, EmploymentV2Prefix, null, out var fieldsV2) &&
            fieldsV2.Count >= baseFieldCountV2 &&
            int.TryParse(fieldsV2[0], out var educationCountV2) &&
            educationCountV2 is >= 0 and <= MaxEducationEntries &&
            fieldsV2.Count == baseFieldCountV2 + educationCountV2 * educationFieldCount)
        {
            var recordV2 = new EmploymentRecordData
            {
                AcademicTitle = ReadEnum<RecordAcademicTitle>(fieldsV2[1]),
                AcademicTitleField = ClampShort(fieldsV2[2]),
                AcademicTitleDate = ClampShort(fieldsV2[3]),
                Licenses = ClampLong(fieldsV2[4]),
                EmploymentHistory = ClampLong(fieldsV2[5]),
                Notes = ClampNotes(fieldsV2[6]),
                LastUpdated = ClampShort(fieldsV2[7]),
            };

            for (var i = 0; i < educationCountV2; i++)
            {
                var offset = baseFieldCountV2 + i * educationFieldCount;
                recordV2.Education.Add(new EducationRecordData
                {
                    Specialty = ClampShort(fieldsV2[offset]),
                    Degree = ReadEnum<RecordAcademicDegree>(fieldsV2[offset + 1]),
                    Institution = ClampShort(fieldsV2[offset + 2]),
                    DiplomaDate = ClampShort(fieldsV2[offset + 3]),
                });
            }

            return recordV2;
        }

        const int baseFieldCount = 7;

        if (!TryReadFields(storage, EmploymentV1Prefix, null, out var fields) ||
            fields.Count < baseFieldCount ||
            !int.TryParse(fields[0], out var educationCount) ||
            educationCount is < 0 or > MaxEducationEntries ||
            fields.Count != baseFieldCount + educationCount * educationFieldCount)
        {
            return new EmploymentRecordData { Notes = ClampNotes(storage) };
        }

        var record = new EmploymentRecordData
        {
            AcademicTitle = ReadEnum<RecordAcademicTitle>(fields[1]),
            AcademicTitleField = ClampShort(fields[2]),
            AcademicTitleDate = ClampShort(fields[3]),
            Licenses = ClampLong(fields[4]),
            EmploymentHistory = ClampLong(fields[5]),
            Notes = ClampNotes(fields[6]),
            LastUpdated = string.Empty,
        };

        for (var i = 0; i < educationCount; i++)
        {
            var offset = baseFieldCount + i * educationFieldCount;
            record.Education.Add(new EducationRecordData
            {
                Specialty = ClampShort(fields[offset]),
                Degree = ReadEnum<RecordAcademicDegree>(fields[offset + 1]),
                Institution = ClampShort(fields[offset + 2]),
                DiplomaDate = ClampShort(fields[offset + 3]),
            });
        }

        return record;
    }

    public static string WriteEmployment(EmploymentRecordData record)
    {
        var educationCount = Math.Min(record.Education.Count, MaxEducationEntries);
        var fields = new List<string>(8 + educationCount * 4)
        {
            educationCount.ToString(),
            record.AcademicTitle.ToString(),
            ClampShort(record.AcademicTitleField),
            ClampShort(record.AcademicTitleDate),
            ClampLong(record.Licenses),
            ClampLong(record.EmploymentHistory),
            ClampNotes(record.Notes),
            ClampShort(record.LastUpdated),
        };

        for (var i = 0; i < educationCount; i++)
        {
            var education = record.Education[i];
            fields.Add(ClampShort(education.Specialty));
            fields.Add(education.Degree.ToString());
            fields.Add(ClampShort(education.Institution));
            fields.Add(ClampShort(education.DiplomaDate));
        }

        return WriteFields(EmploymentV2Prefix, fields);
    }

    public static ResidenceRecordData ReadResidence(string? storage)
    {
        if (!TryReadFields(storage, ResidencePrefix, 5, out var fields))
            return new ResidenceRecordData { Details = ClampShort(storage) };

        return new ResidenceRecordData
        {
            Region = ClampShort(fields[0]),
            Planet = ClampShort(fields[1]),
            Street = ClampShort(fields[2]),
            Unit = ClampShort(fields[3]),
            Details = ClampShort(fields[4]),
        };
    }

    public static string WriteResidence(ResidenceRecordData residence)
    {
        if (string.IsNullOrWhiteSpace(residence.Region) &&
            string.IsNullOrWhiteSpace(residence.Planet) &&
            string.IsNullOrWhiteSpace(residence.Street) &&
            string.IsNullOrWhiteSpace(residence.Unit) &&
            string.IsNullOrWhiteSpace(residence.Details))
        {
            return string.Empty;
        }

        return WriteFields(ResidencePrefix, new[]
        {
            ClampShort(residence.Region),
            ClampShort(residence.Planet),
            ClampShort(residence.Street),
            ClampShort(residence.Unit),
            ClampShort(residence.Details),
        });
    }

    public static string NormalizeMedical(string? storage) => WriteMedical(ReadMedical(storage));
    public static string NormalizeSecurity(string? storage) => WriteSecurity(ReadSecurity(storage));
    public static string NormalizeEmployment(string? storage) => WriteEmployment(ReadEmployment(storage));

    private static string WriteFields(string prefix, IReadOnlyList<string> fields)
    {
        var builder = new StringBuilder(prefix);
        builder.Append(fields.Count).Append(';');
        foreach (var field in fields)
            builder.Append(field.Length).Append(':').Append(field);

        return builder.ToString();
    }

    private static bool TryReadFields(string? storage, string prefix, int? expectedCount, out List<string> fields)
    {
        fields = new List<string>();
        if (string.IsNullOrEmpty(storage) || !storage.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var position = prefix.Length;
        if (!TryReadNumber(storage, ref position, ';', out var count) ||
            count < 0 ||
            expectedCount != null && count != expectedCount)
        {
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            if (!TryReadNumber(storage, ref position, ':', out var length) ||
                length < 0 ||
                length > storage.Length - position)
            {
                return false;
            }

            fields.Add(storage.Substring(position, length));
            position += length;
        }

        return position == storage.Length;
    }

    private static bool TryReadNumber(string value, ref int position, char delimiter, out int number)
    {
        number = 0;
        var hasDigit = false;
        while (position < value.Length && value[position] != delimiter)
        {
            var character = value[position++];
            if (character is < '0' or > '9')
                return false;

            var digit = character - '0';
            if (number > (int.MaxValue - digit) / 10)
                return false;

            number = number * 10 + digit;
            hasDigit = true;
        }

        if (!hasDigit || position >= value.Length)
            return false;

        position++;
        return true;
    }

    private static T ReadEnum<T>(string value) where T : struct, Enum
    {
        return Enum.TryParse<T>(value, out var result) && Enum.IsDefined(result) ? result : default;
    }

    private static bool ReadBool(string value) => value == "1";
    private static string WriteBool(bool value) => value ? "1" : "0";

    public static string NormalizeShortText(string? value) => Clamp(value, MaxShortTextLength);

    private static string ClampShort(string? value) => NormalizeShortText(value);
    private static string ClampLong(string? value) => Clamp(value, MaxLongTextLength);
    private static string ClampNotes(string? value) => Clamp(value, MaxNotesTextLength);

    private static string ClampSerialized(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string Clamp(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = FormattedMessage.RemoveMarkupPermissive(value).Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
// Sunrise added end
