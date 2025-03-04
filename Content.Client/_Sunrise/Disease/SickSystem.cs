// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Disease;
using Content.Shared.Ghost;
namespace Content.Client._Sunrise.Disease;
public sealed class SickSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindContainerComponent, GetStatusIconsEvent>(OnGetStatusIconsGlobal);
        SubscribeNetworkEvent<UpdateInfectionsEvent>(OnUpdateInfect);
    }

    private void OnUpdateInfect(UpdateInfectionsEvent args)
    {
        EnsureComp<SickComponent>(GetEntity(args.Uid)).Inited = true;
    }

    private void OnGetStatusIconsGlobal(EntityUid uid, MindContainerComponent component, ref GetStatusIconsEvent args)
    {
        if (_playerManager.LocalEntity == null)
            return;

        if (!HasComp<DiseaseRoleComponent>(_playerManager.LocalEntity.Value))
            return;

        if (_mobState.IsDead(uid) ||
            HasComp<ActiveNPCComponent>(uid) ||
            !component.ShowExamineInfo)
            return;

        var isInfected = TryComp<SickComponent>(uid, out var sickComp) && sickComp.Inited;
        var iconId = isInfected ? "SmartDiseaseIcon" : "NormalDiseaseIcon";
        
        args.StatusIcons.Add(_prototype.Index<SickIconPrototype>(iconId));
    }
}

