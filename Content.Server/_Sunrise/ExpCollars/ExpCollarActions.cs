using Content.Server.Wires;
using Content.Shared.Wires;

namespace Content.Server._Sunrise.ExpCollars;

public sealed partial class ExpCollarElectrocuteAction : ComponentWireAction<ExpCollarComponent>
{
    public override Color Color { get; set; } = Color.YellowGreen;
    public override string Name { get; set; } = "wire-name-exp-collar-electrocute";
    public override bool LightRequiresPower { get; set; } = false;

    public override StatusLightState? GetLightState(Wire wire, ExpCollarComponent comp)
    {
        return comp.Armed ? StatusLightState.On : StatusLightState.Off;
    }

    public override object? StatusKey { get; } = null;

    public override bool Cut(EntityUid user, Wire wire, ExpCollarComponent comp)
    {
        return EntityManager.System<ExpCollarsSystem>().Electrocute(user, wire, comp);
    }

    public override bool Mend(EntityUid user, Wire wire, ExpCollarComponent comp)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, ExpCollarComponent comp)
    {
        EntityManager.System<ExpCollarsSystem>().Electrocute(user, wire, comp);
    }
}

public sealed partial class ExpCollarArmAction : ComponentWireAction<ExpCollarComponent>
{
    public override Color Color { get; set; } = Color.DarkRed;
    public override string Name { get; set; } = "wire-name-exp-collar-arm";
    public override bool LightRequiresPower { get; set; } = false;

    public override StatusLightState? GetLightState(Wire wire, ExpCollarComponent comp)
    {
        return comp.Armed ? StatusLightState.On : StatusLightState.Off;
    }

    public override object? StatusKey { get; } = null;

    public override bool Cut(EntityUid user, Wire wire, ExpCollarComponent comp)
    {
        return EntityManager.System<ExpCollarsSystem>().Arm(user, wire, comp);
    }

    public override bool Mend(EntityUid user, Wire wire, ExpCollarComponent comp)
    {
        return EntityManager.System<ExpCollarsSystem>().Arm(user, wire, comp);
    }

    public override void Pulse(EntityUid user, Wire wire, ExpCollarComponent comp)
    {
        EntityManager.System<ExpCollarsSystem>().Arm(user, wire, comp);
    }
}

public sealed partial class ExpCollarBoltAction : ComponentWireAction<ExpCollarComponent>
{
    public override Color Color { get; set; } = Color.DarkRed;
    public override string Name { get; set; } = "wire-name-exp-collar-bolt";
    public override bool LightRequiresPower { get; set; } = false;

    public override StatusLightState? GetLightState(Wire wire, ExpCollarComponent comp)
    {
        return comp.Armed ? StatusLightState.On : StatusLightState.Off;
    }

    public override object? StatusKey { get; } = null;

    public override bool Cut(EntityUid user, Wire wire, ExpCollarComponent comp)
    {
        return EntityManager.System<ExpCollarsSystem>().Bolt(user, wire, comp);
    }

    public override bool Mend(EntityUid user, Wire wire, ExpCollarComponent comp)
    {
        return EntityManager.System<ExpCollarsSystem>().Bolt(user, wire, comp);
    }

    public override void Pulse(EntityUid user, Wire wire, ExpCollarComponent comp)
    {
        EntityManager.System<ExpCollarsSystem>().Bolt(user, wire, comp);
    }
}
