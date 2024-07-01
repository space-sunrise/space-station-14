﻿using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Content.Shared._Sunrise.SunriseCCVars;
using Prometheus;
using Robust.Shared.Configuration;

namespace Content.Server._Sunrise.TTS;

// ReSharper disable once InconsistentNaming
public sealed class TTSManager
{
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = new[] {"type"},
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter WantedRadioCount = Metrics.CreateCounter(
        "tts_wanted_radio_count",
        "Amount of wanted TTS radio audio.");

    private static readonly Counter WantedAnnounceCount = Metrics.CreateCounter(
        "tts_wanted_Announce_count",
        "Amount of wanted TTS Announce audio.");

    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private string _apiUrl = string.Empty;
    private string _apiToken = string.Empty;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(SunriseCCVars.TTSApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(SunriseCCVars.TTSApiToken, v =>
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", v);
            _apiToken = v;
        }, true);
    }

    /// <summary>
    /// Generates audio with passed text by API
    /// </summary>
    /// <param name="speaker">Identifier of speaker</param>
    /// <param name="text">SSML formatted text</param>
    /// <returns>OGG audio bytes or null if failed</returns>
    public async Task<byte[]?> ConvertTextToSpeech(string speaker, string text, string? effect = null)
    {
        WantedCount.Inc();
        _sawmill.Verbose($"Generate new audio for '{text}' speech by '{speaker}' speaker");

        var body = new GenerateVoiceRequest
        {
            Text = text,
            Speaker = speaker,
            // Pitch = pitch,
            // Rate = rate,
            Effect = effect
        };

        var request = CreateRequestLink(_apiUrl, body);

        var reqTime = DateTime.UtcNow;
        try
        {
            var timeout = _cfg.GetCVar(SunriseCCVars.TTSApiTimeout);
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            var response = await _httpClient.GetAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    _sawmill.Warning("TTS request was rate limited");
                    return null;
                }

                _sawmill.Error($"TTS request returned bad status code: {response.StatusCode}");
                return null;
            }

            var soundData = await response.Content.ReadAsByteArrayAsync(cts.Token);

            _sawmill.Debug($"Generated new audio for '{text}' speech by '{speaker}' speaker ({soundData.Length} bytes)");
            RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);

            return soundData;
        }
        catch (TaskCanceledException)
        {
            RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Timeout of request generation new audio for '{text}' speech by '{speaker}' speaker");
            return null;
        }
        catch (Exception e)
        {
            RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
            _sawmill.Error($"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");
            return null;
        }
    }

    private static string CreateRequestLink(string url, GenerateVoiceRequest body)
    {
        var uriBuilder = new UriBuilder(url);
        var query = HttpUtility.ParseQueryString(uriBuilder.Query);
        query["speaker"] = body.Speaker;
        query["text"] = body.Text;
        query["pitch"] = body.Pitch;
        query["rate"] = body.Rate;
        query["file"] = "1";
        query["ext"] = "ogg";
        if (body.Effect != null)
            query["effect"] = body.Effect;

        uriBuilder.Query = query.ToString();
        return uriBuilder.ToString();
    }

    public async Task<byte[]?> ConvertTextToSpeechRadio(string speaker, string text)
    {
        WantedRadioCount.Inc();
        var soundData = await ConvertTextToSpeech(speaker, text, "radio");

        return soundData;
    }

    public async Task<byte[]?> ConvertTextToSpeechAnnounce(string speaker, string text)
    {
        WantedAnnounceCount.Inc();
        var soundData = await ConvertTextToSpeech(speaker, text, "announce");

        return soundData;
    }

    private record GenerateVoiceRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; } = default!;

        [JsonPropertyName("speaker")]
        public string Speaker { get; set; } = default!;

        [JsonPropertyName("pitch")]
        public string Pitch { get; set; } = default!;

        [JsonPropertyName("rate")]
        public string Rate { get; set; } = default!;

        [JsonPropertyName("effect")]
        public string? Effect { get; set; }
    }
}
