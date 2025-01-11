using Content.Shared.Inventory.Events;
using Robust.Shared.Timing;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.AnimalHusbandry;

namespace Content.Server.FaceCast;

public sealed class FaceCastSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<FaceCastComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<FaceCastComponent, GotUnequippedEvent>(OnUnequip);
    }

    private void OnEquip(EntityUid ent, FaceCastComponent faceCast, ref GotEquippedEvent args)
    {
        if (!HasComp<IdentityComponent>(args.Equipee))
            return;
        faceCast.Equiper = args.Equipee;
        if (HasComp<InfantComponent>(args.Equipee))
            return;
        faceCast.StartCastingTime = _timing.CurTime;
    }

    private void OnUnequip(EntityUid ent, FaceCastComponent faceCast, ref GotUnequippedEvent args)
    {
        faceCast.StartCastingTime = null;
        faceCast.Equiper = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FaceCastComponent>();
        while (query.MoveNext(out var uid, out var faceCast))
        {
            if (!TryComp<ClothingComponent>(uid, out var cloth))
                continue;

            if (faceCast.Equiper == null)
                continue;

            if (!HasComp<InfantComponent>(faceCast.Equiper) && faceCast.StartCastingTime == null)
            {
                if (Name(uid) != Loc.GetEntityData("ClothingMaskFaceCast").Name)
                    _metaData.SetEntityName((EntityUid) faceCast.Equiper, Name(uid));
                faceCast.StartCastingTime = _timing.CurTime;
                continue;
            }

            if (faceCast.StartCastingTime == null)
                continue;

            if ((_timing.CurTime - faceCast.StartCastingTime) >= TimeSpan.FromSeconds(faceCast.TimeToCast))
            {
                _metaData.SetEntityName(uid, Name((EntityUid)faceCast.Equiper));
                faceCast.StartCastingTime = _timing.CurTime;
            }

        }
    }
}
