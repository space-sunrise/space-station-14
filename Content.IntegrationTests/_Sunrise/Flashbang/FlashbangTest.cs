using System.Numerics;
using Content.Shared._Sunrise.Flashbang;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests._Sunrise.Flashbang;

[TestFixture]
[TestOf(typeof(SharedFlashbangSystem))]
public sealed class FlashbangTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: FlashbangTestSource
  name: FlashbangTestSource
  components:
  - type: FlashbangRadiusOnTrigger
    range: 10
    stunDuration: 4
    knockdownDuration: 4
    minEffectStrength: 0.01
    minAmbientPressure: 0

- type: entity
  id: FlashbangTestTarget
  name: FlashbangTestTarget
  components:
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      200: Dead
  - type: Damageable
  - type: StandingState
  - type: StatusEffectContainer
  - type: Physics
    bodyType: KinematicController
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeCircle
          radius: 0.35

- type: entity
  id: FlashbangTestVulnerableTarget
  name: FlashbangTestVulnerableTarget
  parent: FlashbangTestTarget
  components:
  - type: FlashbangVulnerable
    bypassProtection: true
";

    /// <summary>
    /// Цель в эпицентре должна получить стан и нокдаун.
    /// </summary>
    [Test]
    public async Task TargetAtCenter_ShouldReceiveStunAndKnockdown()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var mapSys = entMan.System<SharedMapSystem>();
        var flashbangSys = entMan.System<SharedFlashbangSystem>();

        EntityUid source = default;
        EntityUid target = default;
        MapId mapId = default;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out mapId);
            mapMan.CreateGrid(mapId);
            source = entMan.SpawnEntity("FlashbangTestSource", new MapCoordinates(0f, 0f, mapId));
            target = entMan.SpawnEntity("FlashbangTestTarget", new MapCoordinates(0f, 0f, mapId));
        });

        await pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            flashbangSys.TryFlashbangArea(source, null);
        });

        await pair.RunTicksSync(3);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<StunnedComponent>(target), Is.True, "Цель в эпицентре должна быть оглушена.");
                Assert.That(entMan.HasComponent<KnockedDownComponent>(target), Is.True, "Цель в эпицентре должна упасть.");
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Цель за пределами радиуса не должна получить эффект.
    /// </summary>
    [Test]
    public async Task TargetOutsideRange_ShouldNotBeAffected()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var mapSys = entMan.System<SharedMapSystem>();
        var flashbangSys = entMan.System<SharedFlashbangSystem>();

        EntityUid source = default;
        EntityUid target = default;
        MapId mapId = default;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out mapId);
            mapMan.CreateGrid(mapId);
            source = entMan.SpawnEntity("FlashbangTestSource", new MapCoordinates(0f, 0f, mapId));
            // Range = 10, ставим за пределами
            target = entMan.SpawnEntity("FlashbangTestTarget", new MapCoordinates(15f, 0f, mapId));
        });

        await pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            flashbangSys.TryFlashbangArea(source, null);
        });

        await pair.RunTicksSync(3);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<StunnedComponent>(target), Is.False, "Цель вне радиуса не должна быть оглушена.");
                Assert.That(entMan.HasComponent<KnockedDownComponent>(target), Is.False, "Цель вне радиуса не должна упасть.");
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// Защита через FlashbangProtectionComponent должна уменьшать эффективную дистанцию,
    /// полностью нейтрализуя слабый эффект у края зоны.
    /// </summary>
    [Test]
    public async Task ProtectionComponent_ShouldNeutralizeEdgeEffect()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var mapSys = entMan.System<SharedMapSystem>();
        var flashbangSys = entMan.System<SharedFlashbangSystem>();

        EntityUid source = default;
        EntityUid target = default;
        MapId mapId = default;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out mapId);
            mapMan.CreateGrid(mapId);
            source = entMan.SpawnEntity("FlashbangTestSource", new MapCoordinates(0f, 0f, mapId));
            // Реальная дистанция = 7, coeff = 0.5, effective = 12 >= range 10 → эффект не применяется
            target = entMan.SpawnEntity("FlashbangTestTarget", new MapCoordinates(7f, 0f, mapId));
            // Добавляем защиту напрямую на цель (имитирует шлем/ухо)
            var prot = entMan.AddComponent<FlashbangProtectionComponent>(target);
            prot.ProtectionRangeCoefficient = 0.5f;
            entMan.Dirty(target, prot);
        });

        await pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            flashbangSys.TryFlashbangArea(source, null);
        });

        await pair.RunTicksSync(3);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<StunnedComponent>(target), Is.False, "Защита должна нейтрализовать слабый эффект у края зоны.");
                Assert.That(entMan.HasComponent<KnockedDownComponent>(target), Is.False, "Защита должна предотвратить нокдаун у края зоны.");
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// IgnoreResistances = true должен игнорировать защиту.
    /// </summary>
    [Test]
    public async Task IgnoreResistances_ShouldBypassProtection()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var mapSys = entMan.System<SharedMapSystem>();
        var flashbangSys = entMan.System<SharedFlashbangSystem>();

        EntityUid source = default;
        EntityUid target = default;
        MapId mapId = default;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out mapId);
            mapMan.CreateGrid(mapId);
            source = entMan.SpawnEntity("FlashbangTestSource", new MapCoordinates(0f, 0f, mapId));
            // Реальная дистанция = 7, большая защита
            target = entMan.SpawnEntity("FlashbangTestTarget", new MapCoordinates(7f, 0f, mapId));
            var prot = entMan.AddComponent<FlashbangProtectionComponent>(target);
            prot.ProtectionRangeCoefficient = 1f;
            entMan.Dirty(target, prot);

            // Получаем компонент и включаем IgnoreResistances
            var comp = entMan.GetComponent<FlashbangRadiusOnTriggerComponent>(source);
            comp.IgnoreResistances = true;
            entMan.Dirty(source, comp);
        });

        await pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            flashbangSys.TryFlashbangArea(source, null);
        });

        await pair.RunTicksSync(3);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<StunnedComponent>(target), Is.True, "IgnoreResistances должен игнорировать защиту и оглушить цель.");
                Assert.That(entMan.HasComponent<KnockedDownComponent>(target), Is.True, "IgnoreResistances должен игнорировать защиту и уронить цель.");
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// FlashbangVulnerableComponent должен позволять применить эффект без сбора защиты.
    /// </summary>
    [Test]
    public async Task VulnerableTarget_ShouldBeAffectedDespiteProtection()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var mapSys = entMan.System<SharedMapSystem>();
        var flashbangSys = entMan.System<SharedFlashbangSystem>();

        EntityUid source = default;
        EntityUid target = default;
        MapId mapId = default;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out mapId);
            mapMan.CreateGrid(mapId);
            source = entMan.SpawnEntity("FlashbangTestSource", new MapCoordinates(0f, 0f, mapId));
            target = entMan.SpawnEntity("FlashbangTestVulnerableTarget", new MapCoordinates(3f, 0f, mapId));
            // Добавляем большую защиту — для уязвимой цели она должна игнорироваться
            var prot = entMan.AddComponent<FlashbangProtectionComponent>(target);
            prot.ProtectionRangeCoefficient = 1f;
            entMan.Dirty(target, prot);
        });

        await pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            flashbangSys.TryFlashbangArea(source, null);
        });

        await pair.RunTicksSync(3);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<StunnedComponent>(target), Is.True, "Уязвимая цель должна получить стан несмотря на защиту.");
                Assert.That(entMan.HasComponent<KnockedDownComponent>(target), Is.True, "Уязвимая цель должна упасть несмотря на защиту.");
            });
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// FlashbangAttemptEvent с Cancelled = true должен отменять применение эффекта.
    /// </summary>
    [Test]
    public async Task CancelledAttemptEvent_ShouldPreventEffect()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.ResolveDependency<IEntityManager>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var mapSys = entMan.System<SharedMapSystem>();
        var flashbangSys = entMan.System<SharedFlashbangSystem>();

        EntityUid source = default;
        EntityUid target = default;
        MapId mapId = default;
        bool eventRaised = false;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out mapId);
            mapMan.CreateGrid(mapId);
            source = entMan.SpawnEntity("FlashbangTestSource", new MapCoordinates(0f, 0f, mapId));
            target = entMan.SpawnEntity("FlashbangTestTarget", new MapCoordinates(0f, 0f, mapId));

            // Подписываемся на событие попытки и отменяем
            entMan.EventBus.SubscribeLocalEvent<FlashbangAttemptEvent>(target, (ref FlashbangAttemptEvent ev) =>
            {
                ev.Cancelled = true;
                eventRaised = true;
            });
        });

        await pair.RunTicksSync(5);

        await server.WaitPost(() =>
        {
            flashbangSys.TryFlashbangArea(source, null);
        });

        await pair.RunTicksSync(3);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(eventRaised, Is.True, "FlashbangAttemptEvent должен быть вызван.");
                Assert.That(entMan.HasComponent<StunnedComponent>(target), Is.False, "Отменённая попытка не должна оглушать цель.");
                Assert.That(entMan.HasComponent<KnockedDownComponent>(target), Is.False, "Отменённая попытка не должна ронять цель.");
            });
        });

        await pair.CleanReturnAsync();
    }
}
