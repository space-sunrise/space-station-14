﻿using System.Linq;
using Content.Server.Body.Systems;
using Content.Shared._Sunrise.Medical.Surgery;
using Content.Shared._Sunrise.Medical.Surgery.Effects.Step;
using Content.Shared._Sunrise.Medical.Surgery.Events;
using Content.Shared._Sunrise.Medical.Surgery.Steps.Parts;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Speech.Muting;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared._Sunrise;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction.Components;
using Microsoft.CodeAnalysis;

namespace Content.Server._Sunrise.Medical.Surgery;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
//
//This file is already overloaded with responsibilities,
//it’s time to break its functionality into different systems.
//However, I don’t want to touch the official systems, so I need to come up with extensions for them.
public sealed partial class SurgerySystem : SharedSurgerySystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    private readonly EntProtoId _virtual = "PartVirtual";
    public void InitializeSteps()
    {
        SubscribeLocalEvent<SurgeryStepBleedEffectComponent, SurgeryStepEvent>(OnStepBleedComplete);
        SubscribeLocalEvent<SurgeryClampBleedEffectComponent, SurgeryStepEvent>(OnStepClampBleedComplete);
        SubscribeLocalEvent<SurgeryStepEmoteEffectComponent, SurgeryStepEvent>(OnStepEmoteEffectComplete);
        SubscribeLocalEvent<SurgeryStepSpawnEffectComponent, SurgeryStepEvent>(OnStepSpawnComplete);

        SubscribeLocalEvent<SurgeryStepOrganExtractComponent, SurgeryStepEvent>(OnStepOrganExtractComplete);
        SubscribeLocalEvent<SurgeryStepOrganInsertComponent, SurgeryStepEvent>(OnStepOrganInsertComplete);

        SubscribeLocalEvent<SurgeryStepAttachLimbEffectComponent, SurgeryStepEvent>(OnStepAttachComplete);
        SubscribeLocalEvent<SurgeryStepAmputationEffectComponent, SurgeryStepEvent>(OnStepAmputationComplete);

        SubscribeLocalEvent<CustomLimbMarkerComponent, ComponentRemove>(CustomLimbRemoved);

        SubscribeLocalEvent<SurgeryRemoveAccentComponent, SurgeryStepEvent>(OnRemoveAccent);

    }

    private void OnStepAttachComplete(Entity<SurgeryStepAttachLimbEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (GetSingleton(args.SurgeryProto) is not { } surgery
            || !TryComp<SurgeryLimbSlotConditionComponent>(surgery, out var slotComp))
            return;

        OnStepAttachLimbComplete(ent, slotComp.Slot, ref args);
        if (slotComp.Slot != "head")
            OnStepAttachItemComplete(ent, slotComp.Slot, ref args);
    }

    private void OnStepBleedComplete(Entity<SurgeryStepBleedEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (ent.Comp.Damage is not null && TryComp<DamageableComponent>(args.Body, out var comp))
            _damageableSystem.TryChangeDamage(args.Body, ent.Comp.Damage);
        //todo add wound
    }

    private void OnStepClampBleedComplete(Entity<SurgeryClampBleedEffectComponent> ent, ref SurgeryStepEvent args)
    {
        //todo remove wound
    }
    private void OnStepOrganInsertComplete(Entity<SurgeryStepOrganInsertComponent> ent, ref SurgeryStepEvent args)
    {
        if (args.Tools.Count == 0
            || !(args.Tools.FirstOrDefault() is var organId)
            || !TryComp<BodyPartComponent>(args.Part, out var bodyPart))
            return;

        var containerId = SharedBodySystem.GetOrganContainerId(ent.Comp.Slot);

        if (ent.Comp.Slot == "cavity" && _containers.TryGetContainer(args.Part, containerId, out var container))
        {
            _containers.Insert(organId, container);
            return;
        }

        if (!TryComp<OrganComponent>(organId, out var organComp))
            return;

        var part = args.Part;
        var body = args.Body;
        _delayAccumulator = 0;
        _delayQueue.Enqueue(() =>
        {
            if (!_body.InsertOrgan(part, organId, ent.Comp.Slot, bodyPart, organComp)) return;

            var ev = new SurgeryOrganImplantationCompleted(body, part, organId);
            RaiseLocalEvent(organId, ref ev);
        });
    }
    private void OnStepOrganExtractComplete(Entity<SurgeryStepOrganExtractComponent> ent, ref SurgeryStepEvent args)
    {
        if (ent.Comp.Organ?.Count != 1) return;

        var type = ent.Comp.Organ.Values.First().Component.GetType();

        if (ent.Comp.Slot != null && _containers.TryGetContainer(args.Part, SharedBodySystem.GetOrganContainerId(ent.Comp.Slot), out var container))
        {
            foreach (var containedEnt in container.ContainedEntities)
                if (HasComp(containedEnt, type))
                    _containers.Remove(containedEnt, container);

            return;
        }

        var organs = _body.GetPartOrgans(args.Part, Comp<BodyPartComponent>(args.Part));
        foreach (var organ in organs)
        {
            if (!HasComp(organ.Id, type) || !_body.RemoveOrgan(organ.Id, organ.Component)) continue;

            var ev = new SurgeryOrganExtracted(args.Body, args.Part, organ.Id);
            RaiseLocalEvent(organ.Id, ref ev);

            return;
        }
    }

    private void OnRemoveAccent(Entity<SurgeryRemoveAccentComponent> ent, ref SurgeryStepEvent args)
    {
        foreach (var accent in _accents)
            if (HasComp(args.Body, accent))
                RemCompDeferred(args.Body, accent);
    }

    private void OnStepEmoteEffectComplete(Entity<SurgeryStepEmoteEffectComponent> ent, ref SurgeryStepEvent args)
        => _chat.TryEmoteWithChat(args.Body, ent.Comp.Emote);
    private void OnStepSpawnComplete(Entity<SurgeryStepSpawnEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (TryComp(args.Body, out TransformComponent? xform))
            SpawnAtPosition(ent.Comp.Entity, xform.Coordinates);
    }
    private void OnStepAttachLimbComplete(Entity<SurgeryStepAttachLimbEffectComponent> _, string slot, ref SurgeryStepEvent args)
    {
        if (args.Tools.Count == 0
            || !(args.Tools.FirstOrDefault() is var limbId)
            || !TryComp<BodyPartComponent>(args.Part, out var bodyPart)
            || !TryComp<BodyPartComponent>(limbId, out var limb))
            return;

        var part = args.Part;
        var body = args.Body;

        if (!_body.AttachPart(part, slot, limbId, bodyPart, limb))
        {
            args.IsCancelled = true;
            return;
        }

        if (TryComp<HumanoidAppearanceComponent>(body, out var humanoid)) //todo move to system
        {
            var limbs = _body.GetBodyPartAdjacentParts(limbId, limb).Except([part]).Concat([limbId]);
            foreach (var partLimbId in limbs)
            {
                if (TryComp<BaseLayerIdComponent>(partLimbId, out var baseLayerStorage)
                    && TryComp(partLimbId, out BodyPartComponent? partLimb))
                {
                    var layer = partLimb.ToHumanoidLayers();
                    if (layer is null) continue;
                    _humanoidAppearanceSystem.SetBaseLayerId(body, layer.Value, baseLayerStorage.Layer, true, humanoid);
                }
            }
        }

        switch (limb.PartType)
        {
            case BodyPartType.Arm: //todo move to systems
                if (limb.Children.Keys.Count == 0)
                    _body.TryCreatePartSlot(limbId, limb.Symmetry == BodyPartSymmetry.Left ? "left hand" : "right hand", BodyPartType.Hand, out var slotId);

                foreach (var slotId in limb.Children.Keys)
                {
                    if (slotId is null) continue;
                    var slotFullId = BodySystem.GetPartSlotContainerId(slotId);
                    var child = _containers.GetContainer(limbId, slotFullId);

                    foreach (var containedEnt in child.ContainedEntities)
                    {
                        if (TryComp(containedEnt, out BodyPartComponent? innerPart)
                            && innerPart.PartType == BodyPartType.Hand)
                            _hands.AddHand(body, slotFullId, limb.Symmetry == BodyPartSymmetry.Left ? HandLocation.Left : HandLocation.Right);
                    }
                }
                break;
            case BodyPartType.Hand:
                _hands.AddHand(body, BodySystem.GetPartSlotContainerId(slot), limb.Symmetry == BodyPartSymmetry.Left ? HandLocation.Left : HandLocation.Right);
                break;
            case BodyPartType.Leg:
                if (limb.Children.Keys.Count == 0)
                    _body.TryCreatePartSlot(limbId, limb.Symmetry == BodyPartSymmetry.Left ? "left foot" : "right foot", BodyPartType.Foot, out var slotId);
                break;
            case BodyPartType.Foot:
                break;
        }
    }

    private void OnStepAttachItemComplete(Entity<SurgeryStepAttachLimbEffectComponent> ent, string slot, ref SurgeryStepEvent args)
    {
        if (args.Tools.Count == 0
            || !(args.Tools.FirstOrDefault() is var itemId)
            || !TryComp<BodyPartComponent>(args.Part, out var bodyPart)
            || !TryComp(itemId, out MetaDataComponent? metada)
            || TryComp<BodyPartComponent>(itemId, out var _)
            || Prototype(itemId) is not EntityPrototype prototype)
            return;

        var marker = EnsureComp<CustomLimbMarkerComponent>(itemId);

        var virtualIteam = Spawn(_virtual);
        var virtualBodyPart = EnsureComp<BodyPartComponent>(virtualIteam);
        var virtualMetadata = EnsureComp<MetaDataComponent>(virtualIteam);
        var virtualCustomLimb = EnsureComp<CustomLimbComponent>(virtualIteam);
        _metadata.SetEntityName(virtualIteam, metada.EntityName, virtualMetadata);

        marker.VirtualPart = virtualIteam;
        virtualCustomLimb.Item = itemId;

        virtualBodyPart.PartType = slot switch
        {
            "left arm" => BodyPartType.Arm,
            "right arm" => BodyPartType.Arm,
            "left hand" => BodyPartType.Hand,
            "right hand" => BodyPartType.Hand,
            "left leg" => BodyPartType.Leg,
            "right leg" => BodyPartType.Leg,
            "left foot" => BodyPartType.Foot,
            "right foot" => BodyPartType.Foot,
            "tail" => BodyPartType.Tail,
            _ => BodyPartType.Other,
        };
        if (!_body.AttachPart(args.Part, slot, virtualIteam, bodyPart, virtualBodyPart))
        {
            args.IsCancelled = true;
            QueueDel(virtualIteam);
            return;
        }

        if (TryComp<HumanoidAppearanceComponent>(args.Body, out var humanoid)) //todo move to system
        {
            var layer = GetLayer(slot);
            if (layer is null)
                return;

            var vizualizer = EnsureComp<CustomLimbVisualizerComponent>(args.Body);
            vizualizer.Layers[layer.Value] = GetNetEntity(itemId);
            Dirty(args.Body, vizualizer);

        }
        AddItemHand(args.Body, itemId, BodySystem.GetPartSlotContainerId(slot));
    }

    private void AddItemHand(EntityUid bodyId, EntityUid itemId, string handId)
    {
        if (!TryComp<HandsComponent>(bodyId, out var hands))
            return;

        if (!itemId.IsValid())
        {
            Log.Debug("no valid item");
            return;
        }

        _hands.AddHand(bodyId, handId, HandLocation.Middle, hands);
        _hands.DoPickup(bodyId, hands.Hands[handId], itemId, hands);
        EnsureComp<UnremoveableComponent>(itemId);
    }

    private void OnStepAmputationComplete(Entity<SurgeryStepAmputationEffectComponent> ent, ref SurgeryStepEvent args)
    {
        if (TryComp(args.Body, out TransformComponent? xform)
            && TryComp(args.Body, out BodyComponent? body)
            && TryComp(args.Part, out BodyPartComponent? limb))
        {

            if (!_containers.TryGetContainingContainer((args.Part, null, null), out var container)) return;

            var parentPartAndSlot = _body.GetParentPartAndSlotOrNull(args.Part);
            if (parentPartAndSlot is null) return;
            var (_, slotId) = parentPartAndSlot.Value;

            if (_containers.Remove(args.Part, container, destination: xform.Coordinates))
            {
                if (TryComp<CustomLimbComponent>(args.Part, out var virtualLimb)
                    && virtualLimb.Item.HasValue)
                {
                    RemoveItemHand(args.Body, virtualLimb.Item.Value, BodySystem.GetPartSlotContainerId(slotId));

                    var vizualizer = EnsureComp<CustomLimbVisualizerComponent>(args.Body);

                    var layer = GetLayer(slotId);
                    if (layer is not null)
                    {
                        vizualizer.Layers.Remove(layer.Value);
                        Dirty(args.Body, vizualizer);
                    }
                    QueueDel(args.Part);
                }
                else
                {
                    if (TryComp<HumanoidAppearanceComponent>(args.Body, out var humanoid)) //todo move to system
                    {
                        var limbs = _body.GetBodyPartAdjacentParts(args.Part, limb).Concat([args.Part]); ;
                        foreach (var partLimbId in limbs)
                        {
                            if (TryComp<BaseLayerIdComponent>(partLimbId, out var baseLayerStorage)
                                && TryComp(partLimbId, out BodyPartComponent? partLimb))
                            {
                                var layer = partLimb.ToHumanoidLayers();
                                if (layer is null) continue;
                                if (humanoid.CustomBaseLayers.TryGetValue(layer.Value, out var customBaseLayer))
                                    baseLayerStorage.Layer = customBaseLayer.Id;
                                else
                                {
                                    var bodyType = _prototypes.Index(humanoid.BodyType);
                                    if (bodyType.Sprites.TryGetValue(layer.Value, out var baseLayer))
                                        baseLayerStorage.Layer = baseLayer;
                                }
                            }
                        }
                    }
                    switch (limb.PartType)
                    {
                        case BodyPartType.Arm:  //todo move to systems
                            foreach (var limbSlotId in limb.Children.Keys)
                            {
                                if (limbSlotId is null) continue;
                                var child = _containers.GetContainer(args.Part, BodySystem.GetPartSlotContainerId(limbSlotId));

                                foreach (var containedEnt in child.ContainedEntities)
                                {
                                    if (TryComp(containedEnt, out BodyPartComponent? innerPart)
                                        && innerPart.PartType == BodyPartType.Hand)
                                        _hands.RemoveHand(args.Body, BodySystem.GetPartSlotContainerId(limbSlotId));
                                }
                            }
                            break;
                        case BodyPartType.Hand:
                            var parentSlot = _body.GetParentPartAndSlotOrNull(args.Part);
                            if (parentSlot is not null)
                                _hands.RemoveHand(args.Body, BodySystem.GetPartSlotContainerId(parentSlot.Value.Slot));
                            break;
                        case BodyPartType.Leg:
                        case BodyPartType.Foot:
                            break;
                    }
                }
            }
        }
    }
    private void RemoveItemHand(EntityUid bodyId, EntityUid itemId, string handId)
    {
        if (!TryComp<HandsComponent>(bodyId, out var hands)
            || !_hands.TryGetHand(bodyId, handId, out var hand, hands))
            return;

        if (!itemId.IsValid())
        {
            Log.Debug("no valid item");
            return;
        }
        RemComp<UnremoveableComponent>(itemId);
        _hands.DoDrop(itemId, hand);
        _hands.RemoveHand(bodyId, handId, hands);
    }

    private void CustomLimbRemoved(Entity<CustomLimbMarkerComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.VirtualPart is null) return;
        QueueDel(ent.Comp.VirtualPart.Value);
    }

    public static HumanoidVisualLayers? GetLayer(string slotId) => slotId switch
    {
        "left arm" => HumanoidVisualLayers.LArm,
        "right arm" => HumanoidVisualLayers.RArm,
        "left hand" => HumanoidVisualLayers.LHand,
        "right hand" => HumanoidVisualLayers.RHand,
        "left leg" => HumanoidVisualLayers.LLeg,
        "right leg" => HumanoidVisualLayers.RLeg,
        "left foot" => HumanoidVisualLayers.LFoot,
        "right foot" => HumanoidVisualLayers.RFoot,
        "tail" => HumanoidVisualLayers.Tail,
        _ => null,
    };

}
