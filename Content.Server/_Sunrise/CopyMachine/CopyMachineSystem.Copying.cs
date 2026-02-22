using System.Numerics;
using Content.Shared._Sunrise.CopyMachine;
using Content.Shared._Sunrise.Paperwork;
using Content.Shared.Labels.Components;
using Content.Shared.Paper;

namespace Content.Server._Sunrise.CopyMachine;

public sealed partial class CopyMachineSystem : EntitySystem
{
    private bool TryCopyFromSlotOrButtScan(Entity<CopyMachineComponent> ent)
    {
        var paperEntity = Spawn(ent.Comp.PaperProtoId, Transform(ent.Owner).Coordinates);
        if (!TryComp<PaperComponent>(paperEntity, out var paperComponent))
            return false;

        Entity<PaperComponent> paper = (paperEntity, paperComponent);

        if (TryCopyButtScan(ent, paper))
            return true;

        return TryCopyFromPaperInCopySlot(ent, paper);
    }

    private bool TryCopyButtScan(Entity<CopyMachineComponent> ent, Entity<PaperComponent> paper)
    {
        if (!TryGetBuckledHumanoidAppearance(ent.Owner, out var humanoidAppearance))
            return false;

        if (!_prototypeManager.TryIndex(humanoidAppearance.Species, out var speciesPrototype) || speciesPrototype.ButtScan == null)
            return false;

        _paper.SetImageContent(paper, speciesPrototype.ButtScan, new Vector2(15, 15));
        paper.Comp.EditingDisabled = true;
        return true;
    }

    private bool TryCopyFromPaperInCopySlot(Entity<CopyMachineComponent> ent, Entity<PaperComponent> paper)
    {
        if (!ent.Comp.CopySlot.HasItem)
            return false;

        var sourcePaperEntity = ent.Comp.CopySlot.Item;
        if (!TryComp<PaperComponent>(sourcePaperEntity, out var sourcePaperComponent))
            return false;

        _paper.SetContent(paper, sourcePaperComponent.Content);

        if (HasComp<PaperTemplateFormComponent>(sourcePaperEntity))
            EnsureComp<PaperTemplateFormComponent>(paper.Owner);

        if (sourcePaperComponent.ImageContent != null)
            _paper.SetImageContent(paper, sourcePaperComponent.ImageContent, sourcePaperComponent.ImageScale);

        paper.Comp.EditingDisabled = sourcePaperComponent.EditingDisabled;

        if (sourcePaperComponent.StampState != null && sourcePaperComponent.StampedBy != null)
        {
            foreach (var stamp in sourcePaperComponent.StampedBy)
            {
                _paper.TryStamp(paper, stamp, sourcePaperComponent.StampState);
            }
        }

        if (TryComp<LabelComponent>(sourcePaperEntity, out var sourceLabelComponent) && !string.IsNullOrWhiteSpace(sourceLabelComponent.CurrentLabel))
            _label.Label(paper.Owner, sourceLabelComponent.CurrentLabel);

        return true;
    }
}
