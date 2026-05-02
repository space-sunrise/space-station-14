using System.Collections.Generic;
using JetBrains.Annotations;

namespace Content.Shared.Localizations;

public enum Case
{
    Nominative,
    Genitive,
    Dative,
    Accusative,
    Instrumental,
    Prepositional
}

[UsedImplicitly]
public static class FluentDecline
{
    private static readonly Dictionary<string, Dictionary<Case, string>> Forms = new()
    {
        // === ТИПЫ ПЕРСОНАЖЕЙ ===
        ["клоун"] = new()
        {
            [Case.Genitive] = "клоуна",
            [Case.Dative] = "клоуну",
            [Case.Accusative] = "клоуна",
            [Case.Instrumental] = "клоуном",
            [Case.Prepositional] = "клоуне"
        },
        ["мим"] = new()
        {
            [Case.Genitive] = "мима",
            [Case.Dative] = "миму",
            [Case.Accusative] = "мима",
            [Case.Instrumental] = "мимом",
            [Case.Prepositional] = "миме"
        },
        ["репортёр"] = new()
        {
            [Case.Genitive] = "репортёра",
            [Case.Dative] = "репортёру",
            [Case.Accusative] = "репортёра",
            [Case.Instrumental] = "репортёром",
            [Case.Prepositional] = "репортёре"
        },
        ["мясник"] = new()
        {
            [Case.Genitive] = "мясника",
            [Case.Dative] = "мяснику",
            [Case.Accusative] = "мясника",
            [Case.Instrumental] = "мясником",
            [Case.Prepositional] = "мяснике"
        },
        ["бармен"] = new()
        {
            [Case.Genitive] = "бармена",
            [Case.Dative] = "бармену",
            [Case.Accusative] = "бармена",
            [Case.Instrumental] = "барменом",
            [Case.Prepositional] = "бармене"
        },
        ["уборщик"] = new()
        {
            [Case.Genitive] = "уборщика",
            [Case.Dative] = "уборщику",
            [Case.Accusative] = "уборщика",
            [Case.Instrumental] = "уборщиком",
            [Case.Prepositional] = "уборщике"
        },
        ["инженер"] = new()
        {
            [Case.Genitive] = "инженера",
            [Case.Dative] = "инженеру",
            [Case.Accusative] = "инженера",
            [Case.Instrumental] = "инженером",
            [Case.Prepositional] = "инженере"
        },
        ["учёный"] = new()
        {
            [Case.Genitive] = "учёного",
            [Case.Dative] = "учёному",
            [Case.Accusative] = "учёного",
            [Case.Instrumental] = "учёным",
            [Case.Prepositional] = "учёном"
        },
        ["стражник"] = new()
        {
            [Case.Genitive] = "стражника",
            [Case.Dative] = "стражнику",
            [Case.Accusative] = "стражника",
            [Case.Instrumental] = "стражником",
            [Case.Prepositional] = "стражнике"
        },
        ["врач"] = new()
        {
            [Case.Genitive] = "врача",
            [Case.Dative] = "врачу",
            [Case.Accusative] = "врача",
            [Case.Instrumental] = "врачом",
            [Case.Prepositional] = "враче"
        },
        ["химик"] = new()
        {
            [Case.Genitive] = "химика",
            [Case.Dative] = "химику",
            [Case.Accusative] = "химика",
            [Case.Instrumental] = "химиком",
            [Case.Prepositional] = "химике"
        },
        ["заключённый"] = new()
        {
            [Case.Genitive] = "заключённого",
            [Case.Dative] = "заключённому",
            [Case.Accusative] = "заключённого",
            [Case.Instrumental] = "заключённым",
            [Case.Prepositional] = "заключённом"
        },
        ["исследователь"] = new()
        {
            [Case.Genitive] = "исследователя",
            [Case.Dative] = "исследователю",
            [Case.Accusative] = "исследователя",
            [Case.Instrumental] = "исследователем",
            [Case.Prepositional] = "исследователе"
        },
        ["торговец"] = new()
        {
            [Case.Genitive] = "торговца",
            [Case.Dative] = "торговцу",
            [Case.Accusative] = "торговца",
            [Case.Instrumental] = "торговцем",
            [Case.Prepositional] = "торговце"
        },
        ["капитан"] = new()
        {
            [Case.Genitive] = "капитана",
            [Case.Dative] = "капитану",
            [Case.Accusative] = "капитана",
            [Case.Instrumental] = "капитаном",
            [Case.Prepositional] = "капитане"
        },
        ["унатх"] = new()
        {
            [Case.Genitive] = "унатха",
            [Case.Dative] = "унатху",
            [Case.Accusative] = "унатха",
            [Case.Instrumental] = "унатхом",
            [Case.Prepositional] = "унатхе"
        },
        ["ниан"] = new()
        {
            [Case.Genitive] = "ниана",
            [Case.Dative] = "ниану",
            [Case.Accusative] = "ниана",
            [Case.Instrumental] = "нианом",
            [Case.Prepositional] = "ниане"
        },
        ["диона"] = new()
        {
            [Case.Genitive] = "дионы",
            [Case.Dative] = "дионе",
            [Case.Accusative] = "диону",
            [Case.Instrumental] = "дионой",
            [Case.Prepositional] = "дионе"
        },
        ["кошкомальчик"] = new()
        {
            [Case.Genitive] = "кошкомальчика",
            [Case.Dative] = "кошкомальчику",
            [Case.Accusative] = "кошкомальчика",
            [Case.Instrumental] = "кошкомальчиком",
            [Case.Prepositional] = "кошкомальчике"
        },
        ["кот"] = new()
        {
            [Case.Genitive] = "кота",
            [Case.Dative] = "коту",
            [Case.Accusative] = "кота",
            [Case.Instrumental] = "котом",
            [Case.Prepositional] = "коте"
        },
        ["корги"] = new()
        {
            [Case.Genitive] = "корги",
            [Case.Dative] = "корги",
            [Case.Accusative] = "корги",
            [Case.Instrumental] = "корги",
            [Case.Prepositional] = "корги"
        },
        ["пёс"] = new()
        {
            [Case.Genitive] = "пса",
            [Case.Dative] = "псу",
            [Case.Accusative] = "пса",
            [Case.Instrumental] = "псом",
            [Case.Prepositional] = "псе"
        },
        ["опоссум"] = new()
        {
            [Case.Genitive] = "опоссума",
            [Case.Dative] = "опоссуму",
            [Case.Accusative] = "опоссума",
            [Case.Instrumental] = "опоссумом",
            [Case.Prepositional] = "опоссуме"
        },
        ["ленивец"] = new()
        {
            [Case.Genitive] = "ленивца",
            [Case.Dative] = "ленивцу",
            [Case.Accusative] = "ленивца",
            [Case.Instrumental] = "ленивцем",
            [Case.Prepositional] = "ленивце"
        },
        ["агент Синдиката"] = new()
        {
            [Case.Genitive] = "агента Синдиката",
            [Case.Dative] = "агенту Синдиката",
            [Case.Accusative] = "агента Синдиката",
            [Case.Instrumental] = "агентом Синдиката",
            [Case.Prepositional] = "агенте Синдиката"
        },
        ["ревенант"] = new()
        {
            [Case.Genitive] = "ревенанта",
            [Case.Dative] = "ревенанту",
            [Case.Accusative] = "ревенанта",
            [Case.Instrumental] = "ревенантом",
            [Case.Prepositional] = "ревенанте"
        },
        ["крысиный король"] = new()
        {
            [Case.Genitive] = "крысиного короля",
            [Case.Dative] = "крысиному королю",
            [Case.Accusative] = "крысиного короля",
            [Case.Instrumental] = "крысиным королём",
            [Case.Prepositional] = "крысином короле"
        },
        ["ниндзя"] = new()
        {
            [Case.Genitive] = "ниндзя",
            [Case.Dative] = "ниндзя",
            [Case.Accusative] = "ниндзя",
            [Case.Instrumental] = "ниндзей",
            [Case.Prepositional] = "ниндзя"
        },
        ["космический дракон"] = new()
        {
            [Case.Genitive] = "космического дракона",
            [Case.Dative] = "космическому дракону",
            [Case.Accusative] = "космического дракона",
            [Case.Instrumental] = "космическим драконом",
            [Case.Prepositional] = "космическом драконе"
        },
        ["революционер"] = new()
        {
            [Case.Genitive] = "революционера",
            [Case.Dative] = "революционеру",
            [Case.Accusative] = "революционера",
            [Case.Instrumental] = "революционером",
            [Case.Prepositional] = "революционере"
        },
        ["ядерный оперативник"] = new()
        {
            [Case.Genitive] = "ядерного оперативника",
            [Case.Dative] = "ядерному оперативнику",
            [Case.Accusative] = "ядерного оперативника",
            [Case.Instrumental] = "ядерным оперативником",
            [Case.Prepositional] = "ядерном оперативнике"
        },
        ["культист Нар'си"] = new()
        {
            [Case.Genitive] = "культиста Нар'си",
            [Case.Dative] = "культисту Нар'си",
            [Case.Accusative] = "культиста Нар'си",
            [Case.Instrumental] = "культистом Нар'си",
            [Case.Prepositional] = "культисте Нар'си"
        },
        ["культист Ратвара"] = new()
        {
            [Case.Genitive] = "культиста Ратвара",
            [Case.Dative] = "культисту Ратвара",
            [Case.Accusative] = "культиста Ратвара",
            [Case.Instrumental] = "культистом Ратвара",
            [Case.Prepositional] = "культисте Ратвара"
        },
        ["грейтайдер"] = new()
        {
            [Case.Genitive] = "грейтайдера",
            [Case.Dative] = "грейтайдеру",
            [Case.Accusative] = "грейтайдера",
            [Case.Instrumental] = "грейтайдером",
            [Case.Prepositional] = "грейтайдере"
        },
        ["арахнид"] = new()
        {
            [Case.Genitive] = "арахнида",
            [Case.Dative] = "арахниду",
            [Case.Accusative] = "арахнида",
            [Case.Instrumental] = "арахнидом",
            [Case.Prepositional] = "арахниде"
        },
        ["вокс"] = new()
        {
            [Case.Genitive] = "вокса",
            [Case.Dative] = "воксу",
            [Case.Accusative] = "вокса",
            [Case.Instrumental] = "воксом",
            [Case.Prepositional] = "воксе"
        },
        ["дворф"] = new()
        {
            [Case.Genitive] = "дворфа",
            [Case.Dative] = "дворфу",
            [Case.Accusative] = "дворфа",
            [Case.Instrumental] = "дворфом",
            [Case.Prepositional] = "дворфе"
        },
        ["вор"] = new()
        {
            [Case.Genitive] = "вора",
            [Case.Dative] = "вору",
            [Case.Accusative] = "вора",
            [Case.Instrumental] = "вором",
            [Case.Prepositional] = "воре"
        },
        ["волшебник"] = new()
        {
            [Case.Genitive] = "волшебника",
            [Case.Dative] = "волшебнику",
            [Case.Accusative] = "волшебника",
            [Case.Instrumental] = "волшебником",
            [Case.Prepositional] = "волшебнике"
        },
        ["слайм"] = new()
        {
            [Case.Genitive] = "слайма",
            [Case.Dative] = "слайму",
            [Case.Accusative] = "слайма",
            [Case.Instrumental] = "слаймом",
            [Case.Prepositional] = "слайме"
        },

        // === ПРИЛАГАТЕЛЬНЫЕ ЧЕРТ ХАРАКТЕРА (мужской род) ===
        ["глупый"] = new()
        {
            [Case.Prepositional] = "глупом"
        },
        ["умный"] = new()
        {
            [Case.Prepositional] = "умном"
        },
        ["смешной"] = new()
        {
            [Case.Prepositional] = "смешном"
        },
        ["привлекательный"] = new()
        {
            [Case.Prepositional] = "привлекательном"
        },
        ["очаровательный"] = new()
        {
            [Case.Prepositional] = "очаровательном"
        },
        ["противный"] = new()
        {
            [Case.Prepositional] = "противном"
        },
        ["умирающий"] = new()
        {
            [Case.Prepositional] = "умирающем"
        },
        ["старый"] = new()
        {
            [Case.Prepositional] = "старом"
        },
        ["молодой"] = new()
        {
            [Case.Prepositional] = "молодом"
        },
        ["богатый"] = new()
        {
            [Case.Prepositional] = "богатом"
        },
        ["бедный"] = new()
        {
            [Case.Prepositional] = "бедном"
        },
        ["популярный"] = new()
        {
            [Case.Prepositional] = "популярном"
        },
        ["рассеянный"] = new()
        {
            [Case.Prepositional] = "рассеянном"
        },
        ["суровый"] = new()
        {
            [Case.Prepositional] = "суровом"
        },
        ["харизматичный"] = new()
        {
            [Case.Prepositional] = "харизматичном"
        },
        ["стоический"] = new()
        {
            [Case.Prepositional] = "стоическом"
        },
        ["милый"] = new()
        {
            [Case.Prepositional] = "милом"
        },
        ["дворфийский"] = new()
        {
            [Case.Prepositional] = "дворфийском"
        },
        ["пахнущий пивом"] = new()
        {
            [Case.Prepositional] = "пахнущем пивом"
        },
        ["радостный"] = new()
        {
            [Case.Prepositional] = "радостном"
        },
        ["страшно красивый"] = new()
        {
            [Case.Prepositional] = "страшно красивом"
        },
        ["роботизированный"] = new()
        {
            [Case.Prepositional] = "роботизированном"
        },
        ["голографический"] = new()
        {
            [Case.Prepositional] = "голографическом"
        },
        ["истерически смеющийся"] = new()
        {
            [Case.Prepositional] = "истерически смеющемся"
        }
    };

