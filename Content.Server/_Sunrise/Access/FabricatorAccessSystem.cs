using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Construction.Components;
using Content.Shared.Lathe;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Sunrise.Access;

/// <summary>
/// Добавляет настройку доступов производственным машинам и защищает их BUI от команд без доступа.
/// </summary>
public sealed class FabricatorAccessSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private static readonly SoundSpecifier AccessDeniedSound =
        new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LatheComponent, ComponentInit>(OnLatheInit);
        SubscribeLocalEvent<LatheComponent, ActivatableUIOpenAttemptEvent>(OnLatheUiOpenAttempt);
        SubscribeLocalEvent<LatheComponent, BoundUserInterfaceMessageAttempt>(OnLatheUiMessageAttempt);

        SubscribeLocalEvent<FlatpackCreatorComponent, ComponentInit>(OnFlatpackerInit);
        SubscribeLocalEvent<FlatpackCreatorComponent, ActivatableUIOpenAttemptEvent>(OnFlatpackerUiOpenAttempt);
        SubscribeLocalEvent<FlatpackCreatorComponent, BoundUserInterfaceMessageAttempt>(OnFlatpackerUiMessageAttempt);
    }

    private void OnLatheInit(Entity<LatheComponent> ent, ref ComponentInit args)
    {
        EnsureAccessReader(ent);
    }

    private void OnFlatpackerInit(Entity<FlatpackCreatorComponent> ent, ref ComponentInit args)
    {
        EnsureAccessReader(ent);
    }

    private void OnLatheUiOpenAttempt(Entity<LatheComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (TryDenyUiOpen(ent, args.User, args.Silent))
            args.Cancel();
    }

    private void OnFlatpackerUiOpenAttempt(Entity<FlatpackCreatorComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (TryDenyUiOpen(ent, args.User, args.Silent))
            args.Cancel();
    }

    private void OnLatheUiMessageAttempt(Entity<LatheComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (TryDenyUiMessage(ent, LatheUiKey.Key, args))
            args.Cancel();
    }

    private void OnFlatpackerUiMessageAttempt(
        Entity<FlatpackCreatorComponent> ent,
        ref BoundUserInterfaceMessageAttempt args)
    {
        if (TryDenyUiMessage(ent, FlatpackCreatorUIKey.Key, args))
            args.Cancel();
    }

    public bool TryDenyUiOpen(EntityUid fabricator, EntityUid user, bool silent)
    {
        if (CanUseFabricator(fabricator, user))
            return false;

        if (!silent)
            PlayAccessDeniedSound(fabricator, user);

        return true;
    }

    public bool TryDenyUiMessage(
        EntityUid fabricator,
        Enum expectedUiKey,
        BoundUserInterfaceMessageAttempt args)
    {
        if (!Equals(args.UiKey, expectedUiKey) || CanUseFabricator(fabricator, args.Actor))
            return false;

        _ui.CloseUi((fabricator, null), args.UiKey, args.Actor);
        PlayAccessDeniedSound(fabricator, args.Actor);
        return true;
    }

    public bool CanUseFabricator(EntityUid fabricator, EntityUid user)
    {
        return _access.IsAllowed(user, fabricator);
    }

    private void EnsureAccessReader(EntityUid fabricator)
    {
        EnsureComp<AccessReaderComponent>(fabricator);
    }

    private void PlayAccessDeniedSound(EntityUid fabricator, EntityUid user)
    {
        _audio.PlayEntity(AccessDeniedSound, user, fabricator);
    }
}
