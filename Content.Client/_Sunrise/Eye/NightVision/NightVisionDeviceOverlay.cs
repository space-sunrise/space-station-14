using System.Numerics;
using Content.Shared._Sunrise.Eye.NightVision.Components;
using Content.Shared.Inventory;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;

namespace Content.Client._Sunrise.Eye.NightVision
{
    public sealed class NightVisionDeviceOverlay : Overlay
    {
        [Dependency] private readonly ILightManager _lightManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        public ShaderInstance? Shader;
        public Color? DisplayColor;
        public bool Enabled;

        public NightVisionDeviceOverlay()
        {
            IoCManager.InjectDependencies(this);
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            var playerEntity = _playerManager.LocalSession?.AttachedEntity;
            if (playerEntity == null)
                return false;

            if (!_entityManager.TryGetComponent(playerEntity, out EyeComponent? eyeComp))
                return false;

            if (args.Viewport.Eye != eyeComp.Eye)
                return false;

            if (!Enabled)
                return false;

            // Явный бред
            _lightManager.DrawLighting = !Enabled;
            return true;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null || Shader == null || DisplayColor == null)
                return;

            Shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            worldHandle.UseShader(Shader);
            worldHandle.DrawRect(viewport, DisplayColor.Value);
            worldHandle.UseShader(null);
        }
    }
}
