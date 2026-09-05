using System;
using System.Collections.Generic;
using Content.Shared._Sunrise.Records;
using Robust.Shared.Random;

namespace Content.Server.StationRecords.Systems;

public sealed partial class StationRecordsSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    // Sunrise added start — авто-заполнение образования для ролей, где его отсутствие нелогично
    // (капитан без диплома, врач без медицинского образования и т.п.). Затрагивает только
    // запись станции текущего раунда — сохранённое досье персонажа (profile.EmploymentRecord)
    // не перезаписывается, поэтому собственные записи игрока никогда не теряются и не подменяются.
    // Покрывает все должности, включая Sunrise-специфичные (_Sunrise/Roles/Jobs), кроме:
    // Passenger/Visitor/PlanetPrisoner (нет фиксированной профессии — как у пассажира или
    // заключённого) и Borg/StationAi (синтетики, у которых само понятие диплома неприменимо).
    //
    // У каждой профессии — своя специальность; общий текст допускается только внутри одной
    // карьерной ветки одного и того же дела (стажёр/специалист/руководитель, например
    // MedicalIntern -> MedicalDoctor, или SecurityCadet -> SecurityOfficer), либо когда роль ERT
    // — это буквально та же профессия, временно откомандированная в отряд быстрого реагирования
    // (ERTEngineer — это инженер станции, ERTMedical — фельдшер и т.д.). Вуз при этом может
    // совпадать чаще специальности — один институт готовит разные специальности, это нормально.
    private static readonly Dictionary<string, (RecordAcademicDegree Degree, string SpecialtyLocKey, string InstitutionLocKey)> RequiredEducationByJob = new()
    {
        // Командование
        ["Captain"] = (RecordAcademicDegree.Doctor, "records-random-specialty-5", "records-random-institution-2"),
        ["HeadOfPersonnel"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-personnel-management", "records-random-institution-2"),

        // Безопасность
        ["HeadOfSecurity"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-law-enforcement-administration", "records-random-institution-7"),
        ["Warden"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-penitentiary-affairs", "records-random-institution-7"),
        ["Detective"] = (RecordAcademicDegree.Bachelor, "records-random-specialty-10", "records-random-institution-7"),
        ["SecurityOfficer"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-combat-training", "records-random-institution-7"),
        ["SecurityCadet"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-combat-training", "records-random-institution-7"),

        // Медицина
        ["ChiefMedicalOfficer"] = (RecordAcademicDegree.Doctor, "records-auto-education-specialty-healthcare-administration", "records-random-institution-4"),
        ["MedicalDoctor"] = (RecordAcademicDegree.Master, "records-random-academic-field-4", "records-random-institution-4"),
        ["MedicalIntern"] = (RecordAcademicDegree.Bachelor, "records-random-academic-field-4", "records-random-institution-4"),
        ["Paramedic"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-emergency-medicine", "records-random-institution-4"),
        ["Chemist"] = (RecordAcademicDegree.Bachelor, "records-random-specialty-4", "records-random-institution-3"),
        ["Psychologist"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-clinical-psychology", "records-random-institution-4"),

        // Наука
        ["ResearchDirector"] = (RecordAcademicDegree.Doctor, "records-random-academic-field-3", "records-random-institution-3"),
        ["Scientist"] = (RecordAcademicDegree.Master, "records-random-academic-field-1", "records-random-institution-6"),
        ["ResearchAssistant"] = (RecordAcademicDegree.Bachelor, "records-random-academic-field-1", "records-random-institution-6"),

        // Инженерия
        ["ChiefEngineer"] = (RecordAcademicDegree.Master, "records-random-academic-field-2", "records-random-institution-1"),
        ["StationEngineer"] = (RecordAcademicDegree.Bachelor, "records-random-specialty-9", "records-random-institution-1"),
        ["AtmosphericTechnician"] = (RecordAcademicDegree.Bachelor, "records-random-specialty-3", "records-random-institution-1"),
        ["TechnicalAssistant"] = (RecordAcademicDegree.Qualificate, "records-random-specialty-9", "records-random-institution-1"),

        // Снабжение
        ["Quartermaster"] = (RecordAcademicDegree.Bachelor, "records-random-specialty-11", "records-random-institution-2"),
        ["CargoTechnician"] = (RecordAcademicDegree.Qualificate, "records-random-specialty-11", "records-random-institution-2"),
        ["SalvageSpecialist"] = (RecordAcademicDegree.Qualificate, "records-random-specialty-8", "records-random-institution-9"),

        // Обслуживание и гражданские специальности
        ["Bartender"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-beverage-service", "records-auto-education-institution-service-academy"),
        ["Botanist"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-agronomy", "records-random-institution-5"),
        ["Chaplain"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-theology", "records-auto-education-institution-theology-seminary"),
        ["Chef"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-culinary-arts", "records-auto-education-institution-service-academy"),
        ["Clown"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-circus-arts", "records-auto-education-institution-arts-conservatory"),
        ["Janitor"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-facility-maintenance", "records-auto-education-institution-service-academy"),
        ["Lawyer"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-corporate-law", "records-random-institution-2"),
        ["Librarian"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-library-science", "records-random-institution-5"),
        ["Mime"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-mime-performance", "records-auto-education-institution-arts-conservatory"),
        ["Musician"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-musical-arts", "records-auto-education-institution-arts-conservatory"),
        ["ServiceWorker"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-guest-services", "records-auto-education-institution-service-academy"),
        ["Reporter"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-journalism", "records-random-institution-5"),

        // Central Command / ERT — свои специальности, кроме случаев, где ERT-роль это та же
        // профессия, что и её станционный аналог, временно откомандированная в отряд
        ["CentralCommandOfficial"] = (RecordAcademicDegree.Doctor, "records-random-academic-field-5", "records-random-institution-2"),
        ["CBURN"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-biochemical-defense", "records-auto-education-institution-hazmat-institute"),
        ["DeathSquad"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-special-forces-training", "records-random-institution-7"),
        ["ERTLeader"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-tactical-command", "records-random-institution-7"),
        ["ERTChaplain"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-theology", "records-auto-education-institution-theology-seminary"),
        ["ERTEngineer"] = (RecordAcademicDegree.Bachelor, "records-random-specialty-9", "records-random-institution-1"),
        ["ERTSecurity"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-combat-training", "records-random-institution-7"),
        ["ERTMedical"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-emergency-medicine", "records-random-institution-4"),
        ["ERTJanitor"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-facility-maintenance", "records-auto-education-institution-service-academy"),

        // Sunrise-специфичные должности (_Sunrise/Roles/Jobs) — старшие/младшие варианты
        // делят специальность с базовой ролью того же карьерного трека (см. правило выше)
        ["SeniorEngineer"] = (RecordAcademicDegree.Master, "records-random-specialty-9", "records-random-institution-1"),
        ["SeniorPhysician"] = (RecordAcademicDegree.Master, "records-random-academic-field-4", "records-random-institution-4"),
        ["SeniorResearcher"] = (RecordAcademicDegree.Master, "records-random-academic-field-1", "records-random-institution-6"),
        ["SeniorOfficer"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-combat-training", "records-random-institution-7"),
        ["MailCarrier"] = (RecordAcademicDegree.Qualificate, "records-random-specialty-11", "records-random-institution-2"),
        ["MiningSpecialist"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-mining-engineering", "records-random-institution-9"),
        ["Adjutant"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-law-enforcement-administration", "records-random-institution-7"),
        ["ComMaid"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-executive-personal-service", "records-auto-education-institution-service-academy"),
        ["NanoTrasenRepresentative"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-corporate-communications", "records-random-institution-2"),
        ["IAA"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-internal-investigations", "records-random-institution-2"),
        ["Magistrat"] = (RecordAcademicDegree.Doctor, "records-auto-education-specialty-judicial-proceedings", "records-random-institution-2"),
        ["NtrOfficer"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-corporate-law-enforcement", "records-random-institution-7"),
        ["NtrLeader"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-corporate-law-enforcement", "records-random-institution-7"),
        ["NtrGuard"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-corporate-law-enforcement", "records-random-institution-7"),
        ["CentCommOfficial"] = (RecordAcademicDegree.Doctor, "records-random-academic-field-5", "records-random-institution-2"),
        ["CentCommOperator"] = (RecordAcademicDegree.Bachelor, "records-random-academic-field-5", "records-random-institution-2"),
        ["BlueShieldEnsign"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-internal-oversight", "records-random-institution-7"),
        ["BlueShieldOfficer"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-internal-oversight", "records-random-institution-7"),
        ["BlueShieldOperative"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-covert-operations", "records-random-institution-2"),
        ["CBURNAgentEVA"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-biochemical-defense", "records-auto-education-institution-hazmat-institute"),
        ["CBURNLeader"] = (RecordAcademicDegree.Doctor, "records-auto-education-specialty-biochemical-defense", "records-auto-education-institution-hazmat-institute"),
        ["TSFMarine"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-combined-arms-training", "records-random-institution-7"),
        ["Barber"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-hairdressing", "records-auto-education-institution-service-academy"),
        ["PrisonInspector"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-penitentiary-affairs", "records-random-institution-7"),
        ["PrisonScientist"] = (RecordAcademicDegree.Bachelor, "records-random-specialty-10", "records-random-institution-7"),
        ["PrisonTrainee"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-penitentiary-affairs", "records-random-institution-7"),
        ["USSPCrew"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-naval-service", "records-auto-education-institution-ussp-academy"),
        ["USSPSoldier"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-naval-service", "records-auto-education-institution-ussp-academy"),
        ["USSPOfficer"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-naval-service", "records-auto-education-institution-ussp-academy"),
        ["USSPOfficerAlt"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-naval-service", "records-auto-education-institution-ussp-academy"),
        ["USSPCaptain"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-naval-service", "records-auto-education-institution-ussp-academy"),

        ["SecurityPilot"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-vehicle-piloting", "records-auto-education-institution-piloting-academy"),
        ["Brigmedic"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-emergency-medicine", "records-random-institution-4"),
        ["Roboticist"] = (RecordAcademicDegree.Bachelor, "records-random-academic-field-7", "records-random-institution-1"),
        ["MedicalPathologist"] = (RecordAcademicDegree.Master, "records-auto-education-specialty-pathological-anatomy", "records-random-institution-4"),

        // Планетарная тюрьма (PlanetPrison) — PlanetPrisoner исключён: это статус заключённого,
        // а не профессия (как и Passenger/Visitor)
        ["HeadOfPrison"] = (RecordAcademicDegree.Doctor, "records-auto-education-specialty-penitentiary-affairs", "records-random-institution-7"),
        ["PrisonChef"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-culinary-arts", "records-auto-education-institution-service-academy"),
        ["PrisonDoctor"] = (RecordAcademicDegree.Master, "records-random-academic-field-4", "records-random-institution-4"),
        ["PrisonEngineer"] = (RecordAcademicDegree.Bachelor, "records-random-specialty-9", "records-random-institution-1"),
        ["PrisonPilot"] = (RecordAcademicDegree.Bachelor, "records-auto-education-specialty-vehicle-piloting", "records-auto-education-institution-piloting-academy"),
        ["PrisonWorker"] = (RecordAcademicDegree.Qualificate, "records-auto-education-specialty-penitentiary-affairs", "records-random-institution-7"),
    };

    // Sunrise-Edit: год диплома раньше был фиксированным диапазоном 2530-2569, не связанным
    // с игровой хронологией (текущий год ~3026) — диплом мог оказаться выдан за сотни лет
    // до рождения персонажа. Теперь дата выводится из года рождения (age) и не позже текущего года.
    private string EnsureRequiredEducation(string employmentRecord, string jobId, int age)
    {
        if (!RequiredEducationByJob.TryGetValue(jobId, out var requirement))
            return employmentRecord;

        var record = StructuredCharacterRecords.ReadEmployment(employmentRecord);
        if (record.Education.Count > 0)
            return employmentRecord;

        var birthYear = RecordDateConventions.CurrentYear - age;
        var earliestDiplomaYear = Math.Min(birthYear + 18, RecordDateConventions.CurrentYear);
        var diplomaYear = _random.Next(earliestDiplomaYear, RecordDateConventions.CurrentYear + 1);

        record.Education.Add(new EducationRecordData
        {
            Specialty = Loc.GetString(requirement.SpecialtyLocKey),
            Degree = requirement.Degree,
            Institution = Loc.GetString(requirement.InstitutionLocKey),
            DiplomaDate = diplomaYear.ToString(),
        });

        return StructuredCharacterRecords.WriteEmployment(record);
    }
    // Sunrise added end
}
