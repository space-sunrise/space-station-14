namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// Component that prevents law changes from external sources.
/// Used to protect certain borgs from having their laws changed by events or other systems.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BlockLawChangeComponent : Component
{
}
