using Content.Server._Sunrise.Research.TechnologyDisk.Components;
using Content.Server.Research.Systems;
using Content.Shared._Sunrise.Research.TechnologyDisk;
using Content.Shared.Research.Components;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Research.TechnologyDisk.Systems;

public sealed class SunriseDiskConsoleSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<SunriseDiskConsoleComponent>(SunriseDiskConsoleUiKey.Key, subs =>
        {
            subs.Event<SunriseDiskConsolePrintDiskMessage>(OnPrintDisk);
        });

        SubscribeLocalEvent<SunriseDiskConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeUiOpen);
        SubscribeLocalEvent<SunriseDiskConsoleComponent, ResearchServerPointsChangedEvent>(OnPointsChanged);
        SubscribeLocalEvent<SunriseDiskConsoleComponent, ResearchRegistrationChangedEvent>(OnRegistrationChanged);
        SubscribeLocalEvent<SunriseDiskConsolePrintingComponent, ComponentShutdown>(OnPrintingShutdown);
    }

    private void OnPrintDisk(Entity<SunriseDiskConsoleComponent> ent, ref SunriseDiskConsolePrintDiskMessage args)
    {
        TryPrintDisk(ent.AsNullable(), args.Prototype);
    }

    private void OnBeforeUiOpen(EntityUid uid, SunriseDiskConsoleComponent component, BeforeActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnPointsChanged(Entity<SunriseDiskConsoleComponent> ent, ref ResearchServerPointsChangedEvent args)
    {
        UpdateUserInterface(ent, ent.Comp);
    }

    private void OnRegistrationChanged(Entity<SunriseDiskConsoleComponent> ent, ref ResearchRegistrationChangedEvent args)
    {
        UpdateUserInterface(ent, ent.Comp);
    }

    private void OnPrintingShutdown(Entity<SunriseDiskConsolePrintingComponent> ent, ref ComponentShutdown args)
    {
        UpdateUserInterface(ent);
    }

    public bool TryPrintDisk(Entity<SunriseDiskConsoleComponent?> ent, EntProtoId prototype)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!CanPrintDisk((ent, ent.Comp), prototype, out var option, out var server, out var serverComp))
            return false;

        _research.ModifyServerPoints(server, -option.PointCost, serverComp);
        _audio.PlayPvs(ent.Comp.PrintSound, ent);

        var printing = EnsureComp<SunriseDiskConsolePrintingComponent>(ent);
        printing.FinishTime = _timing.CurTime + ent.Comp.PrintDuration;
        printing.DiskPrototype = option.Prototype;
        UpdateUserInterface(ent, ent.Comp);
        return true;
    }

    public bool CanPrintDisk(
        Entity<SunriseDiskConsoleComponent> ent,
        EntProtoId prototype,
        out SunriseDiskConsolePrintOption option,
        out EntityUid server,
        out ResearchServerComponent serverComp)
    {
        option = default!;
        server = default;
        serverComp = default!;

        if (HasComp<SunriseDiskConsolePrintingComponent>(ent))
            return false;

        var found = false;
        foreach (var diskOption in ent.Comp.DiskOptions)
        {
            if (diskOption.Prototype != prototype)
                continue;

            option = diskOption;
            found = true;
            break;
        }

        if (!found || option.PointCost <= 0)
            return false;

        if (!_research.TryGetClientServer(ent, out var nullableServer, out var nullableServerComp))
            return false;

        if (nullableServerComp.Points < option.PointCost)
            return false;

        server = nullableServer.Value;
        serverComp = nullableServerComp;
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SunriseDiskConsolePrintingComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var printing, out var xform))
        {
            if (printing.FinishTime > _timing.CurTime)
                continue;

            var prototype = printing.DiskPrototype;
            RemComp(uid, printing);
            Spawn(prototype, xform.Coordinates);
        }
    }

    public void UpdateUserInterface(EntityUid uid, SunriseDiskConsoleComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var totalPoints = 0;
        if (_research.TryGetClientServer(uid, out _, out var server))
            totalPoints = server.Points;

        var isPrinting = TryComp<SunriseDiskConsolePrintingComponent>(uid, out var printing) &&
                         printing.FinishTime >= _timing.CurTime;
        var state = new SunriseDiskConsoleBoundUserInterfaceState(totalPoints, component.DiskOptions, isPrinting);
        _ui.SetUiState(uid, SunriseDiskConsoleUiKey.Key, state);
    }
}
