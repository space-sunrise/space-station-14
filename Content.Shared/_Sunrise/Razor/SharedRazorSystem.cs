// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt;

using Content.Shared._Sunrise.Razor;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared._Sunrise.Razor;
using Content.Shared.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Shared._Sunrise.Razor;

/// <summary>
/// Система для электробритвы - копия ножниц с вайлдберрис. Бритва не может менять цвет волос!
/// </summary>
public abstract class SharedRazorSystem : EntitySystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RazorComponent, AfterInteractEvent>(OnRazorInteract);
        SubscribeLocalEvent<RazorComponent, BeforeActivatableUIOpenEvent>(OnBeforeUIOpen);
        SubscribeLocalEvent<RazorComponent, ActivatableUIOpenAttemptEvent>(OnAttemptOpenUI);
        SubscribeLocalEvent<RazorComponent, BoundUserInterfaceCheckRangeEvent>(OnMirrorRangeCheck);
    }

    private void OnRazorInteract(Entity<RazorComponent> mirror, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        UpdateInterface(mirror, args.Target.Value, mirror);
        _uiSystem.TryOpenUi(mirror.Owner, RazorUiKey.Key, args.User);
    }

    private void OnMirrorRangeCheck(EntityUid uid, RazorComponent component, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (args.Result == BoundUserInterfaceRangeResult.Fail)
            return;

        if (component.Target == null || !Exists(component.Target))
        {
            component.Target = null;
            args.Result = BoundUserInterfaceRangeResult.Fail;
            return;
        }

        if (!_interaction.InRangeUnobstructed(component.Target.Value, uid))
            args.Result = BoundUserInterfaceRangeResult.Fail;
    }

    private void OnAttemptOpenUI(EntityUid uid, RazorComponent component, ref ActivatableUIOpenAttemptEvent args)
    {
        var user = component.Target ?? args.User;

        if (!HasComp<HumanoidAppearanceComponent>(user))
            args.Cancel();
    }

    private void OnBeforeUIOpen(Entity<RazorComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateInterface(ent, args.User, ent);
    }

    protected void UpdateInterface(EntityUid mirrorUid, EntityUid targetUid, RazorComponent component)
    {
        if (!TryComp<HumanoidAppearanceComponent>(targetUid, out var humanoid))
            return;

        component.Target ??= targetUid;

        var hair = humanoid.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairMarkings)
            ? new List<Marking>(hairMarkings)
            : [];

        var facialHair = humanoid.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialHairMarkings)
            ? new List<Marking>(facialHairMarkings)
            : [];

        var state = new RazorUiState(
            humanoid.Species,
            hair,
            humanoid.MarkingSet.PointsLeft(MarkingCategories.Hair) + hair.Count,
            facialHair,
            humanoid.MarkingSet.PointsLeft(MarkingCategories.FacialHair) + facialHair.Count);

        // TODO: Component states
        component.Target = targetUid;
        _uiSystem.SetUiState(mirrorUid, RazorUiKey.Key, state);
        Dirty(mirrorUid, component);
    }
}

[Serializable, NetSerializable]
public enum RazorUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum RazorCategory : byte
{
    Hair,
    FacialHair,
}

[Serializable, NetSerializable]
public sealed class RazorSelectMessage(RazorCategory category, string marking, int slot) : BoundUserInterfaceMessage
{
    public RazorCategory Category { get; } = category;
    public string Marking { get; } = marking;
    public int Slot { get; } = slot;
}

[Serializable, NetSerializable]
public sealed class RazorRemoveSlotMessage(RazorCategory category, int slot) : BoundUserInterfaceMessage
{
    public RazorCategory Category { get; } = category;
    public int Slot { get; } = slot;
}

[Serializable, NetSerializable]
public sealed class RazorSelectSlotMessage(RazorCategory category, int slot) : BoundUserInterfaceMessage
{
    public RazorCategory Category { get; } = category;
    public int Slot { get; } = slot;
}

[Serializable, NetSerializable]
public sealed class RazorAddSlotMessage(RazorCategory category) : BoundUserInterfaceMessage
{
    public RazorCategory Category { get; } = category;
}

[Serializable, NetSerializable]
public sealed class RazorUiState(
    string species,
    List<Marking> hair,
    int hairSlotTotal,
    List<Marking> facialHair,
    int facialHairSlotTotal)
    : BoundUserInterfaceState
{
    public NetEntity Target;

    public string Species = species;

    public List<Marking> Hair = hair;
    public int HairSlotTotal = hairSlotTotal;

    public List<Marking> FacialHair = facialHair;
    public int FacialHairSlotTotal = facialHairSlotTotal;
}

[Serializable, NetSerializable]
public sealed partial class RazorRemoveSlotDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }

    public RazorCategory Category;
    public int Slot;
}

[Serializable, NetSerializable]
public sealed partial class RazorAddSlotDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }

    public RazorCategory Category;
}

[Serializable, NetSerializable]
public sealed partial class RazorSelectDoAfterEvent : DoAfterEvent
{
    public RazorCategory Category;
    public int Slot;
    public string Marking = string.Empty;

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
