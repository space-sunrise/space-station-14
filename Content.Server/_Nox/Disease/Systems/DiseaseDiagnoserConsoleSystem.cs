// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using System.Linq;
using Content.Server.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Content.Shared._Nox.Disease;
using Content.Server._Nox.Disease.Components;
using Content.Shared._Nox.Disease.Components;

namespace Content.Server._Nox.Disease.Systems;

public sealed class DiseaseDiagnoserConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly DiseaseDiagnoserDataServerSystem _dataServer = default!;
    [Dependency] private readonly DiseaseDiagnoserSystem _diagnoser = default!;
    [Dependency] private readonly DiseaseSolutionAnalyzerSystem _diseaseSolutionAnalyzer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseDiagnoserConsoleComponent, UiButtonPressedMessage>(OnButtonPressed);
        SubscribeLocalEvent<DiseaseDiagnoserConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
        SubscribeLocalEvent<DiseaseDiagnoserConsoleComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<DiseaseDiagnoserConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DiseaseDiagnoserConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<DiseaseDiagnoserConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<DiseaseDiagnoserConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnButtonPressed(EntityUid uid, DiseaseDiagnoserConsoleComponent component, UiButtonPressedMessage args)
    {
        if (!_powerReceiverSystem.IsPowered(uid))
            return;

        var dataServer = new DiseaseDiagnoserDataServerComponent();
        var diagnoser = new DiseaseDiagnoserComponent();

        switch (args.Button)
        {
            case UiButton.StartAnalys:
                {
                    if (component.DiseaseSolutionAnalyzer == null || !TryComp<DiseaseSolutionAnalyzerComponent>(component.DiseaseSolutionAnalyzer, out var diseaseSolutionAnalyzer))
                        return;

                    _diseaseSolutionAnalyzer.StartScanDisease((component.DiseaseSolutionAnalyzer.Value, diseaseSolutionAnalyzer));
                    break;
                }
            case UiButton.DeleteData:
                {
                    if (component.DiseaseDiagnoserDataServer == null || !TryComp(component.DiseaseDiagnoserDataServer, out dataServer))
                        return;

                    if (string.IsNullOrEmpty(args.Strain))
                        return;

                    _dataServer.DeleteData((component.DiseaseDiagnoserDataServer.Value, dataServer), args.Strain);
                    break;
                }
            case UiButton.GenerateDisease:
                {
                    if (component.DiseaseDiagnoser == null || !TryComp(component.DiseaseDiagnoser, out diagnoser))
                        return;

                    if (component.DiseaseDiagnoserDataServer == null || !TryComp(component.DiseaseDiagnoserDataServer, out dataServer))
                        return;

                    if (string.IsNullOrEmpty(args.Strain))
                        return;

                    DiseaseData? data = _dataServer.GetData((component.DiseaseDiagnoserDataServer.Value, dataServer), args.Strain);

                    _diagnoser.StartGenerateDisease((component.DiseaseDiagnoser.Value, diagnoser), data);
                    break;
                }
            case UiButton.PrintReport:
                {
                    if (component.DiseaseDiagnoser == null || !TryComp(component.DiseaseDiagnoser, out diagnoser))
                        return;

                    if (component.DiseaseDiagnoserDataServer == null || !TryComp(component.DiseaseDiagnoserDataServer, out dataServer))
                        return;

                    if (String.IsNullOrEmpty(args.Strain))
                        return;

                    DiseaseData? data = _dataServer.GetData((component.DiseaseDiagnoserDataServer.Value, dataServer), args.Strain);

                    _diagnoser.StartPrinting((component.DiseaseDiagnoser.Value, diagnoser), data);
                    break;
                }
            case UiButton.ScanDisease:
                {
                    if (component.DiseaseDiagnoser == null || !TryComp(component.DiseaseDiagnoser, out diagnoser))
                        return;

                    _diagnoser.StartScanDisease((component.DiseaseDiagnoser.Value, diagnoser));
                    break;
                }
            default:
                break;
        }
        UpdateUserInterface((uid, component));
    }

    private void OnPowerChanged(EntityUid uid, DiseaseDiagnoserConsoleComponent component, ref PowerChangedEvent args)
    {
        RecheckConnections((uid, component));
    }

    private void OnMapInit(EntityUid uid, DiseaseDiagnoserConsoleComponent component, MapInitEvent args)
    {
        if (!TryComp<DeviceLinkSourceComponent>(uid, out var receiver))
            return;

        foreach (var port in receiver.Outputs.Values.SelectMany(ports => ports))
        {
            if (TryComp<DiseaseDiagnoserComponent>(port, out var diagnoser))
            {
                component.DiseaseDiagnoser = port;
                diagnoser.ConnectedConsole = uid;
            }

            if (TryComp<DiseaseDiagnoserDataServerComponent>(port, out var server))
            {
                component.DiseaseDiagnoserDataServer = port;
                server.ConnectedConsole = uid;
            }

            if (TryComp<DiseaseSolutionAnalyzerComponent>(port, out var solutionAnalyzer))
            {
                component.DiseaseSolutionAnalyzer = port;
                solutionAnalyzer.ConnectedConsole = uid;
            }
        }
    }

    private void OnNewLink(EntityUid uid, DiseaseDiagnoserConsoleComponent component, NewLinkEvent args)
    {
        if (TryComp<DiseaseDiagnoserComponent>(args.Sink, out var diagnoser) && args.SourcePort == component.DiseaseDiagnoserPort)
        {
            component.DiseaseDiagnoser = args.Sink;
            diagnoser.ConnectedConsole = uid;
        }

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

    private void OnPortDisconnected(Entity<DiseaseDiagnoserConsoleComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port == ent.Comp.DiseaseDiagnoserPort)
            ent.Comp.DiseaseDiagnoser = null;

        if (args.Port == ent.Comp.DiseaseSolutionAnalyzerPort)
            ent.Comp.DiseaseSolutionAnalyzer = null;

        if (args.Port == ent.Comp.DiseaseDiagnoserDataServerPort)
            ent.Comp.DiseaseDiagnoserDataServer = null;

        UpdateUserInterface((ent, ent.Comp));
    }

    private void OnUIOpen(EntityUid uid, DiseaseDiagnoserConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        RecheckConnections((uid, component));
    }

    private void OnAnchorChanged(EntityUid uid, DiseaseDiagnoserConsoleComponent component, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
        {
            RecheckConnections((uid, component));
            return;
        }

        RecheckConnections((uid, component));
    }

    public void UpdateUserInterface(Entity<DiseaseDiagnoserConsoleComponent?> console)
    {
        if (!Resolve(console, ref console.Comp, false))
            return;

        if (!TryComp<UserInterfaceComponent>(console, out var userInterface))
            return;

        if (!_uiSystem.HasUi(console, DiseaseDiagnoserConsoleUiKey.Key, userInterface))
            return;

        if (!_powerReceiverSystem.IsPowered(console))
        {
            _uiSystem.CloseUis((console, userInterface));
            return;
        }

        var newState = GetUserInterfaceState((console, console.Comp));
        _uiSystem.SetUiState((console, userInterface), DiseaseDiagnoserConsoleUiKey.Key, newState);
    }

    public void RecheckConnections(Entity<DiseaseDiagnoserConsoleComponent?> console)
    {
        if (!Resolve(console, ref console.Comp, false))
            return;

        var distance = 0f;

        if (console.Comp.DiseaseDiagnoser != null)
        {
            Transform(console.Comp.DiseaseDiagnoser.Value).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out distance);
            console.Comp.DiagnoserInRange = distance <= console.Comp.MaxDistanceForOther;
        }
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

    private DiseaseDiagnoserConsoleBoundUserInterfaceState GetUserInterfaceState(Entity<DiseaseDiagnoserConsoleComponent?> console)
    {
        if (!Resolve(console, ref console.Comp, false))
            return default!;

        DiseaseDiagnoserDataServerComponent? dataServer = null;

        List<DiseaseStrainRecord> strains;

        if (console.Comp.DiseaseDiagnoserDataServer != null &&
            TryComp(console.Comp.DiseaseDiagnoserDataServer, out dataServer))
        {
            strains = _dataServer.GetAllStrains((console.Comp.DiseaseDiagnoserDataServer.Value, dataServer));
        }
        else
        {
            strains = new List<DiseaseStrainRecord>();
        }

        var points = dataServer?.Points ?? 0;

        var diagnoserConnected = console.Comp.DiseaseDiagnoser != null;
        var dataServerConnected = console.Comp.DiseaseDiagnoserDataServer != null;
        var solutionAnalyzerConnected = console.Comp.DiseaseSolutionAnalyzer != null;

        return new DiseaseDiagnoserConsoleBoundUserInterfaceState(
            strains,
            points,
            diagnoserConnected,
            dataServerConnected,
            solutionAnalyzerConnected,
            console.Comp.DiagnoserInRange,
            console.Comp.DataServerInRange,
            console.Comp.SolutionAnalyzerInRange
        );
    }


}

