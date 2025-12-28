using Content.Shared._Scp.Audio;
using Content.Shared._Scp.Audio.Components;
using Content.Shared._Scp.ScpCCVars;
using Content.Client._Scp.Audio.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Audio;

/// <summary>
/// Система, накладывающая эффект эхо каждому неглобальному звуку.
/// Эффект может быть отключен игроком в настройках
/// </summary>
public sealed class EchoEffectSystem : EntitySystem
{
    [Dependency] private readonly AudioEffectsManagerSystem _effectsManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static readonly ProtoId<AudioPresetPrototype> StandardEchoEffectPreset = "Bathroom";
    private static readonly ProtoId<AudioPresetPrototype> StrongEchoEffectPreset = "SewerPipe";

    private bool _isClientSideEnabled;
    private bool _strongPresetPreferred;

    public override void Initialize()
    {
        base.Initialize();

        // Форсируем статическую инициализацию CVars ДО всех GetCVar (фикс проблемы с регистрацией)
        var _ = ScpCCVars.EchoEnabled;

        SubscribeLocalEvent<AudioComponent, ComponentAdd>(OnAudioAdd);
        SubscribeLocalEvent<AudioEffectedComponent, ComponentStartup>(OnEffectedAudioStartup, after: [typeof(SharedAudioSystem)]);

        _isClientSideEnabled = _cfg.GetCVar(ScpCCVars.EchoEnabled);
        _strongPresetPreferred = _cfg.GetCVar(ScpCCVars.EchoStrongPresetPreferred);

        _cfg.OnValueChanged(ScpCCVars.EchoEnabled, OnEnabledToggled);
        _cfg.OnValueChanged(ScpCCVars.EchoStrongPresetPreferred, OnPreferredPresetToggled);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(ScpCCVars.EchoEnabled, OnEnabledToggled);
        _cfg.UnsubValueChanged(ScpCCVars.EchoStrongPresetPreferred, OnPreferredPresetToggled);
    }

    private void OnAudioAdd(Entity<AudioComponent> ent, ref ComponentAdd args)
    {
        if (!_isClientSideEnabled)
            return;

        EnsureComp<AudioEffectedComponent>(ent);
    }

    private void OnEffectedAudioStartup(Entity<AudioEffectedComponent> ent, ref ComponentStartup args)
    {
        if (!_isClientSideEnabled)
            return;

        if (!TryComp<AudioComponent>(ent.Owner, out var audio))
            return;

        TryApplyEcho((ent.Owner, audio));
    }

    /// <summary>
    /// Пытается применить эхо к данном звуку
    /// </summary>
    /// <param name="sound">Звук, к которому будет применен эффект</param>
    /// <param name="preset">Пресет, если нужно выставить какой-то особенный</param>
    /// <returns>Получилось или не получилось применить эффект</returns>
    public bool TryApplyEcho(Entity<AudioComponent> sound, ProtoId<AudioPresetPrototype>? preset = null)
    {
        if (TerminatingOrDeleted(sound) || Paused(sound))
            return false;

        // Фоновая музыка не должна подвергаться эффектам эха
        if (sound.Comp.Global)
            return false;

        // Выбираем пресет для эха исходя из настроек игрока и возможного приоритетного эффекта при вызове извне системы
        var clientPreferredPreset = _strongPresetPreferred ? StrongEchoEffectPreset : StandardEchoEffectPreset;
        var targetPreset = preset ?? clientPreferredPreset;

        _effectsManager.TryAddEffect(sound, targetPreset);

        // Добавляем компонент-маркер к звуку, который будет хранить эффект эха
        var echoComp = EnsureComp<AudioEchoEffectAffectedComponent>(sound);
        echoComp.Preset = targetPreset;

        return true;
    }

    /// <summary>
    /// Пытается убрать эффект эхо у выбранного звука
    /// </summary>
    public bool TryRemoveEcho(Entity<AudioComponent> sound, AudioEchoEffectAffectedComponent? echoComp = null)
    {
        if (!Resolve(sound, ref echoComp))
            return false;

        if (!_effectsManager.TryRemoveEffect(sound, echoComp.Preset))
            return false;

        return true;
    }

    private void OnEnabledToggled(bool enabled)
    {
        _isClientSideEnabled = enabled;

        if (!enabled)
            RevertChanges();
    }

    private void OnPreferredPresetToggled(bool useStrong)
    {
        _strongPresetPreferred = useStrong;
        var newPreferredPreset = useStrong ? StrongEchoEffectPreset : StandardEchoEffectPreset;

        TogglePreset(newPreferredPreset);
    }

    /// <summary>
    /// Убирает эффекты эхо у всех звуков, что имеют его.
    /// Вызывается при выключении эффекта эха игроком.
    /// </summary>
    private void RevertChanges()
    {
        var query = AllEntityQuery<AudioEchoEffectAffectedComponent, AudioComponent>();

        while (query.MoveNext(out var uid, out var echoComp, out var audio))
        {
            TryRemoveEcho((uid, audio), echoComp);
        }
    }

    private void TogglePreset(ProtoId<AudioPresetPrototype> newPreferredPreset)
    {
        var query = AllEntityQuery<AudioEchoEffectAffectedComponent, AudioComponent>();

        while (query.MoveNext(out var uid, out var echoComp, out var audio))
        {
            if (!TryRemoveEcho((uid, audio), echoComp))
                continue;

            TryApplyEcho((uid, audio), newPreferredPreset);
        }
    }
}
