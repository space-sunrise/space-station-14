using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Inventory.Components;

/// <summary>
/// Ограничивает экипировку на цель при помощи вайтлиста и блеклиста.
/// Если хотите понять как использовать EntityWhitelist посмотрите на другие похожие прототипы.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ClothingWhitelistComponent : Component
{
    /// <summary>
    /// Вайтлист сущностей, которым разрешено экипировать этот предмет.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Блеклист сущностей, которым запрещено экипировать этот предмет.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;
}
