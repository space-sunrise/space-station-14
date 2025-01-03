using System.Numerics;
using System.Linq; // Sunrise-Edit
using Content.Client.Parallax.Managers;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.ViewVariables;
using Robust.Shared.Prototypes; // Sunrise-Edit
using Content.Shared._Sunrise.Lobby; // Sunrise-Edit

namespace Content.Client.Parallax;

/// <summary>
///     Renders the parallax background as a UI control.
/// </summary>
public sealed class ParallaxControl : Control
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IParallaxManager _parallaxManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!; // Sunrise-Edit

    [ViewVariables(VVAccess.ReadWrite)] public Vector2 Offset { get; set; }
    [ViewVariables(VVAccess.ReadWrite)] public string CurrentParallax { get; private set; } = "FastSpace"; // Sunrise-Edit

    private readonly HashSet<string> _invalidParallaxes = new(); // Sunrise-Edit

    public ParallaxControl()
    {
        IoCManager.InjectDependencies(this);

        Offset = new Vector2(_random.Next(0, 1000), _random.Next(0, 1000));
        RectClipContent = true;
        // Sunrise-Edit-Start
        SelectRandomParallax();
    }

    private void SelectRandomParallax()
    {
        var parallaxes = _prototypeManager.EnumeratePrototypes<LobbyParallaxPrototype>()
            .Where(p => !_invalidParallaxes.Contains(p.Parallax))
            .ToList();
        
        if (parallaxes.Any())
        {
            var selectedParallax = _random.Pick(parallaxes);
            CurrentParallax = selectedParallax.Parallax;
        }
        else
        {
            CurrentParallax = "FastSpace";
        }
        
        _parallaxManager.LoadParallaxByName(CurrentParallax);
        // Sunrise-Edit-End
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (Size.X <= 0 || Size.Y <= 0)
            return;
        // Sunrise-Edit-Start
        var layers = _parallaxManager.GetParallaxLayers(CurrentParallax).ToList();
        if (!layers.Any())
        {
            _invalidParallaxes.Add(CurrentParallax);
            SelectRandomParallax();
            return;
        }

        var hasValidLayers = false;
        foreach (var layer in layers)
        {
            var tex = layer.Texture;
            if (tex.Size.X <= 0 || tex.Size.Y <= 0)
                continue;

            var scale = layer.Config.Scale.Floored();
            if (scale.X <= 0 || scale.Y <= 0)
                continue;

            var texSize = new Vector2i(
                (tex.Size.X * (int)Size.X) / 1920,
                (tex.Size.Y * (int)Size.X) / 1920
            ) * scale;

            if (texSize.X <= 0 || texSize.Y <= 0)
                continue;

            hasValidLayers = true;
            var ourSize = PixelSize;
            var currentTime = (float)_timing.RealTime.TotalSeconds;
            // Sunrise-Edit-End
            var offset = Offset + new Vector2(currentTime * 100f, currentTime * 0f);

            if (layer.Config.Tiled)
            {
                var scaledOffset = (offset * layer.Config.Slowness).Floored();
                scaledOffset.X %= texSize.X;
                scaledOffset.Y %= texSize.Y;

                for (var x = -scaledOffset.X; x < ourSize.X; x += texSize.X)
                {
                    for (var y = -scaledOffset.Y; y < ourSize.Y; y += texSize.Y)
                    {
                        handle.DrawTextureRect(tex, UIBox2.FromDimensions(new Vector2(x, y), texSize));
                    }
                }
            }
            else
            {
                var origin = ((ourSize - texSize) / 2) + layer.Config.ControlHomePosition;
                handle.DrawTextureRect(tex, UIBox2.FromDimensions(origin, texSize));
            }
        }
        // Sunrise-Edit-Start
        if (!hasValidLayers)
        {
            _invalidParallaxes.Add(CurrentParallax);
            SelectRandomParallax();
        }
        // Sunrise-Edit-End
    }
}

