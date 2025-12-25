namespace Content.Shared._Sunrise.CarpQueen;

/// <summary>
/// Marker system that grants access permissions to mutate Carp Queen components from server systems.
/// Server systems that need write access should inherit from this.
/// </summary>
public abstract class CarpQueenAccessSystem : EntitySystem
{
}


