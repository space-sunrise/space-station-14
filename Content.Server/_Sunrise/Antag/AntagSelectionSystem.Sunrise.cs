using Content.Server.Antag.Components;

namespace Content.Server.Antag;

public sealed partial class AntagSelectionSystem
{
    public bool TrySetAssignedMindNameByEntity(Entity<AntagSelectionComponent?> ent, EntityUid antag, string name)
    {
        if (!Resolve(ent, ref ent.Comp, false) ||
            !_mind.TryGetMind(antag, out var antagMind, out _))
            return false;

        for (var i = 0; i < ent.Comp.AssignedMinds.Count; i++)
        {
            var (mind, _) = ent.Comp.AssignedMinds[i];
            if (mind != antagMind)
                continue;

            ent.Comp.AssignedMinds[i] = (mind, name);
            return true;
        }

        return false;
    }
}
