using System.Collections.Generic;
using Robust.Shared.GameStates;


namespace Content.Shared._Sunrise.LockableEquipment;

/// <summary>
/// Component that defines what layers this entity blocks when worn/equipped,
/// and the priority level of access to those layers.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LayerBlockingComponent : Component
{
    /// <summary>
    /// Layers that this item blocks when worn/equipped
    /// </summary>
    [DataField("coversLayers")]
    public HashSet<string> CoversLayers = new();
    
    /// <summary>
    /// Priority level for accessing layers - lower values mean less access
    /// Higher priority items block access to lower priority items
    /// </summary>
    [DataField("accessPriority")] 
    public int AccessPriority = 0;
}
