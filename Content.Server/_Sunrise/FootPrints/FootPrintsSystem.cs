using System.Linq;
using Content.Server.Atmos.Components;
using Content.Server.Decals;
using Content.Server.FootPrints.Components;
using Content.Shared.Inventory;
using Content.Shared.Standing;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.FootPrints;

public sealed class FootPrintsSystem : EntitySystem
{
    [Dependency] private readonly DecalSystem _decals = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    private const float AlphaReduce = 0.15f;

    private List<KeyValuePair<EntityUid, uint>> _storedDecals = new();
    private const int MaxStoredDecals = 750;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FootPrintsComponent, MoveEvent>(OnMove);
    }

    private void OnMove(EntityUid uid, FootPrintsComponent component, ref MoveEvent args)
    {
        if (component.PrintsColor.A <= 0f)
            return;

        if (!TryComp<TransformComponent>(uid, out var transform))
            return;

        var dragging = _standing.IsDown(uid);
        var distance = (transform.LocalPosition - component.StepPos).Length();
        var stepSize = dragging ? component.DragSize : component.StepSize;

        if (!(distance > stepSize))
            return;


        if (!PrintDecal((uid, component, transform), dragging))
            return;

        var newAlpha = Math.Max(component.PrintsColor.A - AlphaReduce, 0f);

        component.PrintsColor = component.PrintsColor.WithAlpha(newAlpha);
        component.StepPos = transform.LocalPosition;
        component.RightStep = !component.RightStep;
    }

    private bool PrintDecal(Entity<FootPrintsComponent, TransformComponent> prints, bool dragging)
    {
        if (prints.Comp2.GridUid is not { } gridUid)
            return false;

        if (_storedDecals.Count > MaxStoredDecals)
        {
            var excessDecals = _storedDecals.Count - MaxStoredDecals;
            RemoveExcessDecals(excessDecals);
        }

        var print = PickPrint(prints, dragging);
        var coords = CalculateEntityCoordinates(prints, dragging);
        var colors = prints.Comp1.PrintsColor;
        var rotation = dragging
            ? (prints.Comp2.LocalPosition - prints.Comp1.StepPos).ToAngle() + Angle.FromDegrees(-90f)
            : prints.Comp2.LocalRotation + Angle.FromDegrees(180f);


        if (!_decals.TryAddDecal(print, coords, out var decalId, colors, rotation, 0, true))
            return false;

        _storedDecals.Add(new KeyValuePair<EntityUid, uint>(gridUid, decalId));
        return true;
    }

    private EntityCoordinates CalculateEntityCoordinates(Entity<FootPrintsComponent, TransformComponent> entity,
        bool isDragging)
    {
        var printComp = entity.Comp1;
        var transform = entity.Comp2;

        if (isDragging)
            return new EntityCoordinates(entity, transform.LocalPosition + printComp.OffsetCenter);

        var offset = printComp.OffsetCenter + (printComp.RightStep
            ? new Angle(Angle.FromDegrees(180f) + transform.LocalRotation).RotateVec(printComp.OffsetPrint)
            : new Angle(transform.LocalRotation).RotateVec(printComp.OffsetPrint));

        return new EntityCoordinates(entity, transform.LocalPosition + offset);
    }


    private string PickPrint(Entity<FootPrintsComponent> prints, bool dragging)
    {
        if (dragging)
            return _random.Pick(prints.Comp.DraggingPrint);

        if (_inventorySystem.TryGetSlotEntity(prints, "shoes", out _))
            return prints.Comp.ShoesPrint;

        if (_inventorySystem.TryGetSlotEntity(prints, "outerClothing", out var suit) &&
            TryComp<PressureProtectionComponent>(suit, out _))
            return prints.Comp.SuitPrint;

        return prints.Comp.RightStep ? prints.Comp.RightBarePrint : prints.Comp.LeftBarePrint;
    }

    private void RemoveExcessDecals(int excessDecals)
    {
        for (var i = 0; i < excessDecals; i++)
        {
            var decalToRemove = _storedDecals[i];
            _decals.RemoveDecal(decalToRemove.Key, decalToRemove.Value);
        }

        _storedDecals = _storedDecals.Skip(excessDecals).ToList();
    }
}
