using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Screech;

public sealed class XenoScreechSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly HashSet<Entity<HumanoidAppearanceComponent>> _humans = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoScreechComponent, XenoScreechActionEvent>(OnXenoScreechAction);
    }

    private void OnXenoScreechAction(Entity<XenoScreechComponent> xeno, ref XenoScreechActionEvent args)
    {
        if (args.Handled)
            return;

        var attempt = new XenoScreechAttemptEvent();
        RaiseLocalEvent(xeno, ref attempt);

        if (attempt.Cancelled)
            return;

        if (!TryComp(xeno, out TransformComponent? xform))
            return;

        args.Handled = true;

        if (_net.IsServer)
            _audio.PlayPvs(xeno.Comp.Sound, xeno);

        _humans.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.StunRange, _humans);

        foreach (var receiver in _humans)
        {
            Stun(xeno, receiver, xeno.Comp.StunTime, true);
        }

        _humans.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.ParalyzeRange, _humans);

        foreach (var receiver in _humans)
        {
            Stun(xeno, receiver, xeno.Comp.ParalyzeTime, false);
        }

        _humans.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, xeno.Comp.ParasiteStunRange, _humans);

        foreach (var receiver in _humans)
        {
            Stun(xeno, receiver, xeno.Comp.ParasiteStunTime, true);
        }

        if (_net.IsServer)
            SpawnAttachedTo(xeno.Comp.Effect, xeno.Owner.ToCoordinates());
    }

    private void Stun(EntityUid xeno, EntityUid receiver, TimeSpan time, bool stun)
    {
        if (_mobState.IsDead(receiver))
            return;

        if (!_examineSystem.InRangeUnOccluded(xeno, receiver))
            return;

        if (stun)
            _stun.TryStun(receiver, time, true);
        else
            _stun.TryParalyze(receiver, time, true);
    }
}
