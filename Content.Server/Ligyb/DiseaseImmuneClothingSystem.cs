using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Ligyb;
using Content.Shared.Examine;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
namespace Content.Server.Ligyb;

public sealed class DiseaseImmuneClothingSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DiseaseImmuneClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<DiseaseImmuneClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<DiseaseImmuneClothingComponent, GetVerbsEvent<ExamineVerb>>(OnArmorVerbExamine);
    }

    private void OnGotEquipped(EntityUid uid, DiseaseImmuneClothingComponent component, GotEquippedEvent args)
    {
        if (!TryComp(uid, out ClothingComponent? clothing))
            return;
        var isCorrectSlot = clothing.Slots.HasFlag(args.SlotFlags);
        if (!isCorrectSlot)
            return;

        EnsureComp<DiseaseTempImmuneComponent>(args.Equipee).Prob += component.prob;
        if (Comp<DiseaseTempImmuneComponent>(args.Equipee).Prob > 1) Comp<DiseaseTempImmuneComponent>(args.Equipee).Prob = 1;

        component.IsActive = true;
    }

    private void OnGotUnequipped(EntityUid uid, DiseaseImmuneClothingComponent component, GotUnequippedEvent args)
    {
        if (!component.IsActive)
            return;

        EnsureComp<DiseaseTempImmuneComponent>(args.Equipee).Prob -= component.prob;
        if (Comp<DiseaseTempImmuneComponent>(args.Equipee).Prob < 0) Comp<DiseaseTempImmuneComponent>(args.Equipee).Prob = 0;

        component.IsActive = false;
    }

    private void OnArmorVerbExamine(EntityUid uid, DiseaseImmuneClothingComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var examineMarkup = new FormattedMessage();
        examineMarkup.AddMarkup($"Защищает от заражения на {Convert.ToInt32(component.prob*100)}%");

        _examine.AddDetailedExamineVerb(args, component, examineMarkup,
            "Стерильность", "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            "Изучить показатели стерильности.");
    }

}
