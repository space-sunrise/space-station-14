using Content.Shared._Sunrise.ThermalVision;
using Content.Shared.Body.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.ThermalVision;

public sealed class ThroughWallsVisionOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    private SpriteSystem _spriteSystem = default!;
    private readonly ContainerSystem _containerSystem;
    private readonly TransformSystem _transform;
    private readonly ShaderInstance _shader;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly bool ApplyCamo;
    private EntityQuery<XRayCamoComponent> _camoQuery = default!;
    public ThroughWallsVisionOverlay(bool applyCamo = false)
    {
        IoCManager.InjectDependencies(this);
        _transform = _entityManager.System<TransformSystem>();
        _containerSystem = _entityManager.System<ContainerSystem>();

        ApplyCamo = applyCamo;

        _shader = _prototypeManager.Index<ShaderPrototype>("BrightnessShader").InstanceUnique();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
            return false;

        if (args.Viewport.Eye != eyeComp.Eye)
            return false;

        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (playerEntity == null)
            return false;

        if (!_entityManager.TryGetComponent<ThermalVisionComponent>(playerEntity, out var blurComp))
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_entityManager.SystemOrNull<SpriteSystem>() is not { } spriteSystem)
            return;
        _spriteSystem = spriteSystem;
        _camoQuery = _entityManager.GetEntityQuery<XRayCamoComponent>();

        if (ScreenTexture == null)
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;
        var eyeRotation = args.Viewport.Eye?.Rotation ?? Angle.Zero;

        worldHandle.UseShader(_shader);
        var query = _entityManager.EntityQueryEnumerator<BodyComponent, MetaDataComponent, SpriteComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var meta, out var sprite, out var xform))
        {
            if (xform.MapID != args.MapId || _containerSystem.IsEntityInContainer(uid, meta)) continue;
            var (position, rotation) = _transform.GetWorldPositionRotation(xform);

            if (ApplyCamo && _camoQuery.TryGetComponent(uid, out var camoComp))
            {
                var prevColor = sprite.Color;
                var maskingAmount = Math.Clamp(1f - camoComp.CamoLevel, 0f, 1f);
                _spriteSystem.SetColor((uid, sprite), Color.FromHsv(new System.Numerics.Vector4(0, 0, maskingAmount, maskingAmount)));
                _spriteSystem.RenderSprite((uid, sprite), worldHandle, eyeRotation, rotation, position);
                _spriteSystem.SetColor((uid, sprite), prevColor);
                continue;
            }

            _spriteSystem.RenderSprite((uid, sprite), worldHandle, eyeRotation, rotation, position);
        }

        worldHandle.UseShader(null);
    }
}
