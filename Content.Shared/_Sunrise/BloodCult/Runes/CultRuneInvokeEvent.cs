namespace Content.Shared._Sunrise.BloodCult.Runes;

public sealed class CultRuneInvokeEvent : EntityEventArgs
{
    public CultRuneInvokeEvent(EntityUid rune, EntityUid user, HashSet<EntityUid> cultists)
    {
        Rune = rune;
        User = user;
        Cultists = cultists;
    }

    public EntityUid Rune { get; set; }
    public EntityUid User { get; set; }
    public HashSet<EntityUid> Cultists { get; }
    public bool Result { get; set; }
}
