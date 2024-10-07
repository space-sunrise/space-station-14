using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Sunrise.Eye.NightVision
{
    public sealed class NightVisionDeviceOverlay : Overlay
    {
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
            return Enabled;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null || Shader == null || DisplayColor == null)
                return;

            Shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            worldHandle.SetTransform(Matrix3x2.Identity);
            worldHandle.UseShader(Shader);
            worldHandle.DrawRect(viewport, DisplayColor.Value);
        }
    }
}
