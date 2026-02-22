// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Server._Nox.Disease.Components;
using Content.Shared.Armor;
using Content.Shared.Inventory;
using Content.Shared._Nox.Disease;

namespace Content.Server._Nox.Disease.Systems;

public sealed partial class DiseaseResistanceSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseResistanceComponent, ArmorExamineEvent>(OnArmorExamine);
        SubscribeLocalEvent<DiseaseResistanceComponent, InventoryRelayedEvent<DiseaseResistanceQueryEvent>>(OnResistanceQuery);
    }

    private void OnResistanceQuery(Entity<DiseaseResistanceComponent> ent, ref InventoryRelayedEvent<DiseaseResistanceQueryEvent> query)
    {
        query.Args.TotalCoefficient *= ent.Comp.DiseaseResistanceCoefficient;
    }

    private void OnArmorExamine(Entity<DiseaseResistanceComponent> ent, ref ArmorExamineEvent args)
    {
        var value = MathF.Round(ent.Comp.DiseaseResistanceCoefficient * 100, 1);

        if (value == 0)
            return;

        args.Msg.PushNewline();
        args.Msg.AddMarkupOrThrow(Loc.GetString(ent.Comp.Examine, ("value", value)));
    }


}
