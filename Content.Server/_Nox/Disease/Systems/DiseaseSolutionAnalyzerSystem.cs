// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Robust.Server.Audio;
using Content.Shared.Examine;
using Robust.Shared.Containers;
using Content.Server._Nox.Disease.Components;
using Content.Shared.DeviceLinking.Events;
using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared._Nox.Disease.Components;
using Robust.Server.GameObjects;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared._Nox.Disease;
using Robust.Shared.Prototypes;
using Content.Shared._Nox.Disease.Prototypes;
using Content.Shared.Body.Prototypes;

namespace Content.Server._Nox.Disease.Systems;

public sealed class DiseaseSolutionAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DiseaseDiagnoserConsoleSystem _console = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly DiseaseDiagnoserDataServerSystem _dataServer = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DiseaseEvolutionConsoleSystem _evolutionConsoleSystem = default!;
    private const string FlaskContainerKey = "flask_container_disease_solution_analyzer";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseSolutionAnalyzerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<DiseaseSolutionAnalyzerComponent, AnchorStateChangedEvent>(OnAnchor);
        SubscribeLocalEvent<DiseaseSolutionAnalyzerComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<DiseaseSolutionAnalyzerComponent, EntInsertedIntoContainerMessage>(OnEntInsertCont);
        SubscribeLocalEvent<DiseaseSolutionAnalyzerComponent, EntRemovedFromContainerMessage>(OnEntRemoveCont);
    }

    private void OnEntInsertCont(Entity<DiseaseSolutionAnalyzerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateContainerAppearance((ent, ent.Comp));
    }

    private void OnEntRemoveCont(Entity<DiseaseSolutionAnalyzerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateContainerAppearance((ent, ent.Comp));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DiseaseSolutionAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_powerReceiverSystem.IsPowered(uid))
            {
                SetStatus((uid, comp), DiseaseSolutionAnalyzerStatus.Off);
                continue; // без питания ничего не делаем
            }

            // Если был выключен — включаем
            if (comp.Status == DiseaseSolutionAnalyzerStatus.Off)
                SetStatus((uid, comp), DiseaseSolutionAnalyzerStatus.On);

            if (EntityManager.EntityExists(comp.CurrentSoundEntity))
                continue;

            switch (comp.Status)
            {
                case DiseaseSolutionAnalyzerStatus.Scanning:
                    if (!CanScanning((uid, comp)))
                    {
                        SetStatus((uid, comp), DiseaseSolutionAnalyzerStatus.Denial);
                        break;
                    }

                    EndScanDisease((uid, comp));
                    break;

                case DiseaseSolutionAnalyzerStatus.Denial:
                    SetStatus((uid, comp), DiseaseSolutionAnalyzerStatus.On);
                    break;

                case DiseaseSolutionAnalyzerStatus.Successfully:
                    SetStatus((uid, comp), DiseaseSolutionAnalyzerStatus.On);
                    break;

                case DiseaseSolutionAnalyzerStatus.On:
                default:
                    break;
            }

        }
    }

    private void OnPortDisconnected(Entity<DiseaseSolutionAnalyzerComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port == ent.Comp.DiseaseSolutionAnalyzerPort)
            ent.Comp.ConnectedConsole = null;
    }

    private void OnAnchor(Entity<DiseaseSolutionAnalyzerComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (ent.Comp.ConnectedConsole != null && TryComp<DiseaseDiagnoserConsoleComponent>(ent.Comp.ConnectedConsole, out var console))
        {

            if (args.Anchored)
            {
                _console.RecheckConnections((ent.Comp.ConnectedConsole.Value, console));
                return;
            }

            _console.UpdateUserInterface((ent.Comp.ConnectedConsole.Value, console));
        }

        if (ent.Comp.ConnectedEvolutionConsole != null && TryComp<DiseaseEvolutionConsoleComponent>(ent.Comp.ConnectedEvolutionConsole, out var evolutionConsole))
        {

            if (args.Anchored)
            {
                _evolutionConsoleSystem.RecheckConnections((ent.Comp.ConnectedEvolutionConsole.Value, evolutionConsole));
                return;
            }

            _evolutionConsoleSystem.UpdateUserInterface((ent.Comp.ConnectedEvolutionConsole.Value, evolutionConsole));
        }
    }

    private void OnExamine(EntityUid uid, DiseaseSolutionAnalyzerComponent component, ExaminedEvent args)
    {
        BaseContainer? container = default!;

        if (_container.TryGetContainer(uid, FlaskContainerKey, out container))
        {
            if (container is ContainerSlot slot)
            {
                if (slot.ContainedEntity != null)
                    args.PushMarkup(Loc.GetString("disease-diagnoser-flask-attached"));
            }
        }
    }

    public void StartScanDisease(Entity<DiseaseSolutionAnalyzerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!CanScanning((ent, ent.Comp)))
        {
            SetStatus((ent, ent.Comp), DiseaseSolutionAnalyzerStatus.Denial);
            return;
        }

        SetStatus((ent, ent.Comp), DiseaseSolutionAnalyzerStatus.Scanning);
    }

    private void EndScanDisease(Entity<DiseaseSolutionAnalyzerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        SetStatus((ent, ent.Comp), DiseaseSolutionAnalyzerStatus.Successfully);

        if (ent.Comp.ConnectedConsole == null ||
            !TryComp<DiseaseDiagnoserConsoleComponent>(
                ent.Comp.ConnectedConsole,
                out var console))
            return;

        if (!TryGetDiseaseDataFromContainer(ent, out var diseaseData))
            return;

        if (!TryComp<DiseaseDiagnoserDataServerComponent>(
                console.DiseaseDiagnoserDataServer,
                out var server))
            return;

        foreach (var data in diseaseData)
        {
            _dataServer.SaveData(
                (console.DiseaseDiagnoserDataServer.Value, server),
                data);
        }

        _console.UpdateUserInterface(
            (ent.Comp.ConnectedConsole.Value, console));
    }


    public bool TryGetDiseaseDataFromContainer(
    EntityUid owner,
    out List<DiseaseData> diseaseData)
    {
        diseaseData = new();

        if (!_container.TryGetContainer(owner, FlaskContainerKey, out var container))
            return false;

        if (container is not ContainerSlot slot)
            return false;

        if (slot.ContainedEntity is not { } contained)
            return false;

        if (!TryComp<SolutionContainerManagerComponent>(contained, out var solutionManager))
            return false;

        if (!TryComp<DrawableSolutionComponent>(contained, out var drawable))
            return false;

        var wrapper = new Entity<DrawableSolutionComponent?, SolutionContainerManagerComponent?>(
            contained,
            drawable,
            solutionManager);

        if (!_solutionContainer.TryGetDrawableSolution(
                wrapper,
                out _,
                out var solution))
            return false;

        if (solution == null || solution.Contents.Count == 0)
            return false;

        foreach (var reagent in solution.Contents)
        {
            var dataList = reagent.Reagent.Data;
            if (dataList == null)
                continue;

            foreach (var data in dataList.OfType<DiseaseData>())
            {
                diseaseData.Add(data);
            }
        }

        return diseaseData.Count > 0;
    }

    public void AddSymptom(Entity<DiseaseSolutionAnalyzerComponent?> console, string symptom)
    {
        if (!Resolve(console, ref console.Comp, false))
            return;

        if (console.Comp.Status != DiseaseSolutionAnalyzerStatus.On)
            return;

        SetStatus((console, console.Comp), DiseaseSolutionAnalyzerStatus.Successfully);

        if (_prototypeManager.Index<DiseaseSymptomPrototype>(symptom) == null)
            return;

        if (!TryGetDiseaseDataFromContainer(console, out var diseaseDataList))
            return;

        var diseaseData = diseaseDataList.FirstOrDefault();

        if (diseaseData == null)
            return;

        diseaseData.ActiveSymptom.Add(symptom);
    }

    public void AddBody(Entity<DiseaseSolutionAnalyzerComponent?> console, string body)
    {
        if (!Resolve(console, ref console.Comp, false))
            return;

        if (console.Comp.Status != DiseaseSolutionAnalyzerStatus.On)
            return;

        SetStatus((console, console.Comp), DiseaseSolutionAnalyzerStatus.Successfully);

        if (_prototypeManager.Index<BodyPrototype>(body) == null)
            return;

        if (!TryGetDiseaseDataFromContainer(console, out var diseaseDataList))
            return;

        var diseaseData = diseaseDataList.FirstOrDefault();

        if (diseaseData == null)
            return;

        diseaseData.BodyWhitelist.Add(body);
    }

    public void RemSymptom(Entity<DiseaseSolutionAnalyzerComponent?> console, string symptom)
    {
        if (!Resolve(console, ref console.Comp, false))
            return;

        if (console.Comp.Status != DiseaseSolutionAnalyzerStatus.On)
            return;

        SetStatus((console, console.Comp), DiseaseSolutionAnalyzerStatus.Successfully);

        if (_prototypeManager.Index<DiseaseSymptomPrototype>(symptom) == null)
            return;

        if (!TryGetDiseaseDataFromContainer(console, out var diseaseDataList))
            return;

        var diseaseData = diseaseDataList.FirstOrDefault();

        if (diseaseData == null)
            return;

        diseaseData.ActiveSymptom.Remove(symptom);
    }

    public void RemBody(Entity<DiseaseSolutionAnalyzerComponent?> console, string body)
    {
        if (!Resolve(console, ref console.Comp, false))
            return;

        if (console.Comp.Status != DiseaseSolutionAnalyzerStatus.On)
            return;

        SetStatus((console, console.Comp), DiseaseSolutionAnalyzerStatus.Successfully);

        if (_prototypeManager.Index<BodyPrototype>(body) == null)
            return;

        if (!TryGetDiseaseDataFromContainer(console, out var diseaseDataList))
            return;

        var diseaseData = diseaseDataList.FirstOrDefault();

        if (diseaseData == null)
            return;

        diseaseData.BodyWhitelist.Remove(body);
    }

    private void UpdateAppearance(Entity<DiseaseSolutionAnalyzerComponent> ent)
    {
        if (TryComp<AppearanceComponent>(ent, out var appearance))
            _appearance.SetData(ent, DiseaseSolutionAnalyzerVisuals.Status, ent.Comp.Status, appearance);
    }

    private void UpdateContainerAppearance(Entity<DiseaseSolutionAnalyzerComponent> ent)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        if (!_container.TryGetContainer(ent, FlaskContainerKey, out var flaskContainer) ||
            flaskContainer is not ContainerSlot slot ||
            slot.ContainedEntity == null)
        {
            _appearance.SetData(ent, DiseaseSolutionContainerAnalyzerVisuals.Status, DiseaseSolutionContainerAnalyzerStatus.Empty, appearance);
            return;
        }

        _appearance.SetData(ent, DiseaseSolutionContainerAnalyzerVisuals.Status, DiseaseSolutionContainerAnalyzerStatus.Fill, appearance);
    }


    private void SetStatus(Entity<DiseaseSolutionAnalyzerComponent?> ent, DiseaseSolutionAnalyzerStatus newStatus)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Status == newStatus)
            return;

        if (newStatus != DiseaseSolutionAnalyzerStatus.On)
            QueueDel(ent.Comp.CurrentSoundEntity);

        ent.Comp.CurrentSoundEntity = null;

        switch (newStatus)
        {
            case DiseaseSolutionAnalyzerStatus.On:
                break;
            case DiseaseSolutionAnalyzerStatus.Off:
                break;
            case DiseaseSolutionAnalyzerStatus.Scanning:
                ent.Comp.CurrentSoundEntity = _audio.PlayPvs(ent.Comp.ScanningSound, ent)?.Entity;
                break;
            case DiseaseSolutionAnalyzerStatus.Denial:
                ent.Comp.CurrentSoundEntity = _audio.PlayPvs(ent.Comp.DenialSound, ent)?.Entity;
                break;
            case DiseaseSolutionAnalyzerStatus.Successfully:
                ent.Comp.CurrentSoundEntity = _audio.PlayPvs(ent.Comp.SuccessfullySound, ent)?.Entity;
                break;
            default:

                break;
        }

        ent.Comp.Status = newStatus;

        UpdateAppearance((ent, ent.Comp));
    }

    public bool CanScanning(Entity<DiseaseSolutionAnalyzerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!_container.TryGetContainer(ent, FlaskContainerKey, out var flaskContainer))
            return false;

        if (flaskContainer is not ContainerSlot slot)
            return false;

        if (slot.ContainedEntity == null)
            return false;

        return true;
    }

}