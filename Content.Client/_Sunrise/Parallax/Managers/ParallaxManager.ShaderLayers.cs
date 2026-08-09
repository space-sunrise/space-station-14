using System.Numerics;
using Content.Client.Parallax.Data;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

#pragma warning disable IDE0130 // Пространство имён совпадает с расширяемым vanilla-менеджером.
namespace Content.Client.Parallax.Managers;

public sealed partial class ParallaxManager
{
    private static readonly ResPath LowQualityStarsPath = new("/Prototypes/Parallaxes/parallax_config.toml");
    private static readonly ResPath BrightStarsPath = new("/Prototypes/Parallaxes/parallax_config_stars.toml");
    private static readonly ResPath DimStarsPath = new("/Prototypes/Parallaxes/parallax_config_stars_dim.toml");
    private static readonly ResPath BrightFarStarsPath = new("/Prototypes/Parallaxes/parallax_config_stars-2.toml");
    private static readonly ResPath DimFarStarsPath = new("/Prototypes/Parallaxes/parallax_config_stars_dim-2.toml");

    private static readonly ProtoId<ShaderPrototype> LowQualityStarsShader = "SunriseParallaxStarsLowQuality";
    private static readonly ProtoId<ShaderPrototype> BrightStarsShader = "SunriseParallaxStarsBright";
    private static readonly ProtoId<ShaderPrototype> DimStarsShader = "SunriseParallaxStarsDim";
    private static readonly ProtoId<ShaderPrototype> BrightFarStarsShader = "SunriseParallaxStarsBrightFar";
    private static readonly ProtoId<ShaderPrototype> DimFarStarsShader = "SunriseParallaxStarsDimFar";

    private static readonly Vector2 ShaderTextureSize = new(1920f, 1080f);

    private bool TryPrepareSunriseShaderLayer(
        ParallaxLayerConfig config,
        out ParallaxLayerConfig shaderConfig,
        out Vector2 textureSize)
    {
        shaderConfig = default!;
        textureSize = default;

        if (config.Texture is not GeneratedParallaxTextureSource generated)
            return false;

        ProtoId<ShaderPrototype> shader;
        if (generated.ParallaxConfigPath == LowQualityStarsPath)
            shader = LowQualityStarsShader;
        else if (generated.ParallaxConfigPath == BrightStarsPath)
            shader = BrightStarsShader;
        else if (generated.ParallaxConfigPath == DimStarsPath)
            shader = DimStarsShader;
        else if (generated.ParallaxConfigPath == BrightFarStarsPath)
            shader = BrightFarStarsShader;
        else if (generated.ParallaxConfigPath == DimFarStarsPath)
            shader = DimFarStarsShader;
        else
            return false;

        shaderConfig = new ParallaxLayerConfig
        {
            Texture = new ShaderParallaxTextureSource(),
            Scale = config.Scale,
            Rotation = config.Rotation,
            Tiled = config.Tiled,
            ControlHomePosition = config.ControlHomePosition,
            WorldHomePosition = config.WorldHomePosition,
            WorldAdjustPosition = config.WorldAdjustPosition,
            Slowness = config.Slowness,
            Scrolling = config.Scrolling,
            Shader = shader,
        };
        textureSize = ShaderTextureSize;
        return true;
    }
}
