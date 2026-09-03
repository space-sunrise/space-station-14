using System.Collections.Generic;

namespace Content.Client._Sunrise.Records;

public enum RecordViewKind
{
    Medical,
    Security,
    Employment,
}

public enum RecordValueSource
{
    Automatic,
    Author,
    Placeholder,
}

public sealed record RecordViewField(
    string Label,
    string Value,
    RecordValueSource Source,
    bool LongValue = false,
    bool Warning = false);

public sealed record RecordViewSection(string Title, IReadOnlyList<RecordViewField> Fields);

public sealed record RecordIdentityData(
    string Name,
    string Job,
    string Fingerprint,
    string Dna,
    string FullName,
    string DateLabel,
    string Date,
    string Gender,
    string Species,
    string Height,
    string Weight,
    string Restrictions);

public sealed record RecordViewData(
    RecordViewKind Kind,
    RecordIdentityData Identity,
    IReadOnlyList<RecordViewSection> Sections);
