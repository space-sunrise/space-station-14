using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Prometheus;
using Robust.Server.DataMetrics;
using Robust.Shared.Asynchronous;
using Robust.Shared.Timing;
using PrometheusMetrics = Prometheus.Metrics;

namespace Content.Server._Sunrise.Tutorial;

/// <summary>
/// Exposes aggregated tutorial completion table data through Prometheus.
/// </summary>
public sealed class TutorialMetricsSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IMetricsManager _metrics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ITaskManager _task = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(1);

    private static readonly Gauge TutorialCompletedPlayers = PrometheusMetrics.CreateGauge(
        "ss14_tutorial_completed_players",
        "Unique players that completed each tutorial sequence.",
        new GaugeConfiguration { LabelNames = ["tutorial_id"] });

    private static readonly Gauge TutorialCompletionCount = PrometheusMetrics.CreateGauge(
        "ss14_tutorial_completion_count",
        "Total completion count recorded for each tutorial sequence.",
        new GaugeConfiguration { LabelNames = ["tutorial_id"] });

    private static readonly Gauge TutorialAccountAgeSamples = PrometheusMetrics.CreateGauge(
        "ss14_tutorial_account_age_samples",
        "Tutorial completion rows with account age data.",
        new GaugeConfiguration { LabelNames = ["tutorial_id"] });

    private static readonly Gauge TutorialAverageAccountAgeDays = PrometheusMetrics.CreateGauge(
        "ss14_tutorial_average_account_age_days",
        "Average account age in days when each tutorial sequence was completed.",
        new GaugeConfiguration { LabelNames = ["tutorial_id"] });

    private static readonly Gauge TutorialLastCompletedAtUnixTime = PrometheusMetrics.CreateGauge(
        "ss14_tutorial_last_completed_at_unixtime",
        "Unix timestamp of the latest completion for each tutorial sequence.",
        new GaugeConfiguration { LabelNames = ["tutorial_id"] });

    private static readonly Gauge TutorialMetricsLastRefreshUnixTime = PrometheusMetrics.CreateGauge(
        "ss14_tutorial_metrics_last_refresh_unixtime",
        "Unix timestamp of the last successful tutorial metrics refresh.");

    private ISawmill _sawmill = default!;
    private CancellationTokenSource? _shutdownToken;
    private TimeSpan _nextRefresh;
    private bool _refreshing;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _log.GetSawmill("tutorial.metrics");
        _shutdownToken = new CancellationTokenSource();
        _metrics.UpdateMetrics += OnUpdateMetrics;
        TryRefreshTutorialMetrics(force: true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _metrics.UpdateMetrics -= OnUpdateMetrics;
        _shutdownToken?.Cancel();
        _shutdownToken?.Dispose();
        _shutdownToken = null;
    }

    private void OnUpdateMetrics()
    {
        TryRefreshTutorialMetrics(force: false);
    }

    private void TryRefreshTutorialMetrics(bool force)
    {
        if (_refreshing)
            return;

        var now = _timing.RealTime;
        if (!force && _nextRefresh > now)
            return;

        _nextRefresh = now + RefreshInterval;
        _refreshing = true;

        var cancel = _shutdownToken?.Token ?? CancellationToken.None;
        _ = RefreshTutorialMetricsAsync(cancel);
    }

    private async Task RefreshTutorialMetricsAsync(CancellationToken cancel)
    {
        List<TutorialCompletionMetrics>? metrics = null;
        Exception? error = null;

        try
        {
            metrics = await _db.GetTutorialCompletionMetricsAsync(cancel);
        }
        catch (OperationCanceledException) when (cancel.IsCancellationRequested)
        {
            return;
        }
        catch (Exception e)
        {
            error = e;
        }

        _task.RunOnMainThread(() =>
        {
            if (_shutdownToken == null || cancel.IsCancellationRequested)
                return;

            if (error != null)
                _sawmill.Warning("Failed to refresh tutorial metrics: {0}", error);
            else if (metrics != null)
                ApplyTutorialMetrics(metrics);

            _refreshing = false;
        });
    }

    private static void ApplyTutorialMetrics(List<TutorialCompletionMetrics> metrics)
    {
        for (var i = 0; i < metrics.Count; i++)
        {
            var metric = metrics[i];
            var tutorialId = metric.TutorialId;

            TutorialCompletedPlayers.WithLabels(tutorialId).Set(metric.CompletedPlayers);
            TutorialCompletionCount.WithLabels(tutorialId).Set(metric.CompletionCount);
            TutorialAccountAgeSamples.WithLabels(tutorialId).Set(metric.AccountAgeSamples);
            TutorialAverageAccountAgeDays.WithLabels(tutorialId).Set(metric.AverageAccountAgeDays ?? 0);
            TutorialLastCompletedAtUnixTime.WithLabels(tutorialId).Set(metric.LastCompletedAt.ToUnixTimeSeconds());
        }

        TutorialMetricsLastRefreshUnixTime.Set(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }
}
