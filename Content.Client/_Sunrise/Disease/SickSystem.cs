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

        SubscribeLocalEvent<SickComponent, GetStatusIconsEvent>(OnGetStatusIcon);
        SubscribeNetworkEvent<UpdateInfectionsEvent>(OnUpdateInfect);
    }

    private void OnUpdateInfect(UpdateInfectionsEvent args)
    {
        EnsureComp<SickComponent>(GetEntity(args.Uid)).Inited = true;
    }

    private void OnGetStatusIcon(EntityUid uid, SickComponent component, ref GetStatusIconsEvent args)
    {
        if (component.Inited)
        {
            if (_playerManager.LocalEntity != null)
            {
                if (HasComp<DiseaseRoleComponent>(_playerManager.LocalEntity.Value) || HasComp<GhostComponent>(_playerManager.LocalEntity.Value))
                {
                    if (!_mobState.IsDead(uid) &&
                        !HasComp<ActiveNPCComponent>(uid) &&
                        TryComp<MindContainerComponent>(uid, out var mindContainer) &&
                        mindContainer.ShowExamineInfo)
                    {
                        args.StatusIcons.Add(_prototype.Index<SickIconPrototype>(component.Icon));
                    }
                }
            }
        }
    }
}
