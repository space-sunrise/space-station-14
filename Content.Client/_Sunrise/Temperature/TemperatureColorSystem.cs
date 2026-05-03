using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Robust.Client.GameObjects;

namespace Content.Client._Sunrise.Temperature;

public sealed class TemperatureColorSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TemperatureComponent, OnTemperatureChangeEvent>(OnTemperatureChanged);
    }

    private void OnTemperatureChanged(EntityUid uid, TemperatureComponent component, ref OnTemperatureChangeEvent args)
    {
        component.CurrentTemperature = args.CurrentTemperature;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TemperatureComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var temp, out var sprite))
        {
            if (!temp.ColorTemperature)
                continue;

            var currentTemp = temp.CurrentTemperature;
            Color targetColor = Color.White;

            if (currentTemp < temp.NeutralTemp)
            {
                if (temp.ColorCold)
                {
                    // Interpolate towards ColdColor
                    var ratio = Math.Clamp((temp.NeutralTemp - currentTemp) / (temp.NeutralTemp - temp.ColdThreshold), 0f, 1f);
                    targetColor = Color.InterpolateBetween(Color.White, temp.ColdColor, ratio);
                }
            }
            else
            {
                if (temp.ColorHot)
                {
                    // Interpolate towards HotColor
                    var ratio = Math.Clamp((currentTemp - temp.NeutralTemp) / (temp.HotThreshold - temp.NeutralTemp), 0f, 1f);
                    targetColor = Color.InterpolateBetween(Color.White, temp.HotColor, ratio);
                }
            }

            // Apply color only if it changed to avoid unnecessary sprite updates
            if (!sprite.Color.Equals(targetColor))
            {
                _sprite.SetColor((uid, sprite), targetColor);
            }
        }
    }
}
