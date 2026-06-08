using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Systems;
using Content.Shared.Audio;
using Content.Shared.Power;
using Content.Shared.Radio.Components;
using Content.Shared.Temperature.Components;
using Content.Shared._Sunrise.Radio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using System.Text;

namespace Content.Server._Sunrise.Radio;

public sealed class TelecomThermalSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TemperatureSystem _tempSystem = default!;

    private EntityQuery<AmbientSoundComponent> _ambientQuery;

    public override void Initialize()
    {
        base.Initialize();
        _ambientQuery = GetEntityQuery<AmbientSoundComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TelecomThermalComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var server, out var temp))
        {
            if (server.CurrentLoad > 0)
            {
                server.CurrentLoad = Math.Max(0, server.CurrentLoad - server.LoadDecayRate * frameTime);
            }

            if (server.CurrentLoad > 0)
            {
                _tempSystem.ChangeHeat(uid, server.HeatPerMessage * (server.CurrentLoad / server.LoadDivisor) * frameTime, true, temp);
            }

            if (_ambientQuery.TryGetComponent(uid, out var ambient))
            {
                var ratio = Math.Clamp((temp.CurrentTemperature - server.BaseAmbientTemperature) / (server.MaxTemperature - server.BaseAmbientTemperature), 0f, server.MaxAmbientRatio);
                _ambient.SetVolume(uid, server.BaseAmbientVolume + (ratio * server.AmbientVolumeMultiplier), ambient);
                _ambient.SetRange(uid, server.BaseAmbientRange + (ratio * server.AmbientRangeMultiplier), ambient);
            }

            var pressure = _atmosphere.GetContainingMixture(uid, true)?.Pressure;
            var pressureUnsafe = pressure >= server.MaxPressure;
            var pressureSafe = pressure == null || pressure <= server.HysteresisPressure;

            var temperatureUnsafe = temp.CurrentTemperature >= server.MaxTemperature ||
                                    temp.CurrentTemperature <= server.MinTemperature;
            var temperatureSafe = temp.CurrentTemperature <= server.HysteresisTemperature &&
                                  temp.CurrentTemperature >= server.HysteresisMinTemperature;

            if (temperatureUnsafe || pressureUnsafe)
            {
                if (!server.Overheated)
                {
                    server.Overheated = true;
                    _appearance.SetData(uid, PowerDeviceVisuals.VisualState, 1);
                }
            }
            else if (server.Overheated && temperatureSafe && pressureSafe)
            {
                server.Overheated = false;
                _appearance.SetData(uid, PowerDeviceVisuals.VisualState, 0);
                server.AlarmTimer = 0;
            }

            if (!server.Overheated || server.OverheatSound == null)
                continue;

            server.AlarmTimer -= frameTime;
            if (!(server.AlarmTimer <= 0))
                continue;

            server.AlarmTimer = server.AlarmInterval;
            _audio.PlayPvs(server.OverheatSound, uid);
        }
    }

    public void AddLoad(EntityUid uid, TelecomThermalComponent component)
    {
        if (component.Overheated)
            return;

        component.CurrentLoad += component.LoadIncreasePerMessage;
        _tempSystem.ChangeHeat(uid, component.HeatPerMessage);
    }

    public string AddStatic(TelecomThermalComponent component, string message, float factor)
    {
        if (factor <= component.StaticFactorThreshold)
            return message;

        var result = new StringBuilder();
        var chance = (factor - component.StaticFactorThreshold) * component.StaticChanceMultiplier;
        var inTag = false;

        var words = message.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            var word = words[i];

            if (_random.Prob(chance * component.StaticChanceWordFactor))
            {
                result.Append("...");
            }
            else
            {
                foreach (var c in word)
                {
                    if (c == '[')
                        inTag = true;

                    if (!inTag && _random.Prob(chance))
                    {
                        if (_random.Prob(component.StaticChancePeriodFactor))
                            result.Append('.');
                    }
                    else
                        result.Append(c);

                    if (c == ']')
                        inTag = false;
                }
            }

            if (i < words.Length - 1)
                result.Append(' ');
        }

        return result.ToString();
    }
}
