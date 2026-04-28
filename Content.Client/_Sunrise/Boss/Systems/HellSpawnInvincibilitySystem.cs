using Content.Client.DamageState;
using Content.Shared._Sunrise.Boss.Components;
using Content.Shared._Sunrise.Boss.Systems;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.Boss.Systems;

/// <inheritdoc />
public sealed class HellSpawnInvincibilitySystem : SharedHellSpawnInvincibilitySystem
{
    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HellSpawnInvincibilityComponent, HellSpawnInvincibilityToggledEvent>(OnToggled);
    }

    private void OnToggled(Entity<HellSpawnInvincibilityComponent> ent, ref HellSpawnInvincibilityToggledEvent args)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearanceComponent))
            return;
        if (!TryComp<SpriteComponent>(ent, out var spriteComponent))
            return;

        if (!spriteComponent.LayerMapTryGet(DamageStateVisualLayers.Base, out var layerIdx))
            return;
        if (args.Enabled)
            spriteComponent.LayerSetShader(layerIdx, "unshaded");
        else
            spriteComponent.LayerSetShader(layerIdx, null, null);
    }
}
