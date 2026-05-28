using Content.Server.Guardian;
using Content.Server.Popups;
using Content.Shared.Guardian;
using Content.Shared.Popups;
using Robust.Shared.Containers;

namespace Content.Server._Sunrise.Guardian;

public sealed class GuardianSelfToggleSystem : EntitySystem
{
    [Dependency] private readonly GuardianSystem _guardian = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GuardianComponent, GuardianToggleActionEvent>(OnToggleAction);
    }

    private void OnToggleAction(Entity<GuardianComponent> ent, ref GuardianToggleActionEvent args)
    {
        if (args.Handled)
            return;

        if (TryToggleGuardian(ent.AsNullable()))
            args.Handled = true;
    }

    public bool TryToggleGuardian(Entity<GuardianComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!CanToggleGuardian(ent, out var host, out var hostComponent))
            return false;

        DoToggleGuardian(host, hostComponent);
        return true;
    }

    public bool CanToggleGuardian(
        Entity<GuardianComponent?> ent,
        out EntityUid host,
        out GuardianHostComponent hostComponent,
        bool quiet = false)
    {
        host = default;
        hostComponent = default!;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.Host is not { Valid: true } hostUid || !Exists(hostUid))
            return false;

        if (!TryComp<GuardianHostComponent>(hostUid, out var foundHostComponent))
            return false;

        hostComponent = foundHostComponent;

        if (hostComponent.HostedGuardian != ent.Owner)
            return false;

        if (_container.IsEntityInContainer(hostUid))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("guardian-inside-container"), hostUid, ent.Owner, PopupType.MediumCaution);

            return false;
        }

        host = hostUid;
        return true;
    }

    private void DoToggleGuardian(EntityUid host, GuardianHostComponent hostComponent)
    {
        _guardian.ToggleGuardian(host, hostComponent);
    }
}
