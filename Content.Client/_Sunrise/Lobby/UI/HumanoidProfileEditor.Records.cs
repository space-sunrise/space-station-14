using Content.Client._Sunrise.Lobby.UI;
using Content.Shared._Sunrise.Records;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private CharacterRecordsTab? _recordsTab;

    private void InitializeRecordsTab()
    {
        _recordsTab = new CharacterRecordsTab();
        TabContainer.AddChild(_recordsTab);
        TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("character-records-tab-title"));

        _recordsTab.OnRecordsChanged += OnRecordFieldChanged;
    }

    private void UpdateRecordsTab()
    {
        if (_recordsTab is null || Profile is null)
            return;

        _recordsTab.PatronymicValue      = Profile.Patronymic;
        _recordsTab.BirthDayValue       = Profile.BirthDay;
        _recordsTab.BirthMonthValue     = Profile.BirthMonth;
        _recordsTab.SetBirthYear(RecordDateConventions.CurrentYear - Profile.Age);
        _recordsTab.MedicalRecordValue  = Profile.MedicalRecord;
        _recordsTab.SecurityRecordValue = Profile.SecurityRecord;
        _recordsTab.EmploymentRecordValue = Profile.EmploymentRecord;
        _recordsTab.SetProfileContext(Profile);
    }

    private void OnRecordFieldChanged()
    {
        if (_recordsTab is null || Profile is null)
            return;

        Profile = Profile
            .WithPatronymic(_recordsTab.PatronymicValue)
            .WithBirthDay(ClampBirthDay(_recordsTab.BirthDayValue))
            .WithBirthMonth(ClampBirthMonth(_recordsTab.BirthMonthValue))
            .WithMedicalRecord(_recordsTab.MedicalRecordValue)
            .WithSecurityRecord(_recordsTab.SecurityRecordValue)
            .WithEmploymentRecord(_recordsTab.EmploymentRecordValue);

        SetDirty();
    }

    private static string ClampBirthDay(string raw)
    {
        if (!int.TryParse(raw, out var d))
            return raw;
        return Math.Clamp(d, 1, 30).ToString();
    }

    private static string ClampBirthMonth(string raw)
    {
        if (!int.TryParse(raw, out var m))
            return raw;
        return Math.Clamp(m, 1, 12).ToString();
    }
}
