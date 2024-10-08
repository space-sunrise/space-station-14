using Content.Shared._Sunrise.Eye.NightVision.Components; //creater - vladospupuos
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Eye.NightVision
{
    public sealed class NightVisionOverlay : Overlay
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly ILightManager _lightManager = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;


        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance? _greyscaleShader;
	    public Color DisplayColor = Color.Green;

        private NightVisionComponent _nightVisionComponent = default!;

	    public NightVisionOverlay()
        {
            IoCManager.InjectDependencies(this);
            if (!_prototypeManager.TryIndex<ShaderPrototype>("GreyscaleFullscreen", out var shaderPrototype))
            {
                Logger.Error("GreyscaleFullscreen shader not found.");
                return;
            }
            _greyscaleShader = shaderPrototype.InstanceUnique();
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

            if (!_entityManager.TryGetComponent<NightVisionComponent>(playerEntity.Value, out var nightvisionComp))
                return false;

            _nightVisionComponent = nightvisionComp;

            DisplayColor = _nightVisionComponent.Color;

            var nightvision = _nightVisionComponent.IsNightVision;

            if (!nightvision && _nightVisionComponent.DrawShadows) // Disable our Night Vision
            {
                _lightManager.DrawLighting = true;
                _nightVisionComponent.DrawShadows = false;
                _nightVisionComponent.GraceFrame = true;
                return true;
            }

            return nightvision;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            if (!_nightVisionComponent.GraceFrame)
            {
                _nightVisionComponent.DrawShadows = true; // Enable our Night Vision
                _lightManager.DrawLighting = false;
            }
            else
            {
                _nightVisionComponent.GraceFrame = false;
            }

            if (_nightVisionComponent.IsNightVision)
            {
                _greyscaleShader?.SetParameter("SCREEN_TEXTURE", ScreenTexture);

                var worldHandle = args.WorldHandle;
                var viewport = args.WorldBounds;
                worldHandle.UseShader(_greyscaleShader);
                worldHandle.DrawRect(viewport, DisplayColor);
                worldHandle.UseShader(null);
            }
        }
    }
}
