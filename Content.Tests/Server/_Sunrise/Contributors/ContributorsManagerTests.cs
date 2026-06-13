using System.Reflection;
using Content.Server._Sunrise.Contributors;
using Content.Shared._Sunrise.SunriseCCVars;
using Moq;
using NUnit.Framework;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.UnitTesting;

namespace Content.Tests.Server._Sunrise.Contributors;

[TestFixture]
public sealed class ContributorsManagerTests
{
    [Test]
    public void InitializeAppliesDefaultEnabledState()
    {
        var manager = CreateManager(out _);

        manager.Initialize();

        Assert.That(GetEnabled(manager), Is.True);
    }

    [Test]
    public void InitializeAppliesConfiguredDisabledStateImmediately()
    {
        var manager = CreateManager(out var cfg);
        cfg.SetCVar(SunriseCCVars.ContributorsEnable, false);

        manager.Initialize();

        Assert.That(GetEnabled(manager), Is.False);
    }

    private static ContributorsManager CreateManager(out IConfigurationManager cfg)
    {
        var timing = new Mock<IGameTiming>();
        cfg = MockInterfaces.MakeConfigurationManager(
            timing.Object,
            new LogManager(),
            loadCvarsFromTypes: [typeof(SunriseCCVars)]);

        var manager = new ContributorsManager();
        SetField(manager, "_playerManager", new Mock<IPlayerManager>().Object);
        SetField(manager, "_logManager", new LogManager());
        SetField(manager, "_cfg", cfg);
        SetField(manager, "_netMgr", new Mock<IServerNetManager>().Object);
        SetField(manager, "_timing", timing.Object);
        return manager;
    }

    private static bool GetEnabled(ContributorsManager manager)
    {
        return (bool) typeof(ContributorsManager)
            .GetField("_enable", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(manager)!;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(target, value);
    }
}
