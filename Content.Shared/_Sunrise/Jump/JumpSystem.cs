using Content.Shared._Sunrise.Animations;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Shared._Sunrise.Jump;

public class SharedJumpSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<FixturesComponent> _fixturesQuery;

    public override void Initialize()
    {
        SubscribeLocalEvent<JumpComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<JumpComponent, ComponentShutdown>(OnShutdown);

        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();
    }

    private void OnStartup(Entity<JumpComponent> ent, ref ComponentStartup args)
    {
        if (!_physicsQuery.TryGetComponent(ent.Owner, out var body) ||
            !_fixturesQuery.TryGetComponent(ent.Owner, out var fixtures))
            return;

        _physics.SetBodyStatus(ent.Owner, body, BodyStatus.InAir);
        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            _physics.RemoveCollisionMask(ent.Owner, id, fixture, (int) CollisionGroup.TableLayer, manager: fixtures);
            _physics.RemoveCollisionMask(ent.Owner, id, fixture, (int) CollisionGroup.BulletImpassable, manager: fixtures);
            _physics.RemoveCollisionMask(ent.Owner, id, fixture, (int) CollisionGroup.CrateMask, manager: fixtures);
        }
    }

    private void OnShutdown(Entity<JumpComponent> ent, ref ComponentShutdown args)
    {
        if (!_physicsQuery.TryGetComponent(ent.Owner, out var body) ||
            !_fixturesQuery.TryGetComponent(ent.Owner, out var fixtures))
            return;

        _physics.SetBodyStatus(ent.Owner, body, BodyStatus.OnGround);
        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            _physics.AddCollisionMask(ent.Owner, id, fixture, (int) CollisionGroup.TableLayer, manager: fixtures);
            _physics.AddCollisionMask(ent.Owner, id, fixture, (int) CollisionGroup.BulletImpassable, manager: fixtures);
            _physics.AddCollisionMask(ent.Owner, id, fixture, (int) CollisionGroup.CrateMask, manager: fixtures);
        }
    }
}
