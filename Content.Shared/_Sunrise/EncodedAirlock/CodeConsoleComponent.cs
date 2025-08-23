using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using System.Linq;

namespace Content.Shared._Sunrise.EncodedAirlock;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CodeConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsLocked = true;

    [DataField, AutoNetworkedField]
    public int CodeLength = 6;

    [DataField, AutoNetworkedField]
    public string EnteredCode = string.Empty;

    [DataField("keypadPressSound")]
    public SoundSpecifier KeypadPressSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

    [DataField("accessGrantedSound")]
    public SoundSpecifier AccessGrantedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/confirm_beep.ogg");

    [DataField("accessDeniedSound")]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

    [DataField(serverOnly: true)]
    public string Code
    {
        get => _code;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !value.All(char.IsDigit))
                return;

            CodeLength = value.Length;
            _code = value;
        }
    }

    private string _code = string.Empty;
}



