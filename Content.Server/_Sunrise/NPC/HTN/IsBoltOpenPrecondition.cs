using Content.Server.NPC.HTN.Preconditions;
using Content.Server.NPC;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Hands.Components;

namespace Content.Server._Sunrise.NPC.HTN;

public sealed partial class IsBoltOpenPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entManager.TryGetComponent<HandsComponent>(owner, out var hands) || hands.ActiveHandEntity is not { } heldEntity)
            return false;

        if (!_entManager.TryGetComponent<ChamberMagazineAmmoProviderComponent>(heldEntity, out var chamber))
            return false;

        return chamber.BoltClosed == false;
    }
} 