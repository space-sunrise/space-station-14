﻿using Content.Shared._Sunrise.Medical.Surgery.Steps;
using Content.Shared.Body.Part;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Medical.Surgery.Events;
using Content.Shared._Sunrise.Medical.Surgery.Effects.Step;
using System.Linq;

namespace Content.Shared._Sunrise.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
public abstract partial class SharedSurgerySystem
{

    protected float _delayAccumulator = 0f;
    protected readonly Queue<Action> _delayQueue = new();
    private void InitializeSteps()
    {
        SubscribeLocalEvent<SurgeryStepComponent, SurgeryStepEvent>(OnStep);
        SubscribeLocalEvent<SurgeryClearProgressComponent, SurgeryStepEvent>(OnClearProgressStep);
        SubscribeLocalEvent<SurgeryTargetComponent, SurgeryDoAfterEvent>(OnTargetDoAfter);

        SubscribeLocalEvent<SurgeryStepComponent, SurgeryCanPerformStepEvent>(OnCanPerformStep);

        Subs.BuiEvents<SurgeryTargetComponent>(SurgeryUIKey.Key, subs => subs.Event<SurgeryStepChosenBuiMsg>(OnSurgeryTargetStepChosen));
    }
    private void OnTargetDoAfter(Entity<SurgeryTargetComponent> ent, ref SurgeryDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Handled ||
            args.Target is not { } target ||
            !IsSurgeryValid(ent, target, args.Surgery, args.Step, out var surgery, out var part, out var step) ||
            !PreviousStepsComplete(ent, part, surgery, args.Step) ||
            !CanPerformStep(args.User, ent, part.Comp.PartType, step, false))
        {
            Log.Warning($"{ToPrettyString(args.User)} tried to start invalid surgery.");
            Dirty(ent);
            if (args.Target.HasValue && TryComp<BodyPartComponent>(args.Target.Value, out var dirtyPart))
                Dirty(args.Target.Value, dirtyPart, Comp<MetaDataComponent>(args.Target.Value));
            return;
        }

        var ev = new SurgeryStepEvent(args.User, ent, part, GetTools(args.User))
        {
            StepProto = args.Step,
            SurgeryProto = args.Surgery,
            IsFinal = surgery.Comp.Steps[^1] == args.Step,
        };
        RaiseLocalEvent(step, ref ev);

