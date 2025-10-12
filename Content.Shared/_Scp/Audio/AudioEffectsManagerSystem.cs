using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Content.Shared.GameTicking;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Shared._Scp.Audio;

public sealed class AudioEffectsManagerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly INetManager _net = default!;

    /// <summary>
    /// Захешированные эффекты под их прототипами пренитов. Позволяет не засрать слоты OpenAL сотней одинаковых эффектов
    /// </summary>
    private static readonly Dictionary<ProtoId<AudioPresetPrototype>, EntityUid> CachedEffects = new ();
    private static CancellationTokenSource _tokenSource = new();

    private static readonly TimeSpan RaceConditionWaiting = TimeSpan.FromTicks(10L);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => Clear());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        Clear();
    }

    private static void Clear()
    {
        CachedEffects.Clear();

        _tokenSource.Cancel();
        _tokenSource = new();
    }

    /// <summary>
    /// Добавляет переданный эффект к звуку
    /// </summary>
    public bool TryAddEffect(Entity<AudioComponent> sound, ProtoId<AudioPresetPrototype> preset)
    {
        if (!CachedEffects.TryGetValue(preset, out var effect) && !TryCreateEffect(preset, out effect))
            return false;

        // ЕБАННЫЙ РОТ ЭТОГО РЕЙС КОДИШЕН
        /*
         Лонг-рид причина почему тут стоит таймер:
         Как только only server-side звук приходит сюда, он вызывает только серверную систему добавления Auxiliary
         Тот вызывает Dirty(), который отлавливается на клиенте вручную через компонент стейт
         Там на аудио сурс навешивается эффект. Только по умолчанию сурс это дамми(заглушка)
         Аудио сурс выставляется на подписке AudioComponent на ComponentStartup().
         Так как я могу подписаться только ComponentInit(), который идет раньше, чем ComponentStartup()
         То мой ивент происходит раньше, чем выставляется аудиосурс на клиенте -> сервер успевает втиснуться в промежуток между этой хуйней
         И добавить эффект на заглушку, которая ниче не сделает. Поэтому я на рандом поставил сюда 10 тиков, за это время все успевает сработать
         ГОВНО
         */
        if (_net.IsServer)
        {
            Timer.Spawn(RaceConditionWaiting, () => _audio.SetAuxiliary(sound, sound, effect), _tokenSource.Token);
        }
        else
        {
            _audio.SetAuxiliary(sound, sound, effect);
        }

        return true;
    }

    /// <summary>
    /// Пытается убрать данный эффект со звука
    /// </summary>
    public bool TryRemoveEffect(Entity<AudioComponent> sound, ProtoId<AudioPresetPrototype> preset)
    {
        if (!CachedEffects.TryGetValue(preset, out var effect))
            return false;

        if (sound.Comp.Auxiliary != effect)
            return false;

        _audio.SetAuxiliary(sound, sound, null);
        return true;
    }

    public void RemoveAllEffects(Entity<AudioComponent> sound)
    {
        _audio.SetAuxiliary(sound, sound, null);
    }

    /// <summary>
    /// Пытается создать эффект и захешировать его
    /// </summary>
    /// <param name="preset">Пресет эффектов</param>
    /// <param name="effectStuff">Получаемый эффект. Не представляет собой ничего, когда метод возвращает false</param>
    /// <returns>Возвращает успешно ли создание и хеширование эффекта</returns>
    public bool TryCreateEffect(ProtoId<AudioPresetPrototype> preset, out EntityUid effectStuff)
    {
        effectStuff = default;

        if (!_prototype.TryIndex(preset, out var prototype))
            return false;

        var effect = _audio.CreateEffect();
        var auxiliary = _audio.CreateAuxiliary();

        _audio.SetEffectPreset(effect.Entity, effect.Component, prototype);
        _audio.SetEffect(auxiliary.Entity, auxiliary.Component, effect.Entity);

        if (!Exists(auxiliary.Entity))
            return false;

        if (!CachedEffects.TryAdd(preset, auxiliary.Entity))
            return false;

        effectStuff = auxiliary.Entity;

        return true;
    }

    public static bool HasEffect(Entity<AudioComponent> sound, ProtoId<AudioPresetPrototype> preset)
    {
        if (!CachedEffects.TryGetValue(preset, out var effect))
            return false;

        return sound.Comp.Auxiliary == effect;
    }

    public bool TryGetEffect(Entity<AudioComponent> sound, [NotNullWhen(true)] out ProtoId<AudioPresetPrototype>? preset)
    {
        preset = null;

        foreach (var (storedPreset, auxUid) in CachedEffects)
        {
            if (sound.Comp.Auxiliary != auxUid)
                continue;

            preset = storedPreset;
            return true;
        }

        return false;
    }
}
