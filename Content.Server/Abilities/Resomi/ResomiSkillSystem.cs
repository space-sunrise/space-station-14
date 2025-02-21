using Content.Server.Actions;
using Content.Shared.Abilities.Resomi;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;

namespace Content.Server.Abilities.Resomi;

public sealed class ResomiSkillSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ResomiSkillComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ResomiSkillComponent, ResomiJumpActionEvent>(OnJump);
    }
    
    private void OnStartup(EntityUid uid, ResomiSkillComponent component, ComponentStartup args) => _action.AddAction(uid, component.ActionJumpId);
    
    private void OnJump(EntityUid uid, ResomiSkillComponent component, ResomiJumpActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var xform = Transform(uid);
        var mapCoords = args.Target.ToMap(EntityManager, _transform);
        var direction = mapCoords.Position - xform.MapPosition.Position;

        if (direction.Length() > component.MaxThrow)
            direction = direction.Normalized() * component.MaxThrow;

        _throwing.TryThrow(uid, direction, 7F, uid, 10F);
    }
}
