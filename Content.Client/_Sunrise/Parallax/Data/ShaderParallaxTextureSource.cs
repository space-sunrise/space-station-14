using System.Threading;
using System.Threading.Tasks;
using Content.Client.Parallax.Data;
using JetBrains.Annotations;
using Robust.Client.Graphics;

#pragma warning disable IDE0130 // Пространство имён совпадает с расширяемым vanilla-подсистемой.
namespace Content.Client.Parallax.Data;

/// <summary>
/// Предоставляет stock texture для процедурного shader-слоя параллакса.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class ShaderParallaxTextureSource : IParallaxTextureSource
{
    Task<Texture> IParallaxTextureSource.GenerateTexture(CancellationToken cancel)
    {
        return Task.FromResult(Texture.White);
    }
}
