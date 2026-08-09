using System;
using System.Numerics; // Sunrise-Edit — логический размер shader-слоя может отличаться от размера backing texture.
using Robust.Client.Graphics;
using Content.Client.Parallax.Data;
using Robust.Shared.Graphics;

namespace Content.Client.Parallax;

/// <summary>
/// A 'prepared' (i.e. texture loaded and ready to use) parallax layer.
/// </summary>
public struct ParallaxLayerPrepared
{
    /// <summary>
    /// The loaded texture for this layer.
    /// </summary>
    public Texture Texture { get; set; }

    // Sunrise added start - кэшируем данные shader-слоя вне горячего пути отрисовки.
    /// <summary>
    /// Логический размер исходной текстуры в пикселях.
    /// Для процедурного слоя backing texture может иметь размер 1x1.
    /// </summary>
    public Vector2 TextureSize { get; set; }

    /// <summary>
    /// Подготовленный шейдер слоя.
    /// </summary>
    public ShaderInstance? Shader { get; set; }
    // Sunrise added end

    /// <summary>
    /// The configuration for this layer.
    /// </summary>
    public ParallaxLayerConfig Config { get; set; }
}

