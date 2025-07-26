using System.Linq;
using System.Numerics;
using Content.Server.Actions;
using Content.Server.Administration;
using Content.Server.Administration.Systems;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared._Sunrise.Pets;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.Pets;

public sealed class PettingSystem : SharedPettingSystem
{
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly AdminSystem _admin = default!;
    [Dependency] private readonly GhostRoleSystem _ghostRole = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IConsoleHost _console = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private const int MaxPetNameLenght = 30;

    private static readonly EntProtoId PetInterruptAttackActionId = "PetInterruptAttackAction";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PettableOnInteractComponent, PetSetAILogicEvent>(Pet);
        SubscribeNetworkEvent<PetSetAILogicEvent>(OnClientChangedPetLogic);

        SubscribeNetworkEvent<PetSetGhostAvaliable>(OnPetGhostAvailable);
        SubscribeNetworkEvent<PetSetName>(OnPetChangeNameRequest);

        SubscribeLocalEvent<PetOnInteractComponent, PetInterruptAttackEvent>(OnAttackInterrupt);
        SubscribeLocalEvent<MobStateChangedEvent>(OnKill);
    }

    #region Events

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
        UpdatePetOrder(pet.AsNullable(), args.Order, args.Target);
    }

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
            if (!_player.TryGetSessionByEntity(master.Value, out var masterSession))
                return;

            // Открываем окно для настройки гостроли питомца.
            _ghostRole.OpenMakeGhostRoleEui(masterSession, pet);
        }
        else
        {
            // Получаем сессию питомца, чтобы прописать ему команду
            if (!_player.TryGetSessionByEntity(pet, out var petSession))
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
        if (!_player.TryGetSessionByEntity(master.Value, out var masterSession))
            return;

        // Открываем меню для переименовывания
        _quickDialog.OpenDialog(masterSession,
            Loc.GetString("pet-rename-label"),
            Loc.GetString("pet-name-label"),
            (string newName) => Rename(pet, master.Value, newName));
    }

    /// <summary>
    /// Метод, вызываемый при убистве человека.
    /// Нужен, чтобы заставить питомца следовать за хозяином после успешного убийства приказом атааки
    /// </summary>
    private void OnKill(MobStateChangedEvent ev)
    {
        if (!_mobState.IsIncapacitated(ev.Target))
            return;

        if (!TryComp<PettableOnInteractComponent>(ev.Origin, out var petComponent))
            return;

        if (!TryComp<PetOnInteractComponent>(petComponent.Master, out var masterComponent))
            return;

        // TODO: Довольный звук от питомца

        // Поддержка множества питомцев
        // Проходимся по всем питомцам хозяина и заставляем следовать за ним
        // TODO: Возможность получить только участвовавших в битве и менять приказ лишь им
        foreach (var pet in masterComponent.Pets)
        {
            UpdatePetOrder(pet, PetOrderType.Follow);
        }
    }

    /// <summary>
    /// Метод, вызываемый при отмене приказа атаки хозяином.
    /// Меняет приказ с атаки на следование за хозяином для каждого питомца.
    /// </summary>
    private void OnAttackInterrupt(Entity<PetOnInteractComponent> master, ref PetInterruptAttackEvent args)
    {
        foreach (var pet in master.Comp.Pets)
        {
            UpdatePetOrder(pet, PetOrderType.Follow);
        }
    }

    #endregion

    #region Logic

    /// <summary>
    /// Метод, работающий с логикой НПС питомца.
    /// Задает питомцу переданный приказ и заставляет выполнять его бесконечно, пока не придет новый.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="orderType"></param>
    private void UpdatePetNpc(EntityUid uid, PetOrderType orderType)
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

    public void UpdatePetOrder(Entity<PettableOnInteractComponent?> pet, PetOrderType order, EntityUid? target = null)
    {
        if (!Resolve(pet, ref pet.Comp))
            return;

        var master = pet.Comp.Master;

        // Питомец не может следовать за кем-то без хозяина
        if (!master.HasValue)
            return;

        // Задаем питомцу задачу следовать за хозяином
        switch (order)
        {
            case PetOrderType.Follow:

                RemoveInterruptAction(master.Value);

                _npc.SetBlackboard(pet,
                    NPCBlackboard.FollowTarget,
                    new EntityCoordinates(master.Value, Vector2.Zero));
                break;

            case PetOrderType.Stay:

                RemoveInterruptAction(master.Value);

                _npc.SetBlackboard(pet,
                    NPCBlackboard.FollowTarget,
                    new EntityCoordinates(pet, Vector2.Zero));
                break;

            case PetOrderType.Attack:
                if (!target.HasValue)
                    break;

                AddInterruptAction(master.Value);

                _npc.SetBlackboard(pet,
                    NPCBlackboard.CurrentOrderedTarget,
                    target);
                break;
        }

        UpdatePetNpc(pet, order);
    }

    /// <summary>
    /// Добавляет акшен прерывания атаки, если его еще нет.
    /// </summary>
    private void AddInterruptAction(Entity<PetOnInteractComponent?> master)
    {
        if (!Resolve(master, ref master.Comp))
            return;

        var isActionPresent = master.Comp.PetActions
            .Where(IsActionPresent)
            .Any();

        if (isActionPresent)
            return;

        var action = _actions.AddAction(master, PetInterruptAttackActionId);

        if (!action.HasValue)
            return;

        master.Comp.PetActions.Add(action.Value);
        Dirty(master);
    }

    /// <summary>
    /// Убирает акшен прерывания атаки, если он есть
    /// </summary>
    private void RemoveInterruptAction(Entity<PetOnInteractComponent?> master)
    {
        if (!Resolve(master, ref master.Comp))
            return;

        var action = master.Comp.PetActions
            .Where(IsActionPresent)
            .FirstOrNull();

        if (!action.HasValue)
            return;

        _actions.RemoveAction(master.Owner, action);
        master.Comp.PetActions.Remove(action.Value);

        Dirty(master);
    }

    /// <summary>
    /// Проверяет, является ли данный акшен акшеном прерывания атаки.
    /// </summary>
    /// <param name="action"><see cref="EntityUid"/>Акшен, проходящий проверку</param>
    /// <returns>Является ли акшеном прерывания атаки или нет.</returns>
    private bool IsActionPresent(EntityUid action)
    {
        var meta = MetaData(action);

        if (meta.EntityPrototype == null)
            return false;

        if (meta.EntityPrototype == PetInterruptAttackActionId)
            return true;

        return false;
    }

    /// <summary>
    /// Выделенная в отдельный метод логика переименовывания питомца.
    /// </summary>
    /// <param name="target">EntityUid питомца</param>
    /// <param name="performer">EntityUid сущности, которая совершает переименовывание</param>
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
