using System.Numerics;
using Content.Server.Administration;
using Content.Server.Administration.Systems;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._Sunrise.Pets;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Console;
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
    [Dependency] private readonly GhostRoleSystem _ghostRoleSystem = default!;
    [Dependency] private readonly IConsoleHost _console = default!;

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

    /// <summary>
    /// Метод, работающий с логикой НПС питомца.
    /// Задает питомцу переданный приказ и заставляет выполнять его бесконечно, пока не придет новый.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="orderType"></param>
    private void UpdatePetNPC(EntityUid uid, PetOrderType orderType)
    {
        if (!TryComp<HTNComponent>(uid, out var htn))
            return;

        if (htn.Plan != null)
            _htn.ShutdownPlan(htn);

        // Задаем переданный приказ
        _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, orderType);

        // Заставляем бесконечно выполнять теукщий приказ
        _htn.Replan(htn);
    }

    /// <summary>
    /// Метод, вызываемый, когда игрок изменяет текущий приказ своему питомцу через меню управления
    /// </summary>
    /// <param name="args">Ивент типа PetSetAILogicEvent</param>
    private void OnClientChangedPetLogic(PetSetAILogicEvent args)
    {
        var entity = GetEntity(args.Entity);

        if (!TryComp<PettableOnInteractComponent>(entity, out var component))
            return;

        Pet((entity, component), ref args);
    }

    /// <summary>
    /// Серверная часть приручения питомца.
    /// При приручении стандартным приказом является следование за хозяином.
    /// </summary>
    /// <param name="pet">Entity питомца</param>
    /// <param name="args">Ивент типа PetSetAILogicEvent, передающий текущий приказ питомцу</param>
    private void Pet(Entity<PettableOnInteractComponent> pet, ref PetSetAILogicEvent args)
    {
        var master = pet.Comp.Master;

        // Питомец не может следовать за кем-то без хозяина
        if (!master.HasValue)
            return;

        // Задаем питомцу задачу следовать за хозяином
        switch (args.Order)
        {
            case PetOrderType.Follow:
                _npc.SetBlackboard(pet,
                    NPCBlackboard.FollowTarget,
                    new EntityCoordinates(master.Value, Vector2.Zero));
                break;

            case PetOrderType.Stay:
                _npc.SetBlackboard(pet,
                    NPCBlackboard.FollowTarget,
                    new EntityCoordinates(pet, Vector2.Zero));
                break;

            case PetOrderType.Attack:
                if (!args.Target.HasValue)
                    break;

                _npc.SetBlackboard(pet,
                    NPCBlackboard.CurrentOrderedTarget,
                    args.Target);
                break;
        }

        UpdatePetNPC(pet, args.Order);
    }

    #endregion

    #region Petting events

    /// <summary>
    /// Метод, вызываемый при переключении разумности питомца в его меню управления.
    /// Разумность позволяет призракам вселиться в питомца и управлять им.
    /// Отключение выкидывает игрока из тела, заново включая ИИ
    /// </summary>
    /// <param name="args">Ивент типа PetSetGhostAvaliable</param>
    private void OnPetGhostAvailable(PetSetGhostAvaliable args)
    {
        var pet = GetEntity(args.Entity);

        // В зависимости того, включаем или отключаем разумность делаем всякое.
        if (args.Enable)
        {
            if (!TryComp<PettableOnInteractComponent>(pet, out var petComponent))
                return;

            var master = petComponent.Master;

            if (!master.HasValue)
                return;

            // Получаем сессию хозяина питомца, чтобы открыть ему окно управления
            if (!_playerManager.TryGetSessionByEntity(master.Value, out var masterSession))
                return;

            // Открываем окно для настройки гостроли питомца.
            _ghostRoleSystem.OpenMakeGhostRoleEui(masterSession, pet);
        }
        else
        {
            // Получаем сессию питомца, чтобы прописать ему команду
            if (!_playerManager.TryGetSessionByEntity(pet, out var petSession))
                return;

            // Убираем компонент гостроли
            RemComp<GhostRoleComponent>(pet);

            // Выкидываем игроки из тела
            _console.ExecuteCommand(petSession, "ghost");
        }
    }

    /// <summary>
    /// Метод, вызываемый при запросе смены имени питомца через меню управления питомца.
    /// </summary>
    /// <param name="args">Ивент типа PetSetName</param>
    private void OnPetChangeNameRequest(PetSetName args)
    {
        // Получает EntityUid из передаваемого NetEntity
        var pet = GetEntity(args.Entity);

        // Получаем компонент питомца и проверяем, есть ли он
        if (!TryComp<PettableOnInteractComponent>(pet, out var petComponent))
            return;

        var master = petComponent.Master;

        if (!master.HasValue)
            return;

        // Получаем сессию хозяина питомца
        if (!_playerManager.TryGetSessionByEntity(master.Value, out var masterSession))
            return;

        // Открываем меню для переименовывания
        _quickDialog.OpenDialog(masterSession,
            Loc.GetString("pet-rename-label"),
            Loc.GetString("pet-name-label"),
            (string newName) => Rename(pet, master.Value, newName));
    }

    /// <summary>
    /// Выделенная в отдельный метод логика переименовывания питомца.
    /// </summary>
    /// <param name="target">EntityUid питомца</param>
    /// <param name="name">Новое выбранное имя питомца</param>
    private void Rename(EntityUid target, EntityUid performer, string name)
    {
        // Ограничение имени по символам, чтобы в имени не оказалось огромной пасты.
        if (name.Length > MaxPetNameLenght)
        {
            _popup.PopupEntity(Loc.GetString("pet-name-too-long"), target, performer);
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
