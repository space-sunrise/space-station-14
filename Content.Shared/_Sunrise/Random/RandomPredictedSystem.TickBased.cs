using Content.Shared.GameTicking;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Random;

public sealed partial class RandomPredictedSystem
{
    /*
     * Часть системы, отвечающая за рандом на основе текущего тика.
     */

    private System.Random? _tickRandom;
    private GameTick _lastTick;

    #region Event handlers

    private void InitializeTickBased()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _tickRandom = null;
        _lastTick = GameTick.Zero;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Возвращает случайное целое число, зависящее от текущего тика.
    /// </summary>
    [PublicAPI]
    public int NextByTick(int minValue, int maxValue)
    {
        UpdateTickRandom();
        return _tickRandom!.Next(minValue, maxValue);
    }

    /// <summary>
    /// Возвращает случайное число с плавающей запятой, зависящее от текущего тика.
    /// </summary>
    [PublicAPI]
    public float NextFloatByTick(float minValue = 0f, float maxValue = 1f)
    {
        UpdateTickRandom();
        return (float)(_tickRandom!.NextDouble() * (maxValue - minValue) + minValue);
    }

    /// <summary>
    /// Возвращает true с заданной вероятностью, зависящей от текущего тика.
    /// </summary>
    [PublicAPI]
    public bool ProbByTick(float chance)
    {
        UpdateTickRandom();
        return _tickRandom!.NextDouble() < chance;
    }

    /// <summary>
    /// Обновляет внутренний генератор случайных чисел, если тик изменился.
    /// </summary>
    private void UpdateTickRandom()
    {
        var currentTick = _timing.CurTick;
        if (_lastTick != currentTick)
        {
            _lastTick = currentTick;
            _tickRandom = new System.Random((int)(currentTick.Value & 0x7FFFFFFF));
        }
    }

    #endregion
}
