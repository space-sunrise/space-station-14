// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Content.Shared._Sunrise.Disease;
using Content.Server._Sunrise.Disease.Components;
using Content.Shared._Sunrise.Disease.Components;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Disease.Prototypes;
using Content.Shared.Body.Prototypes;

namespace Content.Server._Sunrise.Disease.Systems;

public sealed class DiseaseEvolutionConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly DiseaseSolutionAnalyzerSystem _diseaseSolutionAnalyzer = default!;
    [Dependency] private readonly DiseaseSystem _diseaseSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseEvolutionConsoleComponent, EvolutionConsoleUiButtonPressedMessage>(OnButtonPressed);
        SubscribeLocalEvent<DiseaseEvolutionConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
        SubscribeLocalEvent<DiseaseEvolutionConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<DiseaseEvolutionConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DiseaseEvolutionConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<DiseaseEvolutionConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<DiseaseEvolutionConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnButtonPressed(EntityUid uid, DiseaseEvolutionConsoleComponent component, EvolutionConsoleUiButtonPressedMessage args)
    {
        if (!_powerReceiverSystem.IsPowered(uid))
            return;

        if (component.DiseaseSolutionAnalyzer == null)
            return;

        if (!TryComp<DiseaseSolutionAnalyzerComponent>(component.DiseaseSolutionAnalyzer, out var analyzer))
            return;

        if (component.DiseaseDiagnoserDataServer == null
            || !TryComp<DiseaseDiagnoserDataServerComponent>(component.DiseaseDiagnoserDataServer, out var server))
            return;

        DiseaseData? diseaseData = null;

        if (_diseaseSolutionAnalyzer.TryGetDiseaseDataFromContainer(
                component.DiseaseSolutionAnalyzer.Value,
                out var diseaseDataList)
            && diseaseDataList != null)
        {
            var source = diseaseDataList.FirstOrDefault();
            // Скорее можно обойтись и без копии, но мне не хочется это проверять, ибо я уже обжигался
            diseaseData = source != null
                ? (DiseaseData)source.Clone()
                : null;
        }

        switch (args.Button)
        {
            case EvolutionConsoleUiButton.EvolutionSymptom:
                {
                    if (args.Symptom == null
                        || !_prototypeManager.TryIndex<DiseaseSymptomPrototype>(args.Symptom, out _)
                        || diseaseData == null)
                        return;

                    var price = _diseaseSystem.GetSymptomPrice(diseaseData, args.Symptom);
                    if (server.Points < price)
                        return;

                    server.Points -= price;

                    _diseaseSolutionAnalyzer.AddSymptom((component.DiseaseSolutionAnalyzer.Value, analyzer), args.Symptom);
                    break;
                }
            case EvolutionConsoleUiButton.EvolutionBody:
                {
                    if (args.Body == null
                        || !_prototypeManager.TryIndex<BodyPrototype>(args.Body, out _)
                        || diseaseData == null)
                        return;

                    var price = _diseaseSystem.GetBodyPrice(diseaseData);
                    if (server.Points < price)
                        return;

                    server.Points -= price;

                    _diseaseSolutionAnalyzer.AddBody((component.DiseaseSolutionAnalyzer.Value, analyzer), args.Body);
                    break;
                }
            case EvolutionConsoleUiButton.DeleteSymptom:
                {
                    if (args.Symptom == null
                        || !_prototypeManager.TryIndex<DiseaseSymptomPrototype>(args.Symptom, out _)
                        || diseaseData == null)
                        return;

                    var price = _diseaseSystem.GetSymptomDeletePrice(diseaseData.MultiPriceDeleteSymptom);
                    if (server.Points < price)
                        return;

                    server.Points -= price;

                    _diseaseSolutionAnalyzer.RemSymptom((component.DiseaseSolutionAnalyzer.Value, analyzer), args.Symptom);
                    break;
                }
            case EvolutionConsoleUiButton.DeleteBody:
                {
                    if (args.Body == null
                        || !_prototypeManager.TryIndex<BodyPrototype>(args.Body, out _)
                        || diseaseData == null)
                        return;

                    var price = _diseaseSystem.GetBodyDeletePrice();
                    if (server.Points < price)
                        return;

                    server.Points -= price;

                    _diseaseSolutionAnalyzer.RemBody((component.DiseaseSolutionAnalyzer.Value, analyzer), args.Body);
                    break;
                }
            default:
                break;
        }

        UpdateUserInterface((uid, component));
    }

    private void OnPowerChanged(EntityUid uid, DiseaseEvolutionConsoleComponent component, ref PowerChangedEvent args)
    {
        RecheckConnections((uid, component));
    }

    private void OnMapInit(EntityUid uid, DiseaseEvolutionConsoleComponent component, MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var receiver))
            return;

        foreach (var port in receiver.Outputs.Values.SelectMany(ports => ports))
        {
            if (TryComp<DiseaseSolutionAnalyzerComponent>(port, out var solutionAnalyzer))
            {
                component.DiseaseSolutionAnalyzer = port;
                solutionAnalyzer.ConnectedConsole = uid;
            }

            if (TryComp<DiseaseDiagnoserDataServerComponent>(port, out var server))
            {
                component.DiseaseDiagnoserDataServer = port;
                server.ConnectedEvolutionConsole = uid;
            }
        }
    }

    private void OnNewLink(EntityUid uid, DiseaseEvolutionConsoleComponent component, NewLinkEvent args)
    {
        if (TryComp<DiseaseDiagnoserDataServerComponent>(args.Sink, out var server) && args.SourcePort == component.DiseaseDiagnoserDataServerPort)
        {
            component.DiseaseDiagnoserDataServer = args.Sink;
            server.ConnectedConsole = uid;
        }

        if (TryComp<DiseaseSolutionAnalyzerComponent>(args.Sink, out var solutionAnalyzer) && args.SourcePort == component.DiseaseSolutionAnalyzerPort)
        {
            component.DiseaseSolutionAnalyzer = args.Sink;
            solutionAnalyzer.ConnectedConsole = uid;
        }

        RecheckConnections((uid, component));
    }

    private void OnPortDisconnected(Entity<DiseaseEvolutionConsoleComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port == ent.Comp.DiseaseSolutionAnalyzerPort)
            ent.Comp.DiseaseSolutionAnalyzer = null;

        if (args.Port == ent.Comp.DiseaseDiagnoserDataServerPort)
            ent.Comp.DiseaseDiagnoserDataServer = null;

        UpdateUserInterface((ent, ent.Comp));
    }

    private void OnUIOpen(EntityUid uid, DiseaseEvolutionConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        RecheckConnections((uid, component));
    }

    private void OnAnchorChanged(EntityUid uid, DiseaseEvolutionConsoleComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            RecheckConnections((uid, component));
            return;
        }

        RecheckConnections((uid, component));
    }

    public void UpdateUserInterface(Entity<DiseaseEvolutionConsoleComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (!TryComp<UserInterfaceComponent>(entity, out var userInterface))
            return;

        if (!_uiSystem.HasUi(entity, DiseaseEvolutionConsoleUiKey.Key, userInterface))
            return;

        if (!_powerReceiverSystem.IsPowered(entity))
        {
            _uiSystem.CloseUis((entity, userInterface));
            return;
        }

        var newState = GetUserInterfaceState((entity, entity.Comp));
        _uiSystem.SetUiState((entity, userInterface), DiseaseEvolutionConsoleUiKey.Key, newState);
    }

    public void RecheckConnections(Entity<DiseaseEvolutionConsoleComponent?> console)
    {
        if (!Resolve(console, ref console.Comp, false))
            return;

        var distance = 0f;
        if (console.Comp.DiseaseDiagnoserDataServer != null)
        {
            Transform(console.Comp.DiseaseDiagnoserDataServer.Value).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out distance);
            console.Comp.DataServerInRange = distance <= console.Comp.MaxDistanceForDataServer;
        }
        if (console.Comp.DiseaseSolutionAnalyzer != null)
        {
            Transform(console.Comp.DiseaseSolutionAnalyzer.Value).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out distance);
            console.Comp.SolutionAnalyzerInRange = distance <= console.Comp.MaxDistanceForOther;
        }

        UpdateUserInterface((console, console.Comp));
    }

    private DiseaseEvolutionConsoleBoundUserInterfaceState GetUserInterfaceState(Entity<DiseaseEvolutionConsoleComponent?> console)
    {
        if (!Resolve(console, ref console.Comp, false))
            return default!;

        DiseaseData? diseaseData = null;

        int points = 0;

        var dataServerConnected = console.Comp.DiseaseDiagnoserDataServer != null;
        var solutionAnalyzerConnected = console.Comp.DiseaseSolutionAnalyzer != null;

        if (console.Comp.DiseaseSolutionAnalyzer != null &&
            _diseaseSolutionAnalyzer.TryGetDiseaseDataFromContainer(console.Comp.DiseaseSolutionAnalyzer.Value, out var diseaseDataList))
        {
            var source = diseaseDataList.FirstOrDefault();
            // Скорее можно обойтись и без копии, но мне не хочется это проверять, ибо я уже обжигался
            diseaseData = source != null
                ? (DiseaseData)source.Clone()
                : null;
        }

        if (console.Comp.DiseaseDiagnoserDataServer != null &&
            TryComp<DiseaseDiagnoserDataServerComponent>(console.Comp.DiseaseDiagnoserDataServer.Value, out var server))
        {
            points = server.Points;
        }

        return new DiseaseEvolutionConsoleBoundUserInterfaceState(
            points,
            diseaseData?.MultiPriceDeleteSymptom ?? 0,
            dataServerConnected,
            solutionAnalyzerConnected,
            console.Comp.DataServerInRange,
            console.Comp.SolutionAnalyzerInRange,
            diseaseData != null,
            diseaseData?.ActiveSymptom,
            diseaseData?.BodyWhitelist,
            isSentientDisease: false
        );
    }


}

