namespace Content.Shared._Sunrise.Weapons.Melee.Components;

/// <summary>
/// Marker component that prevents melee wide (heavy) attacks when attached to a weapon entity.
/// When the player attempts a heavy attack with this weapon, the input is redirected to an alt-interact instead.
/// </summary>
[RegisterComponent]
public sealed partial class DisableMeleeWideAttackComponent : Component;
