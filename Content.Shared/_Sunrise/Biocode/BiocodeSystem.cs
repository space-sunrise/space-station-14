using Content.Shared.NPC.Components;

namespace Content.Shared._Sunrise.Biocode;

public sealed class BiocodeSystem : EntitySystem
{

    public bool CanUse(EntityUid user, HashSet<string> factions)
    {
        var canUse = false;
        if (!TryComp<NpcFactionMemberComponent>(user, out var npcFactionMemberComponent))
            return canUse;

        foreach (var faction in npcFactionMemberComponent.Factions)
        {
            if (factions.Contains(faction))
                canUse = true;
        }

        return canUse;
    }
}