        if (_net.IsClient) return;
        _delayAccumulator = 0f;
        _delayQueue.Enqueue(() => RefreshUI(ent));
    }

    private void OnClearProgressStep(Entity<SurgeryClearProgressComponent> ent, ref SurgeryStepEvent args)
    {
        var progress = Comp<SurgeryProgressComponent>(args.Part);
        progress.CompletedSteps.Clear();
        progress.CompletedSurgeries.Clear();
    }

    private void OnStep(Entity<SurgeryStepComponent> ent, ref SurgeryStepEvent args)
    {
        if (!TryComp<SurgeryClearProgressComponent>(ent, out _))
        {
            if (TryComp<SurgeryProgressComponent>(args.Part, out var progress))
            {
                progress.CompletedSteps.Add($"{args.SurgeryProto}:{args.StepProto}");
                if(!progress.StartedSurgeries.Contains(args.SurgeryProto) && !args.IsFinal)
                    progress.StartedSurgeries.Add(args.SurgeryProto);
                if (progress.StartedSurgeries.Contains(args.SurgeryProto) && args.IsFinal)
                    progress.StartedSurgeries.Remove(args.SurgeryProto);
            }
            else
            {
                progress = new SurgeryProgressComponent { CompletedSteps = [$"{args.SurgeryProto}:{args.StepProto}"] };
                AddComp(args.Part, progress);
            }
            if (args.IsFinal)
                progress.CompletedSurgeries.Add(args.SurgeryProto);
        }

        foreach (var reg in (ent.Comp.Tools ?? []).Values)
        {
            var tool = args.Tools.FirstOrDefault(x => HasComp(x, reg.Component.GetType()));
            if (tool == default) return;

            if (_net.IsServer && TryComp(tool, out SurgeryToolComponent? toolComp) && toolComp.EndSound != null)
                _audio.PlayPvs(toolComp.EndSound, tool);
        }

        foreach (var reg in (ent.Comp.Add ?? []).Values)
        {
            var compType = reg.Component.GetType();
            if (HasComp(args.Part, compType))
                continue;
            var newComp = _compFactory.GetComponent(compType);
            _serialization.CopyTo(reg.Component, ref newComp, notNullableOverride: true);
            AddComp(args.Part, newComp);
        }

        foreach (var reg in (ent.Comp.BodyAdd ?? []).Values)
        {
            var compType = reg.Component.GetType();
            if (HasComp(args.Body, compType))
                continue;

            AddComp(args.Part, _compFactory.GetComponent(compType));
        }

        foreach (var reg in (ent.Comp.Remove ?? []).Values)
            RemComp(args.Part, reg.Component.GetType());

        foreach (var reg in (ent.Comp.BodyRemove ?? []).Values)
            RemComp(args.Body, reg.Component.GetType());
    }

    private void OnCanPerformStep(Entity<SurgeryStepComponent> ent, ref SurgeryCanPerformStepEvent args)
    {
        if (HasComp<SurgeryOperatingTableConditionComponent>(ent)
            && (!TryComp(args.Body, out BuckleComponent? buckle) || !HasComp<OperatingTableComponent>(buckle.BuckledTo)))
        {
            args.Invalid = StepInvalidReason.NeedsOperatingTable;
            return;
        }

        RaiseLocalEvent(args.Body, ref args);

        if (args.Invalid != StepInvalidReason.None || ent.Comp.Tools == null)
            return;

        foreach (var reg in ent.Comp.Tools.Values)
        {
            var tool = args.Tools.FirstOrDefault(x => HasComp(x, reg.Component.GetType()));
            if (tool == default)
            {
                args.Invalid = StepInvalidReason.MissingTool;

                if (reg.Component is ISurgeryToolComponent toolComp)
                    args.Popup = $"You need {toolComp.ToolName} to perform this step!";

                return;
            }

            args.ValidTools.Add(tool);
        }
    }

    private void OnSurgeryTargetStepChosen(Entity<SurgeryTargetComponent> ent, ref SurgeryStepChosenBuiMsg args)
    {
        var user = args.Actor;
        if (GetEntity(args.Entity) is not { Valid: true } body
            || GetEntity(args.Part) is not { Valid: true } targetPart
            || !IsSurgeryValid(body, targetPart, args.Surgery, args.Step, out var surgery, out var part, out var step)
            || GetSingleton(args.Step) is not { } stepEnt
            || !TryComp(stepEnt, out SurgeryStepComponent? stepComp)
            || !CanPerformStep(user, body, part.Comp.PartType, step, true, out _, out _, out var validTools))
        {
            return;
        }
        if(!PreviousStepsComplete(body, part, surgery, args.Step) || IsStepComplete(part, args.Surgery, args.Step))
        {
            var progress = Comp<SurgeryProgressComponent>(part);
            Dirty(part, progress);
            _delayAccumulator = 0f;
            _delayQueue.Enqueue(() => RefreshUI(body));
            return;
        }

        var duration = stepComp.Duration;

        foreach (var tool in validTools)
            if (TryComp(tool, out SurgeryToolComponent? toolComp))
            {
                duration *= toolComp.Speed;
                if (toolComp.StartSound != null) _audio.PlayPvs(toolComp.StartSound, tool);
            }

        if (TryComp(body, out TransformComponent? xform))
            _rotateToFace.TryFaceCoordinates(user, _transform.GetMapCoordinates(body, xform).Position);

        var ev = new SurgeryDoAfterEvent(args.Surgery, args.Step);
        var doAfter = new DoAfterArgs(EntityManager, user, duration, ev, body, part)
        {
            BreakOnMove = true,
            DuplicateCondition = DuplicateConditions.SameTarget,
            ForceNet = true
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    public (Entity<SurgeryComponent> Surgery, int Step)? GetNextStep(EntityUid body, EntityUid part, EntityUid surgery) => GetNextStep(body, part, surgery, []);
    private (Entity<SurgeryComponent> Surgery, int Step)? GetNextStep(EntityUid body, EntityUid part, Entity<SurgeryComponent?> surgery, List<EntityUid> requirements)
    {
        if (!Resolve(surgery, ref surgery.Comp))
            return null;

        if (requirements.Contains(surgery))
            throw new ArgumentException($"Surgery {surgery} has a requirement loop: {string.Join(", ", requirements)}");

        requirements.Add(surgery);

        if (surgery.Comp.Requirement is { } requirementId &&
            GetSingleton(requirementId) is { } requirement &&
            GetNextStep(body, part, requirement, requirements) is { } requiredNext)
            return requiredNext;

        if (!TryComp<SurgeryProgressComponent>(part, out var progress))
        {
            AddComp<SurgeryProgressComponent>(part);
            return ((surgery, surgery.Comp), 0);
        }
        var surgeryProto = Prototype(surgery);
        for (var i = 0; i < surgery.Comp.Steps.Count; i++)
            if (!progress.CompletedSteps.Contains($"{surgeryProto?.ID}:{surgery.Comp.Steps[i]}"))
                return ((surgery, surgery.Comp), i);

        return null;
    }

    public bool PreviousStepsComplete(EntityUid body, EntityUid part, Entity<SurgeryComponent> surgery, EntProtoId step)
    {
        if (surgery.Comp.Requirement is { } requirement)
        {
            if (GetSingleton(requirement) is not { } requiredEnt ||
                !TryComp(requiredEnt, out SurgeryComponent? requiredComp) ||
                !PreviousStepsComplete(body, part, (requiredEnt, requiredComp), step))
            {
                return false;
            }
        }

        foreach (var surgeryStep in surgery.Comp.Steps)
        {
            if (surgeryStep == step)
                break;

            if (Prototype(surgery.Owner) is not EntityPrototype surgProto || !IsStepComplete(part, surgProto.ID, surgeryStep))
                return false;
        }

        return true;
    }

    public bool CanPerformStep(EntityUid user, EntityUid body, BodyPartType part, EntityUid step, bool doPopup) => CanPerformStep(user, body, part, step, doPopup, out _, out _, out _);
    public bool CanPerformStep(EntityUid user, EntityUid body, BodyPartType part, EntityUid step, bool doPopup, out string? popup, out StepInvalidReason reason, out HashSet<EntityUid> validTools)
    {
        var slot = part switch
        {
            BodyPartType.Head => SlotFlags.HEAD,
            BodyPartType.Torso => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
            BodyPartType.Arm => SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING,
            BodyPartType.Hand => SlotFlags.GLOVES,
            BodyPartType.Leg => SlotFlags.OUTERCLOTHING | SlotFlags.LEGS,
            BodyPartType.Foot => SlotFlags.FEET,
            BodyPartType.Tail => SlotFlags.NONE,
            BodyPartType.Other => SlotFlags.NONE,
            _ => SlotFlags.NONE
        };

        var check = new SurgeryCanPerformStepEvent(user, body, GetTools(user), slot);
        RaiseLocalEvent(step, ref check);
        popup = check.Popup;
        validTools = check.ValidTools;

        if (check.Invalid != StepInvalidReason.None)
        {
            if (doPopup && check.Popup != null)
                _popup.PopupEntity(check.Popup, user, PopupType.SmallCaution);

            reason = check.Invalid;
            return false;
        }

        reason = default;
        return true;
    }

    public bool IsStepComplete(EntityUid part, EntProtoId surgery, EntProtoId step)
    {
        if (TryComp<SurgeryProgressComponent>(part, out var comp))
            return comp.CompletedSteps.Contains($"{surgery}:{step}");
        AddComp<SurgeryProgressComponent>(part);
        return false;
    }
}
