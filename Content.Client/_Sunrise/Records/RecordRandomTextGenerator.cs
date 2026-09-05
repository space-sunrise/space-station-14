using System;
using Robust.Shared.Enums;
using Robust.Shared.Random;
using Content.Shared._Sunrise.Records;
using Content.Shared.Preferences;

namespace Content.Client._Sunrise.Records;

// Sunrise added start — генератор шаблонных вариантов для кнопок случайного заполнения досье персонажа
/// <summary>
/// Подбирает случайный вариант текста для кнопок "Рандом" в редакторе досье.
/// Часть полей — простой выбор из локализованного набора строк, часть (контакты,
/// история арестов/заключений/трудоустройства, улица проживания) собирается из
/// независимых частей, чтобы итоговых сочетаний было заметно больше числа строк в FTL.
/// </summary>
public static class RecordRandomTextGenerator
{
    // Sunrise: не менее 11 вариантов у одиночных полей — вероятность точного совпадения
    // двух рандомизаций одного textbox'а (1/N) должна быть меньше 10%.
    private const int DefaultVariantCount = 11;

    private static string Phrase(IRobustRandom random, ILocalizationManager loc, string keyPrefix, int count = DefaultVariantCount)
        => loc.GetString($"{keyPrefix}-{random.Next(1, count + 1)}");

    // Sunrise: код связи в формате станционного ID — 2 буквы + 6 цифр (например, QK-482917),
    // а не военный "позывной"
    private const string CodeLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private static string GenerateServiceCode(IRobustRandom random)
    {
        var first = CodeLetters[random.Next(CodeLetters.Length)];
        var second = CodeLetters[random.Next(CodeLetters.Length)];
        var digits = random.Next(0, 1000000);
        return $"{first}{second}-{digits:D6}";
    }

    public static string EmergencyContact(IRobustRandom random, ILocalizationManager loc, string species, Gender gender)
    {
        if (random.Prob(0.2f))
            return Phrase(random, loc, "records-random-emergency-official", 2);

        var relation = Phrase(random, loc, "records-random-emergency-relation", 6);
        var method = random.Prob(0.4f)
            ? loc.GetString("records-random-emergency-code", ("code", GenerateServiceCode(random)))
            : Phrase(random, loc, "records-random-emergency-method", 4);
        var name = HumanoidCharacterProfile.GetName(species, gender);

        return loc.GetString("records-random-emergency-contact-template",
            ("name", name), ("relation", relation), ("method", method));
    }

    public static string CloseRelatives(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-close-relatives", 11);

    public static string Notes(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-notes");

    public static string Postmortem(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-postmortem");

    public static string Physiological(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-physiological");

    public static string Psychological(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-psychological");

    public static string ResidenceRegion(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-residence-region");

    public static string ResidencePlanet(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-residence-planet");

    public static string ResidenceStreet(IRobustRandom random, ILocalizationManager loc)
    {
        var name = Phrase(random, loc, "records-random-residence-street-name", 5);
        return loc.GetString("records-random-residence-street-template", ("name", name), ("number", random.Next(1, 40)));
    }

    public static string ResidenceDetails(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-residence-details");

    public static string IdentifyingFeatures(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-identifying-features");

    private const int CleanVariantCount = 5;

    public static string ArrestHistory(IRobustRandom random, ILocalizationManager loc)
        => IncidentHistory(random, loc, "records-random-arrest");

    // Sunrise: заключение без ареста нелогично (в тюрьму без задержания не попадают), поэтому
    // если по текущей записи "История арестов" видно, что задержаний не было, заключение
    // тоже принудительно остаётся чистым — дерево инцидентов для него даже не разыгрывается.
    public static string ImprisonmentHistory(IRobustRandom random, ILocalizationManager loc, bool hasArrestRecord)
    {
        if (!hasArrestRecord)
            return Phrase(random, loc, "records-random-imprisonment-clean", CleanVariantCount);

        return IncidentHistory(random, loc, "records-random-imprisonment");
    }

    /// <summary>
    /// Проверяет, является ли текущий текст поля "История арестов" одним из вариантов
    /// "чистой" записи (в т.ч. пустое поле) — используется, чтобы не допустить заключения
    /// без предшествующего ареста при рандомизации.
    /// </summary>
    public static bool IsArrestRecordClean(ILocalizationManager loc, string arrestText)
    {
        var trimmed = arrestText.Trim();
        if (trimmed.Length == 0)
            return true;

        for (var i = 1; i <= CleanVariantCount; i++)
        {
            if (trimmed == loc.GetString($"records-random-arrest-clean-{i}"))
                return true;
        }

        return false;
    }

    // Составной инцидент: с вероятностью 55% — чистая запись, иначе выбирается ветка дерева
    // исход -> причина -> опциональное уточнение (у каждого исхода 4 причины, у каждой причины 2 уточнения,
    // все они существуют только внутри своей ветки, поэтому сочетания всегда получаются логичными)
    private static string IncidentHistory(IRobustRandom random, ILocalizationManager loc, string keyPrefix)
    {
        if (random.Prob(0.55f))
            return Phrase(random, loc, $"{keyPrefix}-clean", CleanVariantCount);

        var outcomeIndex = random.Next(1, 5);
        var reasonIndex = random.Next(1, 5);
        var outcome = loc.GetString($"{keyPrefix}-outcome-{outcomeIndex}");
        var reason = loc.GetString($"{keyPrefix}-outcome-{outcomeIndex}-reason-{reasonIndex}");
        var suffix = random.Prob(0.5f)
            ? loc.GetString($"{keyPrefix}-outcome-{outcomeIndex}-reason-{reasonIndex}-suffix-{random.Next(1, 3)}")
            : string.Empty;

        return loc.GetString($"{keyPrefix}-template", ("outcome", outcome), ("reason", reason), ("suffix", suffix));
    }

    public static string AcademicField(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-academic-field");

    public static string Licenses(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-licenses");

    public static string EmploymentHistory(IRobustRandom random, ILocalizationManager loc)
    {
        var basePhrase = Phrase(random, loc, "records-random-employment-history-base");
        if (!random.Prob(0.5f))
            return basePhrase;

        return loc.GetString("records-random-employment-history-with-years",
            ("base", basePhrase), ("years", random.Next(1, 16)));
    }

    public static string Specialty(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-specialty");

    public static string Institution(IRobustRandom random, ILocalizationManager loc)
        => Phrase(random, loc, "records-random-institution");

    public static string ResidenceUnit(IRobustRandom random, ILocalizationManager loc)
        => loc.GetString("records-random-apartment-unit", ("value", random.Next(1, 340)));

    // Sunrise: год не должен быть раньше рождения персонажа (было: фиксированный диапазон
    // 2530-2569, из-за чего диплом мог оказаться выдан за сотни лет до рождения). Без известного
    // возраста (например, окно предпросмотра без профиля) используем старое поведение — год из
    // последних 40 лет до текущего.
    public static string Year(IRobustRandom random, int? age = null)
    {
        if (age is not { } characterAge)
            return (RecordDateConventions.CurrentYear - random.Next(0, 40)).ToString();

        var birthYear = RecordDateConventions.CurrentYear - characterAge;
        var earliestYear = Math.Min(birthYear + 18, RecordDateConventions.CurrentYear);
        return random.Next(earliestYear, RecordDateConventions.CurrentYear + 1).ToString();
    }
}
// Sunrise added end
