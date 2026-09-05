using Content.Shared._Sunrise.Humanoid;
using Content.Shared._Sunrise.Records;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.StationRecords;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Records;

public static class RecordIdentityBuilder
{
    /// <summary>
    /// Строит идентификационные данные для предпросмотра прямо во время создания персонажа,
    /// до того как появится итоговая станционная запись.
    /// </summary>
    public static RecordIdentityData FromProfile(HumanoidCharacterProfile profile, IPrototypeManager prototypes, ILocalizationManager loc)
    {
        var noData = loc.GetString("records-value-no-data");
        var species = prototypes.TryIndex<SpeciesPrototype>(profile.Species, out var speciesPrototype)
            ? loc.GetString(speciesPrototype.Name)
            : noData;
        var isManufactured = profile.Species.Id is "Ipc" or "Android";
        // Sunrise-Edit: раньше было "||" — при заполнении только дня ИЛИ только месяца
        // получалась ломаная дата вида ".12.3008" или "22..3008"
        var dateOfBirth = !string.IsNullOrEmpty(profile.BirthDay) && !string.IsNullOrEmpty(profile.BirthMonth)
            ? $"{profile.BirthDay}.{profile.BirthMonth}.{RecordDateConventions.CurrentYear - profile.Age}"
            : string.Empty;

        return new RecordIdentityData(
            profile.Name,
            string.Empty,
            noData,
            noData,
            HumanoidCharacterProfile.ComposeFullName(profile.Name, profile.Patronymic),
            loc.GetString(isManufactured ? "records-date-of-manufacture-edit" : "records-date-of-birth-edit"),
            string.IsNullOrWhiteSpace(dateOfBirth) ? noData : dateOfBirth,
            loc.GetString("station-records-gender", ("gender", profile.Gender.ToString())),
            species,
            HumanoidBodyMetrics.FormatHeight(loc, speciesPrototype, profile.Height),
            HumanoidBodyMetrics.FormatWeight(loc, speciesPrototype, profile.Width, profile.Height),
            RecordTraitSummary.FormatDisabilities(loc, prototypes, profile));
    }

    public static RecordIdentityData FromStationRecord(GeneralStationRecord record, IPrototypeManager prototypes, ILocalizationManager loc)
    {
        var noData = loc.GetString("records-value-no-data");
        var species = prototypes.TryIndex<SpeciesPrototype>(record.Species, out var speciesPrototype)
            ? loc.GetString(speciesPrototype.Name)
            : noData;
        var isManufactured = record.Species is "Ipc" or "Android";

        return new RecordIdentityData(
            record.Name,
            record.JobTitle,
            record.Fingerprint ?? noData,
            record.DNA ?? noData,
            string.IsNullOrWhiteSpace(record.FullName) ? record.Name : record.FullName,
            loc.GetString(isManufactured ? "records-date-of-manufacture-edit" : "records-date-of-birth-edit"),
            string.IsNullOrWhiteSpace(record.DateOfBirth) ? noData : record.DateOfBirth,
            loc.GetString("station-records-gender", ("gender", record.Gender.ToString())),
            species,
            HumanoidBodyMetrics.FormatHeight(loc, prototypes, record.Species, record.HumanoidProfile),
            HumanoidBodyMetrics.FormatWeight(loc, prototypes, record.Species, record.HumanoidProfile),
            RecordTraitSummary.FormatDisabilities(loc, prototypes, record.HumanoidProfile));
    }
}
