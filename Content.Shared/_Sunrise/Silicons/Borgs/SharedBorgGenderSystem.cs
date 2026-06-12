using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Borgs;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared._Sunrise.Silicons.Borgs;

/// <summary>
/// Handles borg gender selection and the related BUI.
/// </summary>
public sealed partial class SharedBorgGenderSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;

    private const string BorgChassisRsiPath = "Mobs/Silicon/chassis.rsi";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgGenderComponent, BorgGenderChangeActionEvent>(OnChangeGenderAction);

        Subs.BuiEvents<BorgGenderComponent>(BorgGenderUiKey.Key, subs =>
        {
            subs.Event<BorgGenderChangeMessage>(OnChangeGenderMessage);
        });
    }

    private void OnChangeGenderAction(Entity<BorgGenderComponent> ent, ref BorgGenderChangeActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryOpenGenderUi(ent.AsNullable(), args.Performer))
            return;

        args.Handled = true;
    }

    private void OnChangeGenderMessage(Entity<BorgGenderComponent> ent, ref BorgGenderChangeMessage args)
    {
        TrySetGender(ent.AsNullable(), args.Gender);
    }

    public bool TryOpenGenderUi(Entity<BorgGenderComponent?> ent, EntityUid actor)
    {
        if (!CanOpenGenderUi(ent, actor))
            return false;

        DoOpenGenderUi((ent.Owner, ent.Comp!), actor);
        return true;
    }

    public bool CanOpenGenderUi(Entity<BorgGenderComponent?> ent, EntityUid actor)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!HasComp<BorgChassisComponent>(ent.Owner))
            return false;

        if (!_userInterface.HasUi(ent.Owner, BorgGenderUiKey.Key))
            return false;

        return HasComp<ActorComponent>(actor);
    }

    private void DoOpenGenderUi(Entity<BorgGenderComponent> ent, EntityUid actor)
    {
        var actorComponent = Comp<ActorComponent>(actor);
        UpdateUiState(ent);
        _userInterface.OpenUi(ent.Owner, BorgGenderUiKey.Key, actorComponent.PlayerSession);
    }

    public bool TrySetGender(Entity<BorgGenderComponent?> ent, BorgGender gender)
    {
        if (!CanSetGender(ent, gender))
            return false;

        DoSetGender((ent.Owner, ent.Comp!), gender);
        return true;
    }

    public bool CanSetGender(Entity<BorgGenderComponent?> ent, BorgGender gender)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!HasComp<BorgChassisComponent>(ent.Owner))
            return false;

        return gender is BorgGender.Male or BorgGender.Female;
    }

    private void DoSetGender(Entity<BorgGenderComponent> ent, BorgGender gender)
    {
        if (ent.Comp.SelectedGender == gender)
        {
            UpdateUiState(ent);
            return;
        }

        var oldGender = ent.Comp.SelectedGender;
        ent.Comp.SelectedGender = gender;
        Dirty(ent);
        UpdateUiState(ent);

        var ev = new BorgGenderChangedEvent(oldGender, gender);
        RaiseLocalEvent(ent.Owner, ref ev);
    }

    private void UpdateUiState(Entity<BorgGenderComponent> ent)
    {
        if (!_userInterface.HasUi(ent.Owner, BorgGenderUiKey.Key))
            return;

        _userInterface.SetUiState(ent.Owner, BorgGenderUiKey.Key, new BorgGenderBuiState(ent.Comp.SelectedGender));
    }

    public BorgGenderResolvedVisuals ResolveVisuals(EntityUid uid, BorgTypePrototype prototype)
    {
        var gender = CompOrNull<BorgGenderComponent>(uid)?.SelectedGender ?? BorgGender.Male;
        return ResolveVisuals(prototype, gender);
    }

    public BorgGenderResolvedVisuals ResolveVisuals(BorgTypePrototype prototype, BorgGender gender)
    {
        prototype.GenderSprites.TryGetValue(gender, out var overrides);

        var body = CreateBaseLayer(prototype.SpriteBodyState);
        var movement = prototype.SpriteBodyMovementState is { } movementState
            ? CreateBaseLayer(movementState)
            : null;
        var hasMind = CreateBaseLayer(prototype.SpriteHasMindState);
        var noMind = CreateBaseLayer(prototype.SpriteNoMindState);
        var toggleLight = CreateBaseLayer(prototype.SpriteToggleLightState);

        return new BorgGenderResolvedVisuals(
            MergeLayerData(body, overrides?.Body),
            movement == null && overrides?.BodyMovement == null
                ? null
                : MergeLayerData(movement ?? body, overrides?.BodyMovement),
            MergeLayerData(hasMind, overrides?.HasMind),
            MergeLayerData(noMind, overrides?.NoMind),
            MergeLayerData(toggleLight, overrides?.ToggleLight));
    }

    public SpriteSpecifier ResolveBodySprite(EntityUid uid, BorgTypePrototype prototype)
    {
        var body = ResolveVisuals(uid, prototype).Body;

        if (body.TexturePath is { } texture)
            return new SpriteSpecifier.Texture(new ResPath(texture));

        return new SpriteSpecifier.Rsi(
            new ResPath(body.RsiPath ?? BorgChassisRsiPath),
            body.State ?? prototype.SpriteBodyState);
    }

    private static PrototypeLayerData CreateBaseLayer(string state)
    {
        return new PrototypeLayerData
        {
            State = state,
        };
    }

    private static PrototypeLayerData MergeLayerData(PrototypeLayerData baseLayer, PrototypeLayerData? overrideLayer)
    {
        if (overrideLayer == null)
            return CopyLayerData(baseLayer);

        return new PrototypeLayerData
        {
            Shader = overrideLayer.Shader ?? baseLayer.Shader,
            TexturePath = overrideLayer.TexturePath ?? baseLayer.TexturePath,
            RsiPath = overrideLayer.RsiPath ?? baseLayer.RsiPath,
            State = overrideLayer.State ?? baseLayer.State,
            Scale = overrideLayer.Scale ?? baseLayer.Scale,
            Rotation = overrideLayer.Rotation ?? baseLayer.Rotation,
            Offset = overrideLayer.Offset ?? baseLayer.Offset,
            Visible = overrideLayer.Visible ?? baseLayer.Visible,
            Color = overrideLayer.Color ?? baseLayer.Color,
            MapKeys = CopyMapKeys(overrideLayer.MapKeys ?? baseLayer.MapKeys),
            RenderingStrategy = overrideLayer.RenderingStrategy ?? baseLayer.RenderingStrategy,
            CopyToShaderParameters = overrideLayer.CopyToShaderParameters ?? baseLayer.CopyToShaderParameters,
            Cycle = overrideLayer.Cycle,
            Loop = overrideLayer.Loop,
        };
    }

    private static PrototypeLayerData CopyLayerData(PrototypeLayerData layer)
    {
        return new PrototypeLayerData
        {
            Shader = layer.Shader,
            TexturePath = layer.TexturePath,
            RsiPath = layer.RsiPath,
            State = layer.State,
            Scale = layer.Scale,
            Rotation = layer.Rotation,
            Offset = layer.Offset,
            Visible = layer.Visible,
            Color = layer.Color,
            MapKeys = CopyMapKeys(layer.MapKeys),
            RenderingStrategy = layer.RenderingStrategy,
            CopyToShaderParameters = layer.CopyToShaderParameters,
            Cycle = layer.Cycle,
            Loop = layer.Loop,
        };
    }

    private static HashSet<string>? CopyMapKeys(HashSet<string>? mapKeys)
    {
        return mapKeys == null ? null : new HashSet<string>(mapKeys);
    }
}
