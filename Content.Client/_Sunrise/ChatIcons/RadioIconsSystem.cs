using Content.Client.UserInterface.Systems.Chat;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client._Sunrise.ChatIcons;

public sealed class ChatIconsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IUserInterfaceManager _uiMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.SetCVar("chat_icon.enable", true, true); // Sunrise TEMP ADD
        _cfg.OnValueChanged(SunriseCCVars.ChatIconsEnable, OnRadioIconsChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(SunriseCCVars.ChatIconsEnable, OnRadioIconsChanged);
    }

    private void OnRadioIconsChanged(bool enable)
    {
        _uiMan.GetUIController<ChatUIController>().Repopulate();
    }
}
