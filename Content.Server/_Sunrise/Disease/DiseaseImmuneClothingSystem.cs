// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared._Sunrise.Disease;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Armor;
namespace Content.Server._Sunrise.Disease;

public sealed class DiseaseImmuneClothingSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DiseaseImmuneClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<DiseaseImmuneClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<DiseaseImmuneClothingComponent, ArmorExamineEvent>(OnArmorExamine);
    }

    private void OnGotEquipped(EntityUid uid, DiseaseImmuneClothingComponent component, GotEquippedEvent args)
    {
        if (!TryComp(uid, out ClothingComponent? clothing))
            return;
        var isCorrectSlot = clothing.Slots.HasFlag(args.SlotFlags);
        if (!isCorrectSlot)
            return;

        EnsureComp<DiseaseTempImmuneComponent>(args.Equipee).Prob += component.Prob;
        if (Comp<DiseaseTempImmuneComponent>(args.Equipee).Prob > 1) Comp<DiseaseTempImmuneComponent>(args.Equipee).Prob = 1;

        component.IsActive = true;
    }

    private void OnGotUnequipped(EntityUid uid, DiseaseImmuneClothingComponent component, GotUnequippedEvent args)
    {
        if (!component.IsActive)
            return;

        EnsureComp<DiseaseTempImmuneComponent>(args.Equipee).Prob -= component.Prob;
        if (Comp<DiseaseTempImmuneComponent>(args.Equipee).Prob < 0) Comp<DiseaseTempImmuneComponent>(args.Equipee).Prob = 0;

        component.IsActive = false;
    }

    private void OnArmorExamine(EntityUid uid, DiseaseImmuneClothingComponent component, ref ArmorExamineEvent args)
    {
        args.Msg.PushNewline();
        args.Msg.AddMarkupOrThrow(Loc.GetString("disease-protection-value", ("value", Convert.ToInt32(component.Prob * 100))));
    }
}
