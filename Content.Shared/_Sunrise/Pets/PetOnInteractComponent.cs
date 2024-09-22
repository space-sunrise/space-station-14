using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Pets;

/// <summary>
/// Компонент, позволяющий приручать питомцев по клику пустой рукой
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PetOnInteractComponent : Component
{
    /// <summary>
    /// Список всех прирученных питомцев
    /// </summary>
    public HashSet<EntityUid> Pets = new();

    /// <summary>
    /// Список всех акшенов, которые используются для управления питомцем.
    /// </summary>
    public HashSet<EntityUid?> PetActions = new();
}
