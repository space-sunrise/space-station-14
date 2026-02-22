// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Server.Antag;
using Content.Server._Nox.Disease.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Mind;
using Robust.Shared.Player;

namespace Content.Server._Nox.Disease.Systems;

public sealed class SpawnAntagAfterSelectedRule : GameRuleSystem<SpawnAntagAfterSelectedComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly GhostRoleSystem _ghost = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnAntagAfterSelectedComponent, AfterAntagEntitySelectedEvent>(AfterAntagEntitySelected);
    }

    private void AfterAntagEntitySelected(Entity<SpawnAntagAfterSelectedComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (!_mind.TryGetMind(args.EntityUid, out var mindId, out var mind))
            return;

        if (!_player.TryGetSessionById(mind.UserId, out var session))
            return;

        var antag = Spawn(ent.Comp.Prototype, Transform(args.EntityUid).Coordinates);

        if (TryComp<GhostRoleComponent>(antag, out var ghostRoleComponent))
            _ghost.Takeover(session, ghostRoleComponent.Identifier);
        else
            _mind.ControlMob(args.EntityUid, antag);

        QueueDel(args.EntityUid);
    }

}
