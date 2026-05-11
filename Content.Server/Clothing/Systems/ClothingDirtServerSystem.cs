using Content.Server.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing.Dirt;
using Content.Shared.Damage;
using Content.Shared.Fluids.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Server.Clothing.Dirt;

public sealed class ClothingDirtServerSystem : SharedClothingDirtSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly TransformSystem _xform = default!;
    [Dependency] private readonly SolutionContainerSystem _solutions = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;

    // uid моба -> (когда лёг, цвет лужи)
    // чистим при вставании и при выходе из лужи
    private readonly Dictionary<EntityUid, (TimeSpan At, Color Color)> _lying = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingDirtReceiverComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<ClothingDirtReceiverComponent, StandingStateChangedEvent>(OnStand);
        SubscribeLocalEvent<ClothingDirtReceiverComponent, MoveEvent>(OnMove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        foreach (var (uid, info) in _lying)
        {
            // стоит - не нужно нарастание (встал но ещё не убрали из словаря?)
            if (!TryComp<StandingStateComponent>(uid, out var st) || st.Standing)
                continue;

            var elapsed = (float)(now - info.At).TotalSeconds;
            if (elapsed < 1f)
                continue;

            _lying[uid] = (now, info.Color);
            DirtySlots(uid, BodySlots, info.Color, 10f);
        }
    }

    // урон -> пачкаем кровью верхнюю одежду
    private void OnDamage(EntityUid uid, ClothingDirtReceiverComponent _, DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        var bloodColor = GetBloodColor(uid);
        var amount = Math.Min((float)args.DamageDelta.GetTotal() * 2f, 33f);

        DirtySlots(uid, new[] { "outerClothing", "jumpsuit" }, bloodColor, amount);
    }

    // упал/встал
    private void OnStand(EntityUid uid, ClothingDirtReceiverComponent _, StandingStateChangedEvent args)
    {
        if (!args.Standing)
        {
            // только что упал - смотрим есть ли лужа
            var puddleColor = PuddleAt(uid);
            if (puddleColor == null)
                return;

            _lying[uid] = (_timing.CurTime, puddleColor.Value);
            DirtySlots(uid, BodySlots, puddleColor.Value, 15f);
        }
        else
        {
            _lying.Remove(uid);
        }
    }

    // шаг -> пачкаем обувь если лужа
    private void OnMove(EntityUid uid, ClothingDirtReceiverComponent _, ref MoveEvent args)
    {
        var color = PuddleAt(uid);
        if (color == null)
            return;

        if (TryComp<StandingStateComponent>(uid, out var st) && st.Standing)
        {
            DirtySlots(uid, WalkSlots, color.Value, 8f);
        }
        else
        {
            // ползёт - обновляем цвет текущей лужи
            if (_lying.TryGetValue(uid, out var info))
                _lying[uid] = (info.At, color.Value);
            else
                _lying[uid] = (_timing.CurTime, color.Value);
        }
    }

    // ищем PuddleComponent на том же тайле что и моб
    private Color? PuddleAt(EntityUid uid)
    {
        var coords = Transform(uid).Coordinates;

        foreach (var ent in _xform.GetEntitiesInTile(coords))
        {
            if (!TryComp<PuddleComponent>(ent, out var puddle))
                continue;

            if (!_solutions.TryGetSolution(ent, puddle.SolutionName, out _, out var solution))
                continue;

            return solution.GetColor(EntityManager.EntitySysManager
                .GetEntitySystem<Robust.Shared.Prototypes.IPrototypeManager>());
        }

        return null;
    }

    // цвет крови берём из BloodstreamComponent -> реагент -> SubstanceColor
    private Color GetBloodColor(EntityUid uid)
    {
        if (TryComp<BloodstreamComponent>(uid, out var bs) && bs.BloodSolution != null)
        {
            var color = bs.BloodSolution.GetColor(EntityManager.EntitySysManager
                .GetEntitySystem<Robust.Shared.Prototypes.IPrototypeManager>());
            return color;
        }

        return new Color(0.55f, 0f, 0f); // fallback - тёмно-красный
    }
}
