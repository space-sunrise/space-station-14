using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Sunrise.Reagents.Convert;

// TODO: Поддержка множества реагентов для конверта
[RegisterComponent, NetworkedComponent]
public sealed partial class ByClickReagentConvertComponent : Component
{
    [DataField(required: true), ViewVariables]
    public ProtoId<ReagentPrototype> Target;

    [DataField(required: true), ViewVariables]
    public ProtoId<ReagentPrototype> Result;

    #region Effects

    [DataField]
    public string? PopupMessage
    {
        get => _popupMessageId != null ? Loc.GetString(_popupMessageId) : null;
        set => _popupMessageId = value;
    }

    private string? _popupMessageId;

    [DataField]
    public SoundSpecifier? Sound;

    #endregion

    #region Whitelist

    [DataField] public EntityWhitelist? WhitelistUser;

    [DataField] public EntityWhitelist? WhitelistTarget;

    [DataField] public EntityWhitelist? BlacklistUser;

    [DataField] public EntityWhitelist? BlacklistTarget;

    #endregion

}
