using Content.Server.Actions;
using Content.Shared._Sunrise.SunriseStanding;
using Content.Shared._Sunrise.Abilities.Resomi;
using Content.Shared.Throwing;
using Content.Shared.Standing;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.Abilities.Resomi;

public sealed class ResomiSkillSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResomiSkillComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ResomiSkillComponent, ResomiJumpActionEvent>(OnJump);
    }

    private void OnStartup(EntityUid uid, ResomiSkillComponent component, ComponentStartup args) => _action.AddAction(uid, component.ActionJumpId);

    private void OnJump(EntityUid uid, ResomiSkillComponent component, ResomiJumpActionEvent args)
    {
        if (args.Handled || _standing.IsDown(uid))
            return;

        EnsureComp<ResomiActiveAbilityComponent>(uid);

        args.Handled = true;
        var xform = Transform(uid);
        var mapCoords = args.Target.ToMap(EntityManager, _transform);
        var direction = mapCoords.Position - xform.MapPosition.Position;

        if (direction.Length() > component.MaxThrow)
            direction = direction.Normalized() * component.MaxThrow;

        _throwing.TryThrow(uid, direction, component.ThrowSpeed, uid, component.ThrowRange);

        Timer.Spawn(TimeSpan.FromSeconds(1), () =>
        {
            if (Exists(uid))
                RemComp<ResomiActiveAbilityComponent>(uid);
        });
    }
}
