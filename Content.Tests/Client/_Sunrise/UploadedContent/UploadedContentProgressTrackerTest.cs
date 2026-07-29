using System;
using System.Collections.Generic;
using Content.Client._Sunrise.UploadedContent;
using Content.Shared._Sunrise.UploadedContent;
using NUnit.Framework;
using Robust.Shared.Utility;

namespace Content.Tests.Client._Sunrise.UploadedContent;

[TestFixture]
[TestOf(typeof(UploadedContentProgressTracker))]
public sealed class UploadedContentProgressTrackerTest
{
    private readonly HashSet<ResPath> _existingFiles = [];

    [SetUp]
    public void SetUp()
    {
        _existingFiles.Clear();
    }

    [Test]
    public void EmptyManifestIsComplete()
    {
        var tracker = CreateTracker();

        tracker.ApplyManifest([], TimeSpan.Zero);
        var snapshot = tracker.Snapshot;

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.ManifestReceived, Is.True);
            Assert.That(snapshot.TotalFiles, Is.Zero);
            Assert.That(snapshot.TotalBytes, Is.Zero);
            Assert.That(snapshot.IsComplete, Is.True);
            Assert.That(snapshot.HasEstimatedSpeed, Is.False);
        });
    }

    [Test]
    public void ManifestIsDeduplicatedAndOutOfOrderCompletionKeepsTotalsCorrect()
    {
        var tracker = CreateTracker();
        var first = new ResPath("Audio/_Sunrise/first.ogg");
        var second = new ResPath("Audio/_Sunrise/second.ogg");

        tracker.ApplyManifest(
        [
            new UploadedContentManifestEntry(first, 100),
            new UploadedContentManifestEntry(second, 200),
            new UploadedContentManifestEntry(first, 350),
        ], TimeSpan.Zero);

        _existingFiles.Add(second);
        var snapshot = tracker.Update(TimeSpan.FromMilliseconds(100));

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.TotalFiles, Is.EqualTo(2));
            Assert.That(snapshot.TotalBytes, Is.EqualTo(550));
            Assert.That(snapshot.CompletedFiles, Is.EqualTo(1));
            Assert.That(snapshot.CompletedBytes, Is.EqualTo(200));
            Assert.That(snapshot.CurrentFileIndex, Is.Zero);
            Assert.That(snapshot.IsComplete, Is.False);
        });
    }

    [Test]
    public void FilesExistingBeforeManifestDoNotAffectSpeed()
    {
        var tracker = CreateTracker();
        var existing = new ResPath("Audio/_Sunrise/existing.ogg");
        var pending = new ResPath("Audio/_Sunrise/pending.ogg");
        _existingFiles.Add(existing);

        tracker.ApplyManifest(
        [
            new UploadedContentManifestEntry(existing, 100),
            new UploadedContentManifestEntry(pending, 200),
        ], TimeSpan.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(tracker.Snapshot.CompletedFiles, Is.EqualTo(1));
            Assert.That(tracker.Snapshot.CompletedBytes, Is.EqualTo(100));
            Assert.That(tracker.Snapshot.HasEstimatedSpeed, Is.False);
        });

        _existingFiles.Add(pending);
        var completed = tracker.Update(TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(completed.IsComplete, Is.True);
            Assert.That(completed.HasEstimatedSpeed, Is.True);
            Assert.That(completed.EstimatedBytesPerSecond, Is.EqualTo(200).Within(0.001));
        });
    }

    [Test]
    public void SpeedUsesFirstSampleThenEwma()
    {
        var tracker = CreateTracker();
        var first = new ResPath("Audio/_Sunrise/first.ogg");
        var second = new ResPath("Audio/_Sunrise/second.ogg");
        var pending = new ResPath("Audio/_Sunrise/pending.ogg");

        tracker.ApplyManifest(
        [
            new UploadedContentManifestEntry(first, 100),
            new UploadedContentManifestEntry(second, 300),
            new UploadedContentManifestEntry(pending, 1000),
        ], TimeSpan.Zero);

        _existingFiles.Add(first);
        var firstSample = tracker.Update(TimeSpan.FromSeconds(1));
        Assert.That(firstSample.EstimatedBytesPerSecond, Is.EqualTo(100).Within(0.001));

        _existingFiles.Add(second);
        var secondSample = tracker.Update(TimeSpan.FromSeconds(3));

        Assert.Multiple(() =>
        {
            Assert.That(secondSample.HasEstimatedSpeed, Is.True);
            Assert.That(secondSample.EstimatedBytesPerSecond, Is.EqualTo(117.5).Within(0.001));
            Assert.That(secondSample.CompletedBytes, Is.EqualTo(400));
        });
    }

    [Test]
    public void InitialAnimationAndEstimatedLimitUseControlledTime()
    {
        var tracker = CreateTracker();
        var first = new ResPath("Audio/_Sunrise/first.ogg");
        var second = new ResPath("Audio/_Sunrise/second.ogg");
        tracker.ApplyManifest(
        [
            new UploadedContentManifestEntry(first, 100),
            new UploadedContentManifestEntry(second, 100),
        ], TimeSpan.Zero);

        Assert.That(tracker.Snapshot.CurrentFileProgress, Is.EqualTo(0.1f).Within(0.001));
        Assert.That(
            tracker.Update(TimeSpan.FromSeconds(0.375)).CurrentFileProgress,
            Is.EqualTo(0.5f).Within(0.001));
        Assert.That(
            tracker.Update(TimeSpan.FromSeconds(0.75)).CurrentFileProgress,
            Is.EqualTo(0.9f).Within(0.001));

        _existingFiles.Add(first);
        tracker.Update(TimeSpan.FromSeconds(1));

        var belowLimit = tracker.Update(TimeSpan.FromSeconds(1.949));
        Assert.That(belowLimit.CurrentFileProgress, Is.EqualTo(0.949f).Within(0.002));

        var waiting = tracker.Update(TimeSpan.FromSeconds(10));
        Assert.That(waiting.CurrentFileProgress, Is.InRange(0.9f, 0.95f));
    }

    [Test]
    public void FilePollingIsLimitedToOneHundredMilliseconds()
    {
        var tracker = CreateTracker();
        var path = new ResPath("Audio/_Sunrise/file.ogg");
        tracker.ApplyManifest(
            [new UploadedContentManifestEntry(path, 100)],
            TimeSpan.Zero);
        _existingFiles.Add(path);

        Assert.That(
            tracker.Update(TimeSpan.FromMilliseconds(99)).CompletedFiles,
            Is.Zero);
        Assert.That(
            tracker.Update(TimeSpan.FromMilliseconds(100)).CompletedFiles,
            Is.EqualTo(1));
    }

    [Test]
    public void UpdatedFullManifestReplacesPreviousState()
    {
        var tracker = CreateTracker();
        tracker.ApplyManifest(
            [new UploadedContentManifestEntry(new ResPath("Audio/_Sunrise/old.ogg"), 100)],
            TimeSpan.Zero);

        tracker.ApplyManifest(
        [
            new UploadedContentManifestEntry(new ResPath("Audio/_Sunrise/new.ogg"), 200),
            new UploadedContentManifestEntry(new ResPath("/Audio/_Sunrise/new.ogg"), 300),
        ], TimeSpan.FromSeconds(1));

        Assert.Multiple(() =>
        {
            Assert.That(tracker.Snapshot.TotalFiles, Is.EqualTo(1));
            Assert.That(tracker.Snapshot.TotalBytes, Is.EqualTo(300));
            Assert.That(tracker.Snapshot.CompletedFiles, Is.Zero);
            Assert.That(tracker.Snapshot.CurrentFileIndex, Is.Zero);
            Assert.That(tracker.Snapshot.HasEstimatedSpeed, Is.False);
        });
    }

    [Test]
    public void DisconnectOrRetryResetClearsManifestAndProgress()
    {
        var tracker = CreateTracker();
        tracker.ApplyManifest(
            [new UploadedContentManifestEntry(new ResPath("Audio/_Sunrise/file.ogg"), 100)],
            TimeSpan.Zero);

        tracker.Reset();

        Assert.That(tracker.Snapshot, Is.EqualTo(UploadedContentProgressSnapshot.Empty));
    }

    private UploadedContentProgressTracker CreateTracker()
    {
        return new UploadedContentProgressTracker(_existingFiles.Contains);
    }
}
