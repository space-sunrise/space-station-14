using System;
using System.Reflection;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server._Sunrise.GameTicking;
using Content.Sunrise.Interfaces.Server;
using Moq;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Tests.Server._Sunrise.GameTicking;

[TestFixture]
public sealed class SunriseServerJoinPipelineSystemTests
{
    [Test]
    public void QueueAbsentIsDisabled()
    {
        Assert.That(SunriseServerJoinPipelineSystem.IsJoinQueueEnabled(null), Is.False);
    }

    [Test]
    public void RegisteredDisabledQueueIsDisabled()
    {
        Assert.That(SunriseServerJoinPipelineSystem.IsJoinQueueEnabled(new TestJoinQueueManager(false)), Is.False);
    }

    [Test]
    public void EnabledQueueIsEnabled()
    {
        Assert.That(SunriseServerJoinPipelineSystem.IsJoinQueueEnabled(new TestJoinQueueManager(true)), Is.True);
    }

    [Test]
    public void SendToJoinPipelineWithEnabledQueueStartsUserDbOnce()
    {
        var userDb = CreateUserDb();
        var pipeline = CreatePipeline(userDb);
        var session = CreateSession();
        var queue = new TestJoinQueueManager(true);

        Assert.Multiple(() =>
        {
            Assert.That(pipeline.SendToJoinPipeline(session, queue), Is.True);
            Assert.That(pipeline.SendToJoinPipeline(session, queue), Is.False);
            Assert.That(userDb.GetLoadTask(session), Is.Not.Null);
            Assert.That(queue.ReadyToJoinCount, Is.EqualTo(1));
            Assert.That(pipeline.StopUserDbLoad(session), Is.True);
            Assert.That(pipeline.StopUserDbLoad(session), Is.False);
        });
    }

    [Test]
    public void StartUserDbLoadIfJoinQueueDisabledRespectsQueueState()
    {
        var enabledUserDb = CreateUserDb();
        var enabledPipeline = CreatePipeline(enabledUserDb);
        var enabledSession = CreateSession();

        var disabledUserDb = CreateUserDb();
        var disabledPipeline = CreatePipeline(disabledUserDb);
        var disabledSession = CreateSession();

        Assert.Multiple(() =>
        {
            Assert.That(
                enabledPipeline.StartUserDbLoadIfJoinQueueDisabled(enabledSession, new TestJoinQueueManager(true)),
                Is.False);
            Assert.That(enabledPipeline.StopUserDbLoad(enabledSession), Is.False);

            Assert.That(
                disabledPipeline.StartUserDbLoadIfJoinQueueDisabled(disabledSession, new TestJoinQueueManager(false)),
                Is.True);
            Assert.That(disabledUserDb.GetLoadTask(disabledSession), Is.Not.Null);
            Assert.That(disabledPipeline.StopUserDbLoad(disabledSession), Is.True);
        });
    }

    [Test]
    public void StopUserDbLoadWithoutStartedLoadDoesNotTouchUserDb()
    {
        var pipeline = CreatePipeline(CreateUserDb());
        var session = CreateSession();

        Assert.DoesNotThrow(() => Assert.That(pipeline.StopUserDbLoad(session), Is.False));
    }

    [Test]
    public async Task QueueFailureStopsUserDbLoadAndDisconnects()
    {
        var userDb = CreateUserDb();
        var pipeline = CreatePipeline(userDb);
        var channel = new Mock<INetChannel>();
        var disconnected = false;
        channel
            .Setup(c => c.Disconnect(It.IsAny<string>()))
            .Callback(() => disconnected = true);

        var session = CreateSession(channel);
        var queue = new TestJoinQueueManager(
            true,
            _ => Task.FromException(new InvalidOperationException("queue failed")));

        Assert.That(pipeline.SendToJoinPipeline(session, queue), Is.True);
        await WaitUntil(() => disconnected);

        Assert.That(pipeline.StopUserDbLoad(session), Is.False);
        channel.Verify(c => c.Disconnect(It.Is<string>(reason => reason.Contains("reconnect"))), Times.Once);
    }

    private static SunriseServerJoinPipelineSystem CreatePipeline(UserDbDataManager userDb)
    {
        var player = new Mock<IPlayerManager>();
        var pipeline = new SunriseServerJoinPipelineSystem();
        typeof(SunriseServerJoinPipelineSystem)
            .GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(pipeline, player.Object);
        typeof(SunriseServerJoinPipelineSystem)
            .GetField("_userDb", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(pipeline, userDb);

        return pipeline;
    }

    private static UserDbDataManager CreateUserDb()
    {
        var manager = new UserDbDataManager();
        var sawmill = new LogManager().GetSawmill("sunrise-join-pipeline-test");
        typeof(UserDbDataManager)
            .GetField("_sawmill", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(manager, sawmill);

        return manager;
    }

    private static ICommonSession CreateSession(Mock<INetChannel>? channel = null)
    {
        var session = new Mock<ICommonSession>();
        session.SetupGet(s => s.UserId).Returns(new NetUserId(Guid.NewGuid()));
        session.SetupGet(s => s.Status).Returns(SessionStatus.Connected);
        session.SetupGet(s => s.Channel).Returns((channel ?? new Mock<INetChannel>()).Object);
        session.Setup(s => s.ToString()).Returns("test-session");
        return session.Object;
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 50; i++)
        {
            if (condition())
                return;

            await Task.Delay(10);
        }

        Assert.Fail("Condition was not met in time.");
    }

    private sealed class TestJoinQueueManager : IServerJoinQueueManager
    {
        private readonly Func<ICommonSession, Task> _handleReadyToJoin;

        public TestJoinQueueManager(
            bool isEnabled,
            Func<ICommonSession, Task>? handleReadyToJoin = null)
        {
            IsEnabled = isEnabled;
            _handleReadyToJoin = handleReadyToJoin ?? (_ => Task.CompletedTask);
        }

        public bool IsEnabled { get; }
        public int PlayerInQueueCount => 0;
        public int ActualPlayersCount => 0;
        public void Initialize() {}
        public void PostInitialize() {}
        public int ReadyToJoinCount { get; private set; }
        public Task HandleReadyToJoin(ICommonSession session)
        {
            ReadyToJoinCount++;
            return _handleReadyToJoin(session);
        }
    }
}
