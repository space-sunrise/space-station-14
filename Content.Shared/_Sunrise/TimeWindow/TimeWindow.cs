using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Shared._Sunrise.TimeWindow;

[DataDefinition]
public sealed partial class TimedWindow
{
    [DataField]
    public TimeSpan Min;

    [DataField]
    public TimeSpan Max;

    /// <summary>
    ///     Остаток времени до следующего события.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan Remaining { get; set; } = TimeSpan.Zero;

    public TimedWindow(TimeSpan minSeconds, TimeSpan maxSeconds)
    {
        Min = minSeconds;
        Max = maxSeconds;
    }

    public TimedWindow Clone()
    {
        return new TimedWindow(Min, Max);
    }
}

public sealed class TimedWindowSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    ///     Добавить время окну.
    /// </summary>
    public void AddTime(TimedWindow window, TimeSpan time)
    {
        window.Remaining += time;
    }

    /// <summary>
    ///     Сбрасывает таймер на новое случайное время.
    /// </summary>
    public void Reset(TimedWindow window)
    {
        window.Remaining = _timing.CurTime + GetRandomDuration(window);
    }

    /// <summary>
    ///     Сбрасывает таймер на заданный диапазон времени.
    /// </summary>
    public void Reset(TimedWindow window, float minSeconds, float maxSeconds)
    {
        window.Remaining = _timing.CurTime + GetRandomDuration(minSeconds, maxSeconds);
    }

    /// <summary>
    ///     Проверяет, истекло ли время окна.
    /// </summary>
    public bool IsExpired(TimedWindow window)
    {
        return _timing.CurTime >= window.Remaining;
    }

    /// <summary>
    ///     Проверяет, что окно либо null, либо истекло.
    /// </summary>
    public bool NullOrExpired(TimedWindow? window)
    {
        return window == null || IsExpired(window);
    }

    /// <summary>
    ///     Возвращает остаток секунд до конца таймера.
    /// </summary>
    public int GetSecondsRemaining(TimedWindow window)
    {
        var remaining = window.Remaining - _timing.CurTime;
        return Math.Max(0, (int)Math.Ceiling(remaining.TotalSeconds));
    }

    private TimeSpan GetRandomDuration(TimedWindow window)
    {
        if (window.Min == window.Max)
            return window.Min;

        var seconds = _random.NextFloat((float)window.Min.TotalSeconds, (float)window.Max.TotalSeconds);
        return TimeSpan.FromSeconds(seconds);
    }

    private TimeSpan GetRandomDuration(float minSeconds, float maxSeconds)
    {
        if (minSeconds == maxSeconds)
            return TimeSpan.FromSeconds(minSeconds);

        var seconds = _random.NextFloat(minSeconds, maxSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}