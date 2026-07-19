#nullable enable
using System.Collections.Generic;
using Content.Server._Sunrise.Shuttles.Components;
using Content.Server._Sunrise.Shuttles.Systems;
using Content.Server.Shuttles;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Tests;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.Tests._Sunrise.Shuttles;

[TestFixture]
public sealed class SunriseArrivalsReservationTest : ContentUnitTest
{
    private const string ArrivalDockTag = "DockArrivals";

    [Test]
    public async Task OverlappingAndTouchingAreasCannotBeReservedTogether()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var arrivalsSystem = entityManager.System<SunriseArrivalsSystem>();

        var firstDispatched = false;
        var overlappingDispatched = false;
        var touchingDispatched = false;
        var marginDispatched = false;

        await server.WaitPost(() =>
        {
            var targetGrid = SpawnEntity(entityManager);
            var first = CreateArrivalShuttle(entityManager);
            var overlapping = CreateArrivalShuttle(entityManager);
            var touching = CreateArrivalShuttle(entityManager);
            var withinMargin = CreateArrivalShuttle(entityManager);

            firstDispatched = Dispatch(arrivalsSystem,
                entityManager,
                first,
                targetGrid,
                new Box2(0f, 0f, 2f, 2f));
            overlappingDispatched = Dispatch(arrivalsSystem,
                entityManager,
                overlapping,
                targetGrid,
                new Box2(1f, 0f, 3f, 2f));
            touchingDispatched = Dispatch(arrivalsSystem,
                entityManager,
                touching,
                targetGrid,
                new Box2(2f, 0f, 4f, 2f));
            marginDispatched = Dispatch(arrivalsSystem,
                entityManager,
                withinMargin,
                targetGrid,
                new Box2(2.005f, 0f, 4.005f, 2f));
        });

        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(firstDispatched, Is.True);
            Assert.That(overlappingDispatched, Is.False);
            Assert.That(touchingDispatched, Is.False);
            Assert.That(marginDispatched, Is.False);
        }));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DistantAreasOnSameGridCanBeReservedTogether()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var arrivalsSystem = entityManager.System<SunriseArrivalsSystem>();

        var firstDispatched = false;
        var secondDispatched = false;

        await server.WaitPost(() =>
        {
            var targetGrid = SpawnEntity(entityManager);
            var first = CreateArrivalShuttle(entityManager);
            var second = CreateArrivalShuttle(entityManager);

            firstDispatched = Dispatch(arrivalsSystem,
                entityManager,
                first,
                targetGrid,
                new Box2(0f, 0f, 2f, 2f));
            secondDispatched = Dispatch(arrivalsSystem,
                entityManager,
                second,
                targetGrid,
                new Box2(2.02f, 0f, 4.02f, 2f));
        });

        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(firstDispatched, Is.True);
            Assert.That(secondDispatched, Is.True);
        }));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SameLocalAreaOnDifferentTargetGridsDoesNotConflict()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var arrivalsSystem = entityManager.System<SunriseArrivalsSystem>();

        var firstDispatched = false;
        var secondDispatched = false;

        await server.WaitPost(() =>
        {
            var firstTargetGrid = SpawnEntity(entityManager);
            var secondTargetGrid = SpawnEntity(entityManager);
            var first = CreateArrivalShuttle(entityManager);
            var second = CreateArrivalShuttle(entityManager);
            var area = new Box2(0f, 0f, 2f, 2f);

            firstDispatched = Dispatch(arrivalsSystem, entityManager, first, firstTargetGrid, area);
            secondDispatched = Dispatch(arrivalsSystem, entityManager, second, secondTargetGrid, area);
        });

        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(firstDispatched, Is.True);
            Assert.That(secondDispatched, Is.True);
        }));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DockedShuttleKeepsItsAreaReserved()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var arrivalsSystem = entityManager.System<SunriseArrivalsSystem>();

        var firstDispatched = false;
        var secondDispatched = false;
        SunriseArrivalsShuttleComponent? firstArrivals = null;

        await server.WaitPost(() =>
        {
            var targetGrid = SpawnEntity(entityManager);
            var first = CreateArrivalShuttle(entityManager);
            var second = CreateArrivalShuttle(entityManager);
            var area = new Box2(0f, 0f, 2f, 2f);

            firstDispatched = Dispatch(arrivalsSystem, entityManager, first, targetGrid, area);
            first.Arrivals.State = SunriseArrivalsShuttleState.Docked;
            firstArrivals = first.Arrivals;
            secondDispatched = Dispatch(arrivalsSystem, entityManager, second, targetGrid, area);
        });

        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(firstDispatched, Is.True);
            Assert.That(firstArrivals!.ReservedDockingArea, Is.Not.Null);
            Assert.That(secondDispatched, Is.False);
        }));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DepartureRemovalAndFailedDockingReleaseReservation()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var arrivalsSystem = entityManager.System<SunriseArrivalsSystem>();

        var targetGrid = EntityUid.Invalid;
        var departurePort = EntityUid.Invalid;
        var foreignDeparturePort = EntityUid.Invalid;
        var foreignReservationOwner = EntityUid.Invalid;
        var removalPort = EntityUid.Invalid;
        var failedDockingPort = EntityUid.Invalid;
        var departureArea = new Box2(0f, 0f, 2f, 2f);
        var removalArea = new Box2(4f, 0f, 6f, 2f);
        var failedDockingArea = new Box2(8f, 0f, 10f, 2f);
        var departureDispatched = false;
        var removalDispatched = false;
        var failedDockingDispatched = false;
        SunriseArrivalsShuttleComponent? failedArrivals = null;

        await server.WaitPost(() =>
        {
            targetGrid = SpawnEntity(entityManager);
            var departure = CreateArrivalShuttle(entityManager);
            var removal = CreateArrivalShuttle(entityManager);
            var failedDocking = CreateArrivalShuttle(entityManager);

            departurePort = SpawnEntity(entityManager);
            foreignDeparturePort = SpawnEntity(entityManager);
            foreignReservationOwner = SpawnEntity(entityManager);
            removalPort = SpawnEntity(entityManager);
            failedDockingPort = SpawnEntity(entityManager);

            departureDispatched = Dispatch(arrivalsSystem,
                entityManager,
                departure,
                targetGrid,
                departureArea,
                departurePort,
                foreignDeparturePort);
            removalDispatched = Dispatch(arrivalsSystem,
                entityManager,
                removal,
                targetGrid,
                removalArea,
                removalPort);
            failedDockingDispatched = Dispatch(arrivalsSystem,
                entityManager,
                failedDocking,
                targetGrid,
                failedDockingArea,
                failedDockingPort);

            entityManager.GetComponent<FtlReservationComponent>(foreignDeparturePort).ReservedBy = foreignReservationOwner;
            arrivalsSystem.StartDeparture(departure.Uid, departure.Arrivals);
            entityManager.RemoveComponent<SunriseArrivalsShuttleComponent>(removal.Uid);

            var completed = new FTLCompletedEvent(failedDocking.Uid, targetGrid);
            entityManager.EventBus.RaiseLocalEvent(failedDocking.Uid, ref completed);
            failedArrivals = failedDocking.Arrivals;
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(departureDispatched, Is.True);
            Assert.That(removalDispatched, Is.True);
            Assert.That(failedDockingDispatched, Is.True);
            Assert.That(arrivalsSystem.IsArrivalAreaAvailable(EntityUid.Invalid, targetGrid, departureArea), Is.True);
            Assert.That(arrivalsSystem.IsArrivalAreaAvailable(EntityUid.Invalid, targetGrid, removalArea), Is.True);
            Assert.That(arrivalsSystem.IsArrivalAreaAvailable(EntityUid.Invalid, targetGrid, failedDockingArea), Is.True);
            Assert.That(entityManager.HasComponent<FtlReservationComponent>(departurePort), Is.False);
            Assert.That(entityManager.GetComponent<FtlReservationComponent>(foreignDeparturePort).ReservedBy,
                Is.EqualTo(foreignReservationOwner));
            Assert.That(entityManager.HasComponent<FtlReservationComponent>(removalPort), Is.False);
            Assert.That(entityManager.HasComponent<FtlReservationComponent>(failedDockingPort), Is.False);
            Assert.That(failedArrivals!.State, Is.EqualTo(SunriseArrivalsShuttleState.Queued));
        }));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task FailedDockingRestoresHoldingFtlAfterOldComponentRemoval()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var arrivalsSystem = entityManager.System<SunriseArrivalsSystem>();

        var dispatched = false;
        var restoredBeforeRemoval = false;
        var queueContinued = false;
        var restoredAfterRemoval = false;
        FTLComponent? restoredFtl = null;
        SunriseArrivalsShuttleComponent? failedArrivals = null;

        await server.WaitPost(() =>
        {
            var targetGrid = SpawnEntity(entityManager);
            var shuttleUid = map.Grid;
            var arrivals = entityManager.EnsureComponent<SunriseArrivalsShuttleComponent>(shuttleUid);
            var oldFtl = entityManager.EnsureComponent<FTLComponent>(shuttleUid);
            var shuttle = entityManager.GetComponent<ShuttleComponent>(shuttleUid);
            arrivals.State = SunriseArrivalsShuttleState.Queued;
            oldFtl.State = FTLState.Travelling;

            var failed = (Uid: shuttleUid, Arrivals: arrivals, Ftl: oldFtl);
            var area = new Box2(0f, 0f, 2f, 2f);
            dispatched = Dispatch(arrivalsSystem, entityManager, failed, targetGrid, area);

            var completed = new FTLCompletedEvent(shuttleUid, targetGrid);
            entityManager.EventBus.RaiseLocalEvent(shuttleUid, ref completed);
            failedArrivals = arrivals;

            restoredBeforeRemoval = arrivalsSystem.TryRestoreHoldingFtl(shuttleUid, arrivals, shuttle);

            var next = CreateArrivalShuttle(entityManager);
            queueContinued = Dispatch(arrivalsSystem, entityManager, next, targetGrid, area);

            entityManager.RemoveComponent<FTLComponent>(shuttleUid);
            restoredAfterRemoval = arrivalsSystem.TryRestoreHoldingFtl(shuttleUid, arrivals, shuttle);
            entityManager.TryGetComponent(shuttleUid, out restoredFtl);
        });

        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(dispatched, Is.True);
            Assert.That(failedArrivals!.State, Is.EqualTo(SunriseArrivalsShuttleState.Queued));
            Assert.That(restoredBeforeRemoval, Is.False);
            Assert.That(queueContinued, Is.True);
            Assert.That(restoredAfterRemoval, Is.True);
            Assert.That(restoredFtl, Is.Not.Null);
            Assert.That(restoredFtl!.State, Is.EqualTo(FTLState.Starting));
            Assert.That(restoredFtl.TravelTime, Is.EqualTo(3600f));
        }));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ConsecutiveDispatchesChooseOnlyNonConflictingRankedConfigurations()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var arrivalsSystem = entityManager.System<SunriseArrivalsSystem>();

        var firstDispatched = false;
        var secondDispatched = false;
        SunriseArrivalsShuttleComponent? firstArrivals = null;
        SunriseArrivalsShuttleComponent? secondArrivals = null;
        FTLComponent? secondFtl = null;
        EntityUid[] selectedPorts = [];

        await server.WaitPost(() =>
        {
            var targetGrid = SpawnEntity(entityManager);
            var first = CreateArrivalShuttle(entityManager);
            var second = CreateArrivalShuttle(entityManager);
            var occupiedArea = new Box2(0f, 0f, 2f, 2f);
            var safeArea = new Box2(4f, 0f, 6f, 2f);

            firstDispatched = Dispatch(arrivalsSystem, entityManager, first, targetGrid, occupiedArea);
            firstArrivals = first.Arrivals;

            var conflictingPriorityPort = SpawnPriorityDock(entityManager);
            var nonPriorityPorts = SpawnDocks(entityManager, 3);
            var onePriorityPort = SpawnPriorityDock(entityManager);
            var farPriorityPorts = SpawnDocks(entityManager, 2);
            entityManager.EnsureComponent<PriorityDockComponent>(farPriorityPorts[0]).Tag = ArrivalDockTag;
            selectedPorts = SpawnDocks(entityManager, 2);
            entityManager.EnsureComponent<PriorityDockComponent>(selectedPorts[0]).Tag = ArrivalDockTag;

            var configs = new List<DockingConfig>
            {
                CreateDockingConfig(entityManager,
                    targetGrid,
                    occupiedArea,
                    Angle.Zero,
                    conflictingPriorityPort),
                CreateDockingConfig(entityManager,
                    targetGrid,
                    safeArea,
                    Angle.FromDegrees(1),
                    nonPriorityPorts),
                CreateDockingConfig(entityManager,
                    targetGrid,
                    safeArea,
                    Angle.FromDegrees(1),
                    onePriorityPort),
                CreateDockingConfig(entityManager,
                    targetGrid,
                    safeArea,
                    Angle.FromDegrees(30),
                    farPriorityPorts),
                CreateDockingConfig(entityManager,
                    targetGrid,
                    safeArea,
                    Angle.FromDegrees(5),
                    selectedPorts),
            };

            secondDispatched = arrivalsSystem.TryDispatchShuttleToSafeConfig(second.Uid,
                second.Arrivals,
                second.Ftl,
                targetGrid,
                configs);
            secondArrivals = second.Arrivals;
            secondFtl = second.Ftl;
        });

        await server.WaitAssertion(() => Assert.Multiple(() =>
        {
            Assert.That(firstDispatched, Is.True);
            Assert.That(secondDispatched, Is.True);
            Assert.That(firstArrivals!.ReservedDockingArea, Is.Not.Null);
            Assert.That(secondArrivals!.ReservedDockingArea, Is.Not.Null);
            Assert.That(firstArrivals.ReservedDockingArea!.Value.Enlarged(0.01f)
                .Intersects(secondArrivals.ReservedDockingArea!.Value), Is.False);
            Assert.That(secondFtl!.TargetAngle, Is.EqualTo(Angle.FromDegrees(5)));
            Assert.That(secondArrivals.ReservedDocks, Is.EquivalentTo(selectedPorts));
        }));

        await pair.CleanReturnAsync();
    }

    private static bool Dispatch(
        SunriseArrivalsSystem arrivalsSystem,
        IEntityManager entityManager,
        (EntityUid Uid, SunriseArrivalsShuttleComponent Arrivals, FTLComponent Ftl) shuttle,
        EntityUid targetGrid,
        Box2 area,
        params EntityUid[] targetDocks)
    {
        if (targetDocks.Length == 0)
            targetDocks = [SpawnEntity(entityManager)];

        var config = CreateDockingConfig(entityManager, targetGrid, area, Angle.Zero, targetDocks);
        return arrivalsSystem.TryDispatchShuttleToSafeConfig(shuttle.Uid,
            shuttle.Arrivals,
            shuttle.Ftl,
            targetGrid,
            [config]);
    }

    private static (EntityUid Uid, SunriseArrivalsShuttleComponent Arrivals, FTLComponent Ftl)
        CreateArrivalShuttle(IEntityManager entityManager)
    {
        var uid = SpawnEntity(entityManager);
        var arrivals = entityManager.EnsureComponent<SunriseArrivalsShuttleComponent>(uid);
        var ftl = entityManager.EnsureComponent<FTLComponent>(uid);
        arrivals.State = SunriseArrivalsShuttleState.Queued;
        ftl.State = FTLState.Travelling;
        return (uid, arrivals, ftl);
    }

    private static DockingConfig CreateDockingConfig(
        IEntityManager entityManager,
        EntityUid targetGrid,
        Box2 area,
        Angle angle,
        params EntityUid[] targetDocks)
    {
        var config = new DockingConfig
        {
            TargetGrid = targetGrid,
            Area = area,
            Coordinates = new EntityCoordinates(targetGrid, area.Center),
            Angle = angle,
        };

        foreach (var targetDockUid in targetDocks)
        {
            var shuttleDockUid = SpawnEntity(entityManager);
            var shuttleDock = entityManager.EnsureComponent<DockingComponent>(shuttleDockUid);
            var targetDock = entityManager.EnsureComponent<DockingComponent>(targetDockUid);
            config.Docks.Add((shuttleDockUid, targetDockUid, shuttleDock, targetDock));
        }

        return config;
    }

    private static EntityUid SpawnPriorityDock(IEntityManager entityManager)
    {
        var dock = SpawnEntity(entityManager);
        entityManager.EnsureComponent<PriorityDockComponent>(dock).Tag = ArrivalDockTag;
        return dock;
    }

    private static EntityUid[] SpawnDocks(IEntityManager entityManager, int count)
    {
        var docks = new EntityUid[count];
        for (var i = 0; i < count; i++)
        {
            docks[i] = SpawnEntity(entityManager);
        }

        return docks;
    }

    private static EntityUid SpawnEntity(IEntityManager entityManager)
    {
        return entityManager.SpawnEntity(null, MapCoordinates.Nullspace);
    }
}
