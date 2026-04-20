using Content.Shared._Sunrise.CriminalRecords.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.TextScreen;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.CriminalRecords.Systems;

public sealed class PrisonTimerSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrisonTimerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PrisonTimerComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<PrisonTimerComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnStartup(EntityUid uid, PrisonTimerComponent component, ComponentStartup args)
    {
        _deviceLink.EnsureSinkPorts(uid, "PrisonTimerSet", "PrisonTimerReset");
    }

    private void OnUse(EntityUid uid, PrisonTimerComponent component, UseInHandEvent args)
    {
        if (component.Locked)
        {
            // Popup "Timer is locked by console"
            args.Handled = true;
        }
    }

    private void OnSignalReceived(EntityUid uid, PrisonTimerComponent component, ref SignalReceivedEvent args)
    {
        if (args.Port == "PrisonTimerSet")
        {
            component.Locked = true;
        }
        else if (args.Port == "PrisonTimerReset")
        {
            component.Locked = false;
            ResetTimer(uid);
        }
    }

    public void SetTimer(EntityUid uid, string label, TimeSpan duration)
    {
        UpdateScreenText(uid, label);
        _appearance.SetData(uid, TextScreenVisuals.TargetTime, _timing.CurTime + duration);
    }

    public void ResetTimer(EntityUid uid, string label = "")
    {
        if (string.IsNullOrEmpty(label))
        {
            _appearance.SetData(uid, TextScreenVisuals.ScreenText, string.Empty);
        }
        else
        {
            UpdateScreenText(uid, label, "OPEN");
        }

        _appearance.SetData(uid, TextScreenVisuals.TargetTime, TimeSpan.Zero);
    }

    private void UpdateScreenText(EntityUid uid, string line1, string line2 = "")
    {
        // We target RowLength = 6 (as defined in prison_cell.yml)
        const int rowLength = 6;
        
        var text = line1.PadRight(rowLength);
        if (!string.IsNullOrEmpty(line2))
            text += line2.PadRight(rowLength);
            
        _appearance.SetData(uid, TextScreenVisuals.ScreenText, text);
    }
}
