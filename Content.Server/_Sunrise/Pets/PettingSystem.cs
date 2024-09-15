using System.Numerics;
using Content.Server.Administration;
using Content.Server.Administration.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._Sunrise.Pets;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server._Sunrise.Pets;

public sealed class PettingSystem : EntitySystem
{
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly AdminSystem _admin = default!;

    private const int MaxPetNameLenght = 30;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PettableOnInteractComponent, PetSetAILogicEvent>(Pet);
        SubscribeNetworkEvent<PetSetAILogicEvent>(OnClientChangedPetLogic);

        SubscribeNetworkEvent<PetSetGhostAvaliable>(OnPetGhostAvailable);
        SubscribeNetworkEvent<PetSetName>(OnPetChangeNameRequest);
    }

    #region Base petting

    private void UpdatePetNPC(EntityUid uid, PetOrderType orderType)
    {
        if (!TryComp<HTNComponent>(uid, out var htn))
            return;

        if (htn.Plan != null)
            _htn.ShutdownPlan(htn);

        _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, orderType);
        _htn.Replan(htn);
    }

    private void OnClientChangedPetLogic(PetSetAILogicEvent args)
    {
        var entity = GetEntity(args.Entity);

        if (!TryComp<PettableOnInteractComponent>(entity, out var component))
            return;

        Pet(entity, component, args);
    }

    private void Pet(EntityUid pet, PettableOnInteractComponent component, PetSetAILogicEvent args)
    {
        // Питомец не может следовать за кем-то без хозяина
        if (!component.Master.HasValue)
            return;

        _npc.SetBlackboard(pet,
            NPCBlackboard.FollowTarget,
            new EntityCoordinates(component.Master.Value, Vector2.Zero));
        UpdatePetNPC(pet, args.Order);
    }

    #endregion

    #region Petting events

    private void OnPetGhostAvailable(PetSetGhostAvaliable args)
    {
        var pet = GetEntity(args.Entity);

        if (args.Enable)
        {
            var ghost = EnsureComp<GhostRoleComponent>(pet);
        }
        else
        {
            RemComp<GhostRoleComponent>(pet);
        }
    }

    /// <summary>
    /// Метод, вызываемый при запросе смены имени питомца через меню управления питомца.
    /// </summary>
    /// <param name="args">Ивент типа PetSetName</param>
    private void OnPetChangeNameRequest(PetSetName args)
    {
        var pet = GetEntity(args.Entity);

        if (!TryComp<PettableOnInteractComponent>(pet, out var petComponent))
            return;

        var master = petComponent.Master;

        if (!master.HasValue)
            return;

        if (!_playerManager.TryGetSessionByEntity(master.Value, out var masterSession))
            return;

        _quickDialog.OpenDialog(masterSession, "Переименовать", "Имя", (string newName) => Rename(pet, newName));
    }

    /// <summary>
    /// Выделенная в отдельный метод логика переименовывания питомца.
    /// </summary>
    /// <param name="target">EntityUid питомца</param>
    /// <param name="name">Новое выбранное имя питомца</param>
    private void Rename(EntityUid target, string name)
    {
        // Ограничение имени по символам, чтобы в имени не оказалось огромной пасты.
        if (name.Length > MaxPetNameLenght)
        {
            _popup.PopupEntity("Выбранное имя слишком большое", target, target);
            return;
        }

        _metaData.SetEntityName(target, name);

        // Переименовывание имени персонажа в разуме питомца.
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return;

        mind.CharacterName = name;
        Dirty(mindId, mind);

        // Admin Overlay - работает только тогда, когда в питомце сидит игрок.
        if (TryComp<ActorComponent>(target, out var actorComp))
            _admin.UpdatePlayerList(actorComp.PlayerSession);
    }

    #endregion

}
