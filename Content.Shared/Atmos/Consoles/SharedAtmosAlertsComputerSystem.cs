using Content.Shared.Atmos.Components;
using Content.Shared.Verbs; // Sunrise-edit

namespace Content.Shared.Atmos.Consoles;

public abstract partial class SharedAtmosAlertsComputerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AtmosAlertsComputerComponent, AtmosAlertsComputerDeviceSilencedMessage>(OnDeviceSilencedMessage);
        SubscribeLocalEvent<AtmosAlertsComputerComponent, AtmosAlertsComputerAlertSoundToggleMessage>(OnAlertSoundToggleMessage); // Sunrise-edit
        SubscribeLocalEvent<AtmosAlertsComputerComponent, GetVerbsEvent<InteractionVerb>>(AddToggleVerb); // Sunrise-edit
    }

    private void OnDeviceSilencedMessage(EntityUid uid, AtmosAlertsComputerComponent component, AtmosAlertsComputerDeviceSilencedMessage args)
    {
        if (args.SilenceDevice)
            component.SilencedDevices.Add(args.AtmosDevice);

        else
            component.SilencedDevices.Remove(args.AtmosDevice);

        Dirty(uid, component);
    }

    // Sunrise-start
    private void OnAlertSoundToggleMessage(Entity<AtmosAlertsComputerComponent> ent, ref AtmosAlertsComputerAlertSoundToggleMessage args)
    {
        ent.Comp.DoAtmosAlert = args.Enabled;
        Dirty(ent);
    }

    private void AddToggleVerb(Entity<AtmosAlertsComputerComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var verb = new InteractionVerb
        {
            Text = Loc.GetString(ent.Comp.DoAtmosAlert
                ? "item-toggle-deactivate-alert"
                : "item-toggle-activate-alert"),

            Act = () => ToggleAlert(ent),
        };

        args.Verbs.Add(verb);
    }

    private void ToggleAlert(Entity<AtmosAlertsComputerComponent> ent)
    {
        ent.Comp.DoAtmosAlert = !ent.Comp.DoAtmosAlert;
        Dirty(ent);
    }

    // Sunrise-end
}
