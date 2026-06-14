using System.Diagnostics.CodeAnalysis;
using Content.Server.Silicons.Laws;
using Content.Shared._Sunrise.Silicons.StationAi;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Containers;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.Silicons.StationAi;

public sealed partial class StationAiBodySystem
{
    /*
     * Features partial.
     *
     * This file owns extra gameplay features attached to station AI bodies:
     * access reader setup, radio channel inheritance from the AI core, and silicon law lookup
     * while the AI is operating through a body.
     */

    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;

    #region Initialize

    private void InitializeBodyFeatures()
    {
        SubscribeLocalEvent<StationAiBodyComponent, GetSiliconLawsEvent>(OnBodyGetLaws);
    }

    #endregion

    #region Events

    private void OnBodyGetLaws(Entity<StationAiBodyComponent> body, ref GetSiliconLawsEvent args)
    {
        if (args.Handled || body.Comp.LinkedAi is not { } stationAi)
            return;

        args.Laws = _siliconLaw.GetLaws(stationAi);
        args.Handled = true;
    }

    #endregion

    #region Access

    private void SetFreeBodyAccess(EntityUid chassis)
    {
        if (!TryComp<AccessReaderComponent>(chassis, out var accessReader))
            return;

        _accessReader.TrySetAccesses((chassis, accessReader), new List<HashSet<ProtoId<AccessLevelPrototype>>>
        {
            new() { "Captain" },
            new() { "ResearchDirector" },
            new() { "CentralCommand" },
        });
        _accessReader.SetActive((chassis, accessReader), false);
    }

    private void SetControlledBodyAccess(EntityUid chassis)
    {
        if (!TryComp<AccessReaderComponent>(chassis, out var accessReader))
            return;

        _accessReader.SetActive((chassis, accessReader), true);
    }

    #endregion

    #region Radio

    private void SetStationAiRadio(EntityUid stationAi, Entity<StationAiBodyComponent> body)
    {
        if (!TryGetRadioChannelsHolderByAiCore(stationAi, out var radioChannelsHolder))
            return;

        if (TryComp<IntrinsicRadioTransmitterComponent>(body, out var transmitterReceiver)
            && TryComp<IntrinsicRadioTransmitterComponent>(radioChannelsHolder, out var transmitterTransmitter))
        {
            body.Comp.CachedChannels[nameof(IntrinsicRadioTransmitterComponent)] = [..transmitterReceiver.Channels];

            transmitterReceiver.Channels.UnionWith(transmitterTransmitter.Channels);
            Dirty(body, transmitterReceiver);
        }

        if (TryComp<ActiveRadioComponent>(body, out var activeRadioReceiver)
            && TryComp<ActiveRadioComponent>(radioChannelsHolder, out var activeRadioTransmitter))
        {
            body.Comp.CachedChannels[nameof(ActiveRadioComponent)] = [..activeRadioReceiver.Channels];

            activeRadioReceiver.Channels.UnionWith(activeRadioTransmitter.Channels);
            Dirty(body, activeRadioReceiver);
        }

        Dirty(body);
    }

    private bool TryGetRadioChannelsHolderByAiCore(EntityUid stationAi, [NotNullWhen(true)] out EntityUid? radioChannelsHolder)
    {
        radioChannelsHolder = null;
        if (!TryComp<ContainerCompComponent>(stationAi, out var containerComp))
            return false;

        if (!_container.TryGetContainer(stationAi, containerComp.Container, out var container))
            return false;

        foreach (var containedEntity in container.ContainedEntities)
        {
            var proto = Prototype(containedEntity);
            if (proto == null || proto != containerComp.Proto)
                continue;

            radioChannelsHolder = containedEntity;
            return true;
        }

        return false;
    }

    #endregion

    #region Helpers

    private void DisableBodyAccess(EntityUid chassis)
    {
        if (TryComp<AccessReaderComponent>(chassis, out var accessReader))
            _accessReader.SetActive((chassis, accessReader), false);
    }

    #endregion
}
