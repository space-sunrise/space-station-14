using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Actions;

// TODO ACTIONS make this a separate component and remove the inheritance stuff.
// TODO ACTIONS convert to auto comp state?

// TODO add access attribute. Need to figure out what to do with decal & mapping actions.
// [Access(typeof(SharedActionsSystem))]
[EntityCategory("Actions")]
public abstract partial class BaseActionComponent : Component
{
    /// <summary>
    ///     What entity, if any, currently has this action in the actions component?
    /// </summary>
    [DataField] public EntityUid? AttachedEntity;

    /// <summary>
    ///     Whether or not to automatically add this action to the action bar when it becomes available.
    /// </summary>
    [DataField("autoPopulate")] public bool AutoPopulate = true;

    /// <summary>
    ///     For toggle actions only, background to show when toggled on.
    /// </summary>
    [DataField] public SpriteSpecifier? BackgroundOn;

    /// <summary>
    ///     Convenience tool for actions with limited number of charges. Automatically decremented on use, and the
    ///     action is disabled when it reaches zero. Does NOT automatically remove the action from the action bar.
    ///     However, charges will regenerate if <see cref="RenewCharges"/> is enabled and the action will not disable
    ///     when charges reach zero.
    /// </summary>
    [DataField("charges")] public int? Charges;

    /// <summary>
    ///     Whether the action system should block this action if the user cannot currently interact. Some spells or
    ///     abilities may want to disable this and implement their own checks.
    /// </summary>
    [DataField("checkCanInteract")] public bool CheckCanInteract = true;

    /// <summary>
    /// Whether to check if the user is conscious or not. Can be used instead of <see cref="CheckCanInteract"/>
    /// for a more permissive check.
    /// </summary>
    [DataField] public bool CheckConsciousness = true;

    /// <summary>
    ///     If true, this will cause the action to only execute locally without ever notifying the server.
    /// </summary>
    [DataField("clientExclusive")] public bool ClientExclusive = false;
    // Sunrise-End

    /// <summary>
    /// The entity that contains this action. If the action is innate, this may be the user themselves.
    /// This should almost always be non-null.
    /// </summary>
    [Access(typeof(ActionContainerSystem), typeof(SharedActionsSystem))]
    [DataField]
    public EntityUid? Container;

    /// <summary>
    ///     The current cooldown on the action.
    /// </summary>
    // TODO serialization
    public (TimeSpan Start, TimeSpan End)? Cooldown;

    // Sunrise-Start
    [DataField("deleteActionsWithoutCharges")]
    public bool DeleteActionsWithoutCharges;

    /// <summary>
    ///     The color the action should turn to when disabled
    /// </summary>
    [DataField] public Color DisabledIconColor = Color.DimGray;

    /// <summary>
    ///     Whether this action is currently enabled. If not enabled, this action cannot be performed.
    /// </summary>
    [DataField("enabled")] public bool Enabled = true;

    [DataField]
    public EntityUid? EntIcon;

    /// <summary>
    ///     Icon representing this action in the UI.
    /// </summary>
    [DataField("icon")] public SpriteSpecifier? Icon;

    /// <summary>
    ///     If not null, this color will modulate the action icon color.
    /// </summary>
    /// <remarks>
    ///     This currently only exists for decal-placement actions, so that the action icons correspond to the color of
    ///     the decal. But this is probably useful for other actions, including maybe changing color on toggle.
    /// </remarks>
    [DataField("iconColor")] public Color IconColor = Color.White;

    /// <summary>
    ///     For toggle actions only, icon to show when toggled on. If omitted, the action will simply be highlighted
    ///     when turned on.
    /// </summary>
    [DataField("iconOn")] public SpriteSpecifier? IconOn;

    /// <summary>
    ///     Determines the appearance of the entity-icon for actions that are enabled via some entity.
    /// </summary>
    [DataField("itemIconStyle")] public ItemActionIconStyle ItemIconStyle;

    /// <summary>
    ///     Keywords that can be used to search for this action in the action menu.
    /// </summary>
    [DataField("keywords")] public HashSet<string> Keywords = new();

    /// <summary>
    ///     The max charges this action has. If null, this is set automatically from <see cref="Charges"/> on mapinit.
    /// </summary>
    [DataField] public int? MaxCharges;

    /// <summary>
    ///     The original <see cref="IconColor"/> this action was.
    /// </summary>
    [DataField] public Color OriginalIconColor;

    /// <summary>
    ///     Determines the order in which actions are automatically added the action bar.
    /// </summary>
    [DataField("priority")] public int Priority = 0;

    /// <summary>
    ///     If true, this will cause the the action event to always be raised directed at the action itself instead of the action's container/provider.
    ///     Takes priority over RaiseOnUser.
    /// </summary>
    [DataField]
    [Obsolete("This datafield will be reworked in an upcoming action refactor")]
    public bool RaiseOnAction;

