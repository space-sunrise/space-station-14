using System.Numerics;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._Sunrise.Pets;
using Robust.Shared.Map;

namespace Content.Server._Sunrise.Pets;

public sealed class PettingSystem : EntitySystem
{
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly NPCSystem _npc = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PettableOnInteractComponent, PetSetAILogicEvent>(Pet);
        SubscribeNetworkEvent<PetSetAILogicEvent>(OnClientChangedPetLogic);
    }

    private void UpdatePetNPC(EntityUid uid, PetOrderType orderType)
    {
        if (!TryComp<HTNComponent>(uid, out var htn))
            return;

        if (htn.Plan != null)
            _htn.ShutdownPlan(htn);

        _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, orderType);
        _htn.Replan(htn);
    }

    private void OnClientChangedPetLogic(PetSetAILogicEvent args)
    {
        var entity = GetEntity(args.Entity);

        if (!TryComp<PettableOnInteractComponent>(entity, out var component))
            return;

        Pet(entity, component, args);
    }

    private void Pet(EntityUid pet, PettableOnInteractComponent component, PetSetAILogicEvent args)
    {
        // Питомец не может следовать за кем-то без хозяина
        if (!component.Master.HasValue)
            return;

        _npc.SetBlackboard(pet, NPCBlackboard.FollowTarget, new EntityCoordinates(component.Master.Value, Vector2.Zero));
        UpdatePetNPC(pet, args.Order);
    }

}