    private static readonly Dictionary<string, string> ActionCases = new()
    {
        // действие -> падеж (значения: "nominative","genitive","dative","accusative","instrumental","prepositional")
        ["сливаются в поцелуе, на глазах у"] = "genitive",
        ["насмерть душат"] = "accusative",
        ["умудряются разнести на части"] = "accusative",
        ["выигрывают партию в шахматы, где их оппонентом является"] = "nominative",
        ["с треском проигрывают партию в шахматы, где их оппонентом является"] = "nominative",
        ["раскрывают тёмные тайны, которые хранит"] = "nominative",
        ["манипулируют"] = "instrumental",
        ["приносят на алтаре жертву, которой является"] = "nominative",
        ["присутствуют на свадьбе, вместе с"] = "instrumental",
        ["объединяют усилия, чтобы победить общего врага, которым является"] = "nominative",
        ["вынуждены работать вместе, чтобы спастись от"] = "genitive",
        ["делают ценный подарок"] = "dative"
    };

    public static string GetCaseForAction(string actionPhrase)
    {
        if (ActionCases.TryGetValue(actionPhrase, out var caseName))
            return caseName;
        return "accusative"; // fallback на самый частый
    }

    public static string Decline(string word, string caseName)
    {
        if (Enum.TryParse<Case>(caseName, ignoreCase: true, out var c))
        {
            if (Forms.TryGetValue(word, out var forms) && forms.TryGetValue(c, out var declined))
                return declined;
        }
        return word; // fallback — именительный падеж
    }
}
