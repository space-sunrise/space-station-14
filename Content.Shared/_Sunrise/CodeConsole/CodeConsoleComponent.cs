using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using System.Linq;

namespace Content.Shared._Sunrise.CodeConsole;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CodeConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool IsLocked = true;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public int CodeLength = 6;

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

    [DataField, AutoNetworkedField]
    public bool IsSealed = false;

    [DataField, AutoNetworkedField]
    public string ActivatePort = "Pressed";

    [DataField, AutoNetworkedField]
    public string WrongCodePort = "WrongCode";

    private string _code = string.Empty;

    [DataField(serverOnly: true)]
    public string EnteredCode = string.Empty;

    [DataField]
    public SoundSpecifier KeypadPressSound = new SoundPathSpecifier("/Audio/Machines/Nuke/general_beep.ogg");

    [DataField]
    public SoundSpecifier AccessGrantedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/confirm_beep.ogg");

    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");
}



