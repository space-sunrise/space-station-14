using Content.Server.Actions;
using Content.Server.Standing;
using Content.Shared._Sunrise.Abilities.Resomi;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Abilities.Resomi;

public sealed class ResomiSkillSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResomiSkillComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ResomiSkillComponent, ResomiJumpActionEvent>(OnJump);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<ResomiSkillComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (curTime >= component.ExpireTime)
                RemComp<ResomiSkillComponent>(uid);
        }
    }

    private void OnStartup(EntityUid uid, ResomiSkillComponent component, ComponentStartup args) => _action.AddAction(uid, component.ActionJumpId);

    private void OnJump(EntityUid uid, ResomiSkillComponent component, ResomiJumpActionEvent args)
    {
        if (args.Handled || _standing.IsDown(uid))
            return;

        var activeComponent = EnsureComp<ResomiSkillComponent>(uid);
        activeComponent.ExpireTime = _gameTiming.CurTime + component.ExpireTime;

        args.Handled = true;
        var xform = Transform(uid);
        var mapCoords = args.Target.ToMap(EntityManager, _transform);
        var direction = mapCoords.Position - xform.MapPosition.Position;

        if (direction.Length() > component.MaxThrow)
            direction = direction.Normalized() * component.MaxThrow;

        _throwing.TryThrow(uid, direction, component.ThrowSpeed, uid, component.ThrowRange);
    }
}