    /// <summary>
    ///     If true, this will cause the the action event to always be raised directed at the action performer/user instead of the action's container/provider.
    /// </summary>
    [DataField]
    public bool RaiseOnUser;

    /// <summary>
    ///     If enabled, charges will regenerate after a <see cref="Cooldown"/> is complete
    /// </summary>
    [DataField("renewCharges")] public bool RenewCharges;

    /// <summary>
    ///     If not null, this sound will be played when performing this action.
    /// </summary>
    [DataField("sound")] public SoundSpecifier? Sound;

    /// <summary>
    ///     If true, the action will have an initial cooldown applied upon addition.
    /// </summary>
    [DataField] public bool StartDelay = false;

    /// <summary>
    ///     Temporary actions are deleted when they get removed a <see cref="ActionsComponent"/>.
    /// </summary>
    [DataField("temporary")] public bool Temporary;

    /// <summary>
    ///     The toggle state of this action. Toggling switches the currently displayed icon, see <see cref="Icon"/> and <see cref="IconOn"/>.
    /// </summary>
    /// <remarks>
    ///     The toggle can set directly via <see cref="SharedActionsSystem.SetToggled"/>, but it will also be
    ///     automatically toggled for targeted-actions while selecting a target.
    /// </remarks>
    [DataField]
    public bool Toggled;

    /// <summary>
    ///     Time interval between action uses.
    /// </summary>
    [DataField("useDelay")] public TimeSpan? UseDelay;

    public abstract BaseActionEvent? BaseEvent { get; }

    // Sunrise-Start
    public TimeSpan? LastChargeRenewTime { get; set; }

    [DataField("renewChargeDelay")]
    public TimeSpan RenewChargeDelay { get; set; }

    /// <summary>
    ///     Entity to use for the action icon. If no entity is provided and the <see cref="Container"/> differs from
    ///     <see cref="AttachedEntity"/>, then it will default to using <see cref="Container"/>
    /// </summary>
    public EntityUid? EntityIcon
    {
        get
        {
            if (EntIcon != null)
                return EntIcon;

            if (AttachedEntity != Container)
                return Container;

            return null;
        }
        set => EntIcon = value;
    }
    // Sunrise-End
}

[Serializable, NetSerializable]
public abstract class BaseActionComponentState : ComponentState
{
    public NetEntity? AttachedEntity;
    public bool AutoPopulate;
    public int? Charges;
    public bool CheckCanInteract;
    public bool CheckConsciousness;
    public bool ClientExclusive;
    public NetEntity? Container;
    public (TimeSpan Start, TimeSpan End)? Cooldown;
    public bool DeleteActionsWithoutCharges; // Sunrise-Edit
    public Color DisabledIconColor;
    public bool Enabled;
    public NetEntity? EntityIcon;
    public SpriteSpecifier? Icon;
    public Color IconColor;
    public SpriteSpecifier? IconOn;
    public ItemActionIconStyle ItemIconStyle;
    public HashSet<string> Keywords;
    public TimeSpan? LastChargeRenewTime; // Sunrise-Edit
    public int? MaxCharges;
    public Color OriginalIconColor;
    public int Priority;
    public bool RaiseOnAction;
    public bool RaiseOnUser;
    public TimeSpan RenewChargeDelay; // Sunrise-Edit
    public bool RenewCharges;
    public SoundSpecifier? Sound;
    public bool Temporary;
    public bool Toggled;
    public TimeSpan? UseDelay;

    protected BaseActionComponentState(BaseActionComponent component, IEntityManager entManager)
    {
        Container = entManager.GetNetEntity(component.Container);
        EntityIcon = entManager.GetNetEntity(component.EntIcon);
        AttachedEntity = entManager.GetNetEntity(component.AttachedEntity);
        RaiseOnUser = component.RaiseOnUser;
        RaiseOnAction = component.RaiseOnAction;
        Icon = component.Icon;
        IconOn = component.IconOn;
        IconColor = component.IconColor;
        OriginalIconColor = component.OriginalIconColor;
        DisabledIconColor = component.DisabledIconColor;
        Keywords = component.Keywords;
        Enabled = component.Enabled;
        Toggled = component.Toggled;
        Cooldown = component.Cooldown;
        UseDelay = component.UseDelay;
        Charges = component.Charges;
        MaxCharges = component.MaxCharges;
        RenewCharges = component.RenewCharges;
        RenewChargeDelay = component.RenewChargeDelay; // Sunrise-Edit
        LastChargeRenewTime = component.LastChargeRenewTime; // Sunrise-Edit
        CheckCanInteract = component.CheckCanInteract;
        CheckConsciousness = component.CheckConsciousness;
        ClientExclusive = component.ClientExclusive;
        Priority = component.Priority;
        AutoPopulate = component.AutoPopulate;
        Temporary = component.Temporary;
        ItemIconStyle = component.ItemIconStyle;
        Sound = component.Sound;
        DeleteActionsWithoutCharges = component.DeleteActionsWithoutCharges; // Sunrise-Edit
    }
}
