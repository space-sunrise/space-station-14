using Content.Server.Temperature.Systems;
using Content.Shared.Audio;
using Content.Shared.Power;
using Content.Shared.Radio.Components;
using Content.Shared.Temperature.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Text;

namespace Content.Server._Sunrise.Radio;

public sealed class TelecomThermalSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly TemperatureSystem _tempSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TelecomServerComponent, TemperatureComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var server, out var temp, out var xform))
        {
            // 1. Decay load over time
            if (server.CurrentLoad > 0)
            {
                server.CurrentLoad = Math.Max(0, server.CurrentLoad - server.LoadDecayRate * frameTime);
            }

            // 2. Heat exchange with environment
            if (server.CurrentLoad > 0)
            {
                // Scaled heat accumulation
                _tempSystem.ChangeHeat(uid, server.HeatPerMessage * (server.CurrentLoad / 10f) * frameTime, true, temp);
            }

            // Exchange heat with atmos (handled automatically by AtmosphereSystem)

            // 3. Dynamic ambient sound scaling
            if (TryComp<AmbientSoundComponent>(uid, out var ambient))
            {
                var ratio = Math.Clamp((temp.CurrentTemperature - 300f) / (server.MaxTemperature - 300f), 0f, 1.2f);
                _ambient.SetVolume(uid, -9f + (ratio * 12f), ambient);
                _ambient.SetRange(uid, 5f + (ratio * 15f), ambient);
            }

            // 4. Handle Overheating State & Alarms
            if (temp.CurrentTemperature >= server.MaxTemperature)
            {
                if (!server.Overheated)
                {
                    server.Overheated = true;
                    _appearance.SetData(uid, PowerDeviceVisuals.VisualState, 1);
                }
            }
            else if (server.Overheated && temp.CurrentTemperature <= server.HysteresisTemperature)
            {
                server.Overheated = false;
                _appearance.SetData(uid, PowerDeviceVisuals.VisualState, 0);
                server.AlarmTimer = 0;
            }

            // Audible alarm during overheat
            if (server.Overheated && server.OverheatSound != null)
            {
                server.AlarmTimer -= frameTime;
                if (server.AlarmTimer <= 0)
                {
                    server.AlarmTimer = server.AlarmInterval;
                    _audio.PlayPvs(server.OverheatSound, uid);
                }
            }
        }
    }

    public void AddLoad(EntityUid uid, TelecomServerComponent component)
    {
        if (component.Overheated)
            return;

        component.CurrentLoad += 1.0f;
        _tempSystem.ChangeHeat(uid, component.HeatPerMessage);
    }

    public string AddStatic(string message, float factor)
    {
        if (factor <= 0.3f) 
            return message;

        var result = new StringBuilder();
        var chance = (factor - 0.3f) * 0.8f;
        var inTag = false;

        foreach (var c in message)
        {
            if (c == '[')
                inTag = true;

            if (!inTag && _random.Prob(chance))
                result.Append(_random.Pick(new[] { '#', '*', '$', '!', '&', '?' }));
            else
                result.Append(c);

            if (c == ']')
                inTag = false;
        }

        return result.ToString();
    }
}
