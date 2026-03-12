using Content.Server.Popups;
using Content.Shared._Sunrise.Biocode;
using Content.Shared.Medical;

namespace Content.Server._Sunrise.Biocode.Systems;

/// <summary>
/// System that handles biocode checks for defibrillators.
/// </summary>
public sealed class BiocodeDefibrillatorSystem : EntitySystem
{
    [Dependency] private readonly BiocodeSystem _biocode = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiocodeComponent, SunriseCanZapEvent>(OnCanZap);
    }

    private void OnCanZap(EntityUid uid, BiocodeComponent component, ref SunriseCanZapEvent args)
    {
        if (args.User == null)
            return;

        if (_biocode.CanUse(args.User.Value, component.Factions))
            return;

        // User is not authorized, cancel the zap
        if (!string.IsNullOrEmpty(component.AlertText))
            _popup.PopupEntity(Loc.GetString(component.AlertText), uid, args.User.Value);

        args.Cancelled = true;
    }
}

