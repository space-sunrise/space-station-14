﻿namespace Content.Shared._Sunrise.VendingMachines;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class PlayerCountDependentStockComponent : Component
{
    [DataField("coefficient")]
    public float Coefficient = 0.1f;
}
