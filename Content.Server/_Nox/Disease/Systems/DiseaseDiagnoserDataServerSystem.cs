// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Server._Nox.Disease.Components;
using Content.Shared._Nox.Disease.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Server.Power.EntitySystems;
using System.Linq;
using Content.Shared._Nox.Disease;
using Content.Shared.Interaction;
using Robust.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Database;
using Content.Server.Popups;
using Content.Server.Research.Disk;
using Content.Shared._Nox.TimeWindow;

namespace Content.Server._Nox.Disease.Systems;

public sealed class DiseaseDiagnoserDataServerSystem : EntitySystem
{
    [Dependency] private readonly DiseaseDiagnoserConsoleSystem _console = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DiseaseEvolutionConsoleSystem _evolutionConsoleSystem = default!;
    [Dependency] private readonly TimedWindowSystem _timedWindowSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseDiagnoserDataServerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DiseaseDiagnoserDataServerComponent, AnchorStateChangedEvent>(OnAnchor);
        SubscribeLocalEvent<DiseaseDiagnoserDataServerComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<DiseaseDiagnoserDataServerComponent, GetVerbsEvent<Verb>>(DoSetObeliskVerbs);
        SubscribeLocalEvent<DiseaseDiagnoserDataServerComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
    }

    private void OnAfterInteractUsing(EntityUid uid, DiseaseDiagnoserDataServerComponent component, AfterInteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach)
            return;

        if (!TryComp<ResearchDiskComponent>(args.Used, out var diskComponent))
            return;

        AddPoints((uid, component), diskComponent.Points);
        _popupSystem.PopupEntity(
            Loc.GetString("research-disk-inserted", ("points", diskComponent.Points)),
            uid,
            args.User);

        QueueDel(args.Used);
        args.Handled = true;
    }

    private void OnInit(EntityUid uid, DiseaseDiagnoserDataServerComponent component, ComponentInit args)
    {
        _timedWindowSystem.Reset(component.UpdateWindow);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiseaseDiagnoserDataServerComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_timedWindowSystem.IsExpired(component.UpdateWindow))
            {
                _timedWindowSystem.Reset(component.UpdateWindow);
                UpdateServer(uid, component);
            }
        }
    }

    private void UpdateServer(EntityUid uid, DiseaseDiagnoserDataServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var totalPoints = 0;
        foreach (var data in component.StrainData.Values)
        {
            totalPoints += data.ActiveSymptom.Count * component.SymptomsPointsMultiply;
            totalPoints += data.BodyWhitelist.Count * component.BodyPointsMultiply;
        }

        // UpdateConnectedInterfaces не делаем в Update(float frameTime), интерфейс не адаптирован под это
        component.Points += totalPoints;
    }

    public void UpdateConnectedInterfaces(EntityUid uid, DiseaseDiagnoserDataServerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.ConnectedConsole == null || !TryComp<DiseaseDiagnoserConsoleComponent>(component.ConnectedConsole, out var console))
            return;

        _console.UpdateUserInterface((component.ConnectedConsole.Value, console));

        if (component.ConnectedEvolutionConsole == null || !TryComp<DiseaseEvolutionConsoleComponent>(component.ConnectedEvolutionConsole, out var evolutionConsole))
            return;

        _evolutionConsoleSystem.UpdateUserInterface((component.ConnectedEvolutionConsole.Value, evolutionConsole));
    }

    private void DoSetObeliskVerbs(Entity<DiseaseDiagnoserDataServerComponent> server, ref GetVerbsEvent<Verb> args)
    {
        if (server.Comp.Points <= 0)
            return;

        AddDiskVerb(server, ref args, 1000);
        AddDiskVerb(server, ref args, 5000);
        AddDiskVerb(server, ref args, 10000);
    }

    private void AddDiskVerb(
    Entity<DiseaseDiagnoserDataServerComponent> server,
    ref GetVerbsEvent<Verb> args,
    int requestedPoints)
    {
        if (server.Comp.Points <= 0)
            return;

        var actualPoints = Math.Min(server.Comp.Points, requestedPoints);

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString(
                "disease-data-server-get-disk-verb-text",
                ("value", actualPoints)),
            Icon = new SpriteSpecifier.Texture(
                new("/Textures/Interface/VerbIcons/dot.svg.192dpi.png")),
            Act = () =>
            {
                if (server.Comp.Points <= 0)
                    return;

                var pointsToTake = Math.Min(server.Comp.Points, requestedPoints);

                var diskEnt = Spawn(server.Comp.Disk, Transform(server).Coordinates);

                if (!TryComp<ResearchDiskComponent>(diskEnt, out var diskComp))
                {
                    Del(diskEnt);
                    return;
                }

                diskComp.Points = pointsToTake;
                server.Comp.Points -= pointsToTake;

                if (server.Comp.ConnectedEvolutionConsole != null &&
                    TryComp<DiseaseEvolutionConsoleComponent>(
                        server.Comp.ConnectedEvolutionConsole,
                        out var evolutionConsole))
                {
                    UpdateConnectedInterfaces(server, server.Comp);
                }
            },
            Impact = LogImpact.Medium
        });
    }

    private void OnPortDisconnected(Entity<DiseaseDiagnoserDataServerComponent> server, ref PortDisconnectedEvent args)
    {
        if (args.Port == server.Comp.DiseaseDiagnoserDataServerPort)
            server.Comp.ConnectedConsole = null;
    }

    private void OnAnchor(Entity<DiseaseDiagnoserDataServerComponent> server, ref AnchorStateChangedEvent args)
    {
        if (server.Comp.ConnectedConsole != null && TryComp<DiseaseDiagnoserConsoleComponent>(server.Comp.ConnectedConsole, out var console))
        {

            if (args.Anchored)
            {
                _console.RecheckConnections((server.Comp.ConnectedConsole.Value, console));
                return;
            }

            _console.UpdateUserInterface((server.Comp.ConnectedConsole.Value, console));
        }

        if (server.Comp.ConnectedEvolutionConsole != null && TryComp<DiseaseEvolutionConsoleComponent>(server.Comp.ConnectedEvolutionConsole, out var evolutionConsole))
        {

            if (args.Anchored)
            {
                _evolutionConsoleSystem.RecheckConnections((server.Comp.ConnectedEvolutionConsole.Value, evolutionConsole));
                return;
            }

            _evolutionConsoleSystem.UpdateUserInterface((server.Comp.ConnectedEvolutionConsole.Value, evolutionConsole));
        }
    }

    public void AddPoints(Entity<DiseaseDiagnoserDataServerComponent?> server, int points)
    {
        if (!Resolve(server, ref server.Comp, false))
            return;

        server.Comp.Points += points;

        if (server.Comp.ConnectedConsole == null || !TryComp<DiseaseDiagnoserConsoleComponent>(server.Comp.ConnectedConsole, out var console))
            return;

        UpdateConnectedInterfaces(server, server.Comp);
    }

    public void SaveData(Entity<DiseaseDiagnoserDataServerComponent?> server, DiseaseData data)
    {
        if (!Resolve(server, ref server.Comp, false))
            return;

        if (!_powerReceiverSystem.IsPowered(server))
            return;

        var timeFormatted = _timing.CurTime.ToString(@"hh\:mm\:ss");

        // ищем существующую запись с таким StrainId
        var existingKey = server.Comp.StrainData.Keys
            .FirstOrDefault(x => x.Strain == data.StrainId);

        if (existingKey.Strain != null)
            server.Comp.StrainData.Remove(existingKey);

        var record = new DiseaseStrainRecord(
            data.StrainId,
            timeFormatted
        );

        server.Comp.StrainData[record] = (DiseaseData)data.Clone();
    }

    public void DeleteData(Entity<DiseaseDiagnoserDataServerComponent?> server, string strainId)
    {
        if (!Resolve(server, ref server.Comp, false))
            return;

        if (!_powerReceiverSystem.IsPowered(server))
            return;

        var key = server.Comp.StrainData.Keys
            .FirstOrDefault(k => k.Strain == strainId);

        if (!key.Equals(default(DiseaseStrainRecord)))
            server.Comp.StrainData.Remove(key);
    }

    public DiseaseData? GetData(Entity<DiseaseDiagnoserDataServerComponent?> server, string strainId)
    {
        if (!Resolve(server, ref server.Comp, false))
            return null;

        if (!_powerReceiverSystem.IsPowered(server))
            return null;

        var entry = server.Comp.StrainData
                    .FirstOrDefault(kvp => kvp.Key.Strain == strainId);

        // Проверка: если ключ по умолчанию — значит ничего не найдено
        if (EqualityComparer<KeyValuePair<DiseaseStrainRecord, DiseaseData>>.Default.Equals(entry, default))
            return null;

        var data = entry.Value;
        return (DiseaseData)data.Clone();
    }

    public List<DiseaseStrainRecord> GetAllStrains(Entity<DiseaseDiagnoserDataServerComponent?> server)
    {
        if (!Resolve(server, ref server.Comp, false))
            return new List<DiseaseStrainRecord>();

        return server.Comp.StrainData.Keys.ToList();
    }


}
