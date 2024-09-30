using Content.Client.Alerts;
using Content.Client.UserInterface.Systems.Alerts.Controls;
using Content.Shared.Changeling;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Changeling;

public sealed partial class ChangelingSystem : EntitySystem
{

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, UpdateAlertSpriteEvent>(OnUpdateAlert);
        SubscribeLocalEvent<ChangelingComponent, GetStatusIconsEvent>(GetChanglingIcon);
    }

    private void OnUpdateAlert(EntityUid uid, ChangelingComponent comp, ref UpdateAlertSpriteEvent args)
    {
        var stateNormalized = 0f;
        var sprite = args.SpriteViewEnt.Comp;

        // hardcoded because uhh umm i don't know. send help.
        switch (args.Alert.AlertKey.AlertType)
        {
            case "ChangelingChemicals":
                stateNormalized = (int)(comp.Chemicals / comp.MaxChemicals * 18);
                break;

            case "ChangelingBiomass":
                stateNormalized = (int)(comp.Biomass / comp.MaxBiomass * 16);
                break;

            case "ChangelingHive":
                if (sprite.BaseRSI == null)
                    break;
                if (!sprite.BaseRSI.TryGetState($"{comp.Hive}", out _))
                    throw new Exception("testexception");
                sprite.LayerSetState(ChangelingHiveVisualLayers.Base, $"{comp.Hive}");
                return;

            default:
                return;
        }
        sprite.LayerSetState(AlertVisualLayers.Base, $"{stateNormalized}");
    }

    private void GetChanglingIcon(Entity<ChangelingComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<HivemindComponent>(ent) && _prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}
