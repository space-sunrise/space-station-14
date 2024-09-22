using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Cloning;
using Content.Shared.Gibbing.Events;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Pets;

public sealed class SharedPettingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    // Стандартный приказ, выдающийся при приручении
    private const PetOrderType DefaultOrder = PetOrderType.Follow;
    private const PetOrderType AttackOrder = PetOrderType.Attack;

    // Айди акшенов, которые будут выдаваться хозяину при появлении питомца.
    private const string OpenUiActionID = "PetOpenAllUiAction";
    private const string AttackTargetActionID = "PetAttackTargetAction";

    // Эффекты
    private const string PettingSuccessEffectID = "EffectHearts";

    public override void Initialize()
    {
        base.Initialize();

        // Приручение
        SubscribeLocalEvent<PetOnInteractComponent, BeforeInteractHandEvent>(OnPetAttempt);

        // Управление
        SubscribeLocalEvent<PetOnInteractComponent, PetOpenAllUiEvent>(OnPetUiActionToggled);
        SubscribeLocalEvent<PetOnInteractComponent, PetAttackTargetEvent>(OnPetAttackToggled);

        // Совместимость
        SubscribeLocalEvent<PetOnInteractComponent, CloningEvent>(OnMasterCloned);
        SubscribeLocalEvent<PetOnInteractComponent, EntityGibbedEvent>(OnMasterGibbed);

    }

    #region Base petting

    private void OnPetAttempt(EntityUid uid, PetOnInteractComponent component, BeforeInteractHandEvent args)
    {
        // Эта проверка позволяет методу срабатывать на клиенте только в первый раз, когда он предугадывает его.
        // Нужно, чтобы клиент 999 раз не выполнял действия ниже
        if (!_timing.IsFirstTimePredicted)
            return;

        // Проверяем возможность приручения у таргета.
        if (!TryComp<PettableOnInteractComponent>(args.Target, out var pet))
            return;

        // Проверяем, спит ли таргет
        if (HasComp<SleepingComponent>(args.Target))
            return;

        // Проверяем, жив ли таргет.
        if (TryComp<MobStateComponent>(args.Target, out var state) && !_mobStateSystem.IsAlive(args.Target, state))
            return;

        // Объявляем перменные, чтобы несколько раз не передавать ее длинную расшифровку.
        var petEntity = (args.Target, pet);
        var masterEntity = (args.User, component);

        // Пытаемся задать питомцу хозязина и проверяем, получилось ли.
        if (!TrySetMaster(petEntity, masterEntity))
            return;

        // Приручаем питомца, если все прошло успешно и код дошел до этого момента.
        Pet(petEntity);
    }

    /// <summary>
    /// Задает владельца питомца и вызывает ивент, сообщающий об этом
    /// </summary>
    /// <param name="pet">Питомец</param>
    /// <param name="master">Владелец</param>
    /// <param name="overrideMaster">Следует ли перезаписать текущего владельца, если он есть?</param>
    public bool TrySetMaster(Entity<PettableOnInteractComponent> pet, Entity<PetOnInteractComponent> master, bool overrideMaster = false)
    {
        // По умолчанию сменить владельца после приручения нельзя
        if (pet.Comp.Master != null && !overrideMaster)
            return false;

        // Меняем владельца
        pet.Comp.Master = master;
        pet.Comp.NetMaster = GetNetEntity(master);
        Dirty(pet);

        // Добавляем питомца в список прирученных питомцев
        master.Comp.Pets.Add(pet);
        Dirty(master);

        // Вызываем ивент, который сообщает о смене владельца
        RaiseLocalEvent(pet, new PetMasterChanged { NewMaster = master });

        return true;
    }

    /// <summary>
    /// Метод, работающий с логикой отвязывания хозяина от питомца
    /// </summary>
    /// <param name="pet">Питомец</param>
    /// <param name="master">Хозяин</param>
    private void RemoveMaster(Entity<PettableOnInteractComponent> pet, Entity<PetOnInteractComponent> master)
    {
        // Заставляем питомца забыть своего хозяина
        pet.Comp.Master = null;
        pet.Comp.NetMaster = null;
        Dirty(pet);

        // Убираем питомца из списка прирученных питомцев у хозяина
        master.Comp.Pets.Remove(pet.Owner);
        Dirty(master);

        // Если после отвязки у хозяина не осталось питомцев...
        if (master.Comp.Pets.Count != 0)
            return;

        // ..Мы убираем у него все акшены, связанные с питомцами.
        foreach (var actionUid in master.Comp.PetActions)
        {
            _actions.RemoveAction(actionUid);
        }

        // TODO: Возможность убрать акшен атаки отдельно, когда не осталось питомцев с возможностью атаковать.
    }

    /// <summary>
    /// Работает с логикой поведения питомца после приручения
    /// </summary>
    /// <param name="pet">Питомец</param>
    /// <param name="silent">Скрывать визуальные эффекты, звуки и попапы?</param>
    private void Pet(Entity<PettableOnInteractComponent> pet, bool silent = false)
    {
        // Проигрываем звук приручения, если он есть
        if (pet.Comp.PettingSuccessfulSound != null && !silent)
            _audio.PlayEntity(pet.Comp.PettingSuccessfulSound, pet, pet);

        // Необходимая проверка, чтобы еще раз убедиться, что хозяин имеется. Даже с учетом того, что он не может не иметься
        // Так как метод вызывается после присваивания хозяина.
        var master = pet.Comp.Master;
        if (!master.HasValue)
            return;

        // Показываем игроку попап об успешном приручении
        if (!silent)
            _popup.PopupClient($"Вы успешно приручаете {MetaData(pet).EntityName}", pet.Owner, master.Value);

        // Спавним эффект с сердечками
        if (!silent)
            Spawn(PettingSuccessEffectID, _transform.GetMapCoordinates(pet));

        // Если питомцу не доступен стандартный приказ, то он не меняет своей логики поведения
        if (!pet.Comp.AllowedOrders.Contains(DefaultOrder))
            return;

        // Ивент информирует, что питомцу требуется обновление логики НПС. А она только на сервере
        RaiseLocalEvent(pet, new PetSetAILogicEvent(DefaultOrder));

        // Дальше добавляем акшены для управления питомцем.
        // Делаем это через код, чтобы они появлялись только после приручения питомца

        if (!TryComp<PetOnInteractComponent>(master, out var masterComponent))
            return;

        var masterEntity = (master.Value, masterComponent);

        TryAddOpenUiAction(masterEntity);
        TryAddAttackAction(masterEntity);
    }

    /// <summary>
    /// Добавляет акшен открывающий интерфейсы питомцев.
    /// Работает только 1 раз, после первого приручения
    /// </summary>
    /// <param name="master">Entity(PetOnInteractComponent) его хозяина</param>
    private void TryAddOpenUiAction(Entity<PetOnInteractComponent> master)
    {
        // Если питомцев больше одного - это не первое приручение и акшен уже имеется, разворачиваемся
        if (master.Comp.Pets.Count != 1)
            return;

        // Добавляем акшен хозяину и добавляем его в список акшенов
        var action = _actions.AddAction(master, OpenUiActionID);
        master.Comp.PetActions.Add(action);
    }

    /// <summary>
    /// Добавляет акшен выбирающий питомцам цель атаки
    /// Работает только 1 раз, после добавления питомца с возможностью атаковать.
    /// </summary>
    /// <param name="master">Entity(PetOnInteractComponent) его хозяина</param>
    private void TryAddAttackAction(Entity<PetOnInteractComponent> master)
    {
        // Список всех питомцев, имеющих возможность атаковать
        var agressivePetList =
            master.Comp.Pets.Where(x => TryComp<PettableOnInteractComponent>(x, out var petComponent)
                                   && petComponent.AllowedOrders.Contains(PetOrderType.Attack));

        // Если таких питомцев больше одного - это не первое приручение и акшен уже имеется, разворачиваемся
        if (agressivePetList.Count() != 1)
            return;

        // Добавляем акшен хозяину и добавляем его в список акшенов
        var action = _actions.AddAction(master, AttackTargetActionID);
        master.Comp.PetActions.Add(action);
    }

    #endregion

    #region Petting events

    /// <summary>
    /// Метод, вызываем при активации акшена хозяина питомцев.
    /// Акшен должен переключать меню управления всех питомцев хозяина для удобства управления несколькими питомцами.
    /// </summary>
    /// <param name="master">EntityUid хозяина</param>
    /// <param name="component">Компонент, позволяющий приручать питомцев типа PetOnInteractComponent</param>
    /// <param name="args">Ивент, вызываемый акшеном, типа PetOpenAllUiEvent</param>
    private void OnPetUiActionToggled(EntityUid master, PetOnInteractComponent component, PetOpenAllUiEvent args)
    {
        // Выполняем действия для каждого питомца из списка прирученных питомцев у хозяина.
        foreach (var pet in component.Pets)
        {
            // Проверяем, открыто ли меню управления у конкретного питомца
            var isOpen = _ui.IsUiOpen(pet, PetControlUiKey.Key);

            // Если меню открыто мы его закрываем, если закрыто - открываем
            if (isOpen)
                _ui.CloseUi(pet, PetControlUiKey.Key, master);
            else
                _ui.OpenUi(pet, PetControlUiKey.Key, master);
        }
    }

    /// <summary>
    /// Метод, работающий с каждым питомцем, задавая ему приказ атаковать, если это возможно.
    /// </summary>
    /// <param name="master">EntityUid хозяина</param>
    /// <param name="component">Компонент, позволяющий приручать питомцев, типа PetOnInteractComponent</param>
    /// <param name="args">Ивент, передающий приказ атаковать и таргет для атаки, типа PetAttackTargetEvent</param>
    private void OnPetAttackToggled(EntityUid master, PetOnInteractComponent component, PetAttackTargetEvent args)
    {
        // Проходимся по всем прирученным питомцам у владельца
        foreach (var pet in component.Pets)
        {
            // Получаем компонент питомца, чтобы достать оттуда список доступных приказов
            if (!TryComp<PettableOnInteractComponent>(pet, out var petComponent))
                continue;

            // Если питомец не имеет приказа атаковать, то он пропускается
            if (!petComponent.AllowedOrders.Contains(AttackOrder))
                continue;

            // Вызываем ивент, информирующий о смене логики поведения питомца на приказ атаки, передавая цель.
            RaiseLocalEvent(pet, new PetSetAILogicEvent(AttackOrder, args.Target));
        }
    }

    #endregion

    #region Pets compability

    /// <summary>
    /// Метод, работающий с логикой передачи всех питомцев от одного хозяина к другому.
    /// Если нового хозяина нет, метод очищает весь список питомцев у старого владельца и отвязывает их.
    /// </summary>
    /// <param name="uid">EntityUid старого хозяина</param>
    /// <param name="component">Компонент хозяина</param>
    /// <param name="newMaster">EntityUid нового хозяина, если есть. В него перейдут все питомцы и добавятся все нужные для этого акшены</param>
    private void CleanMaster(EntityUid uid, PetOnInteractComponent component, EntityUid? newMaster = null)
    {
        // Захешировал список, так как хз, как поведет себя цикл при удалении оттуда питомцев при итерации
        var oldPets = component.Pets;

        // Проходимся по списку всех питомцев у старого владельца
        foreach (var petUid in oldPets)
        {
            if (!TryComp<PettableOnInteractComponent>(petUid, out var petComponent))
                continue;

            var petEntity = (petUid, petComponent);

            // Убираем питомца из списка питомцев старого тела
            RemoveMaster(petEntity, (uid, component));

            // Если хозяин null, значит мы не передаем питомцев в новое тело.
            // Потоум что тела или нет, или это нам не требуется
            if (!newMaster.HasValue)
                continue;

            if (!TryComp<PetOnInteractComponent>(newMaster, out var newMasterComponent))
                continue;

            // Новое тела хозяина
            var newMasterEntity = (newMaster.Value, newMasterComponent);

            // Добавляем питомца в нового мастера
            TrySetMaster(petEntity, newMasterEntity, true);

            // Задаем питомцу правильную логику и добавляем владельцу акшены
            Pet(petEntity, true);
        }

        // TODO: В идеале список питомцев должен храниться в Mind`е хозяина, чтобы автоматически переноситься в новое тело, как только туда зайдет игрок
    }

    /// <summary>
    /// Метод, обрабатывающий логику передачи питомцев от старого тела хозяина к новому при клонировании
    /// </summary>
    /// <param name="uid">EntityUid прошлого тела хозяина</param>
    /// <param name="component">Компонент типа PetOnInteractComponent</param>
    /// <param name="args">Ивент типа CloningEvent</param>
    private void OnMasterCloned(EntityUid uid, PetOnInteractComponent component, ref CloningEvent args)
    {
        CleanMaster(uid, component, args.Target);
    }

    /// <summary>
    /// Метод, обрабатывающий логику отвязывания питомцев от хозяина, когда его тело гибнули.
    /// </summary>
    /// <param name="uid">EntityUid хозяина</param>
    /// <param name="component">Компонент хозяина</param>
    /// <param name="args">Ивент типа EntityGibbedEvent, вызываемый после гиба тела.</param>
    private void OnMasterGibbed(EntityUid uid, PetOnInteractComponent component, ref EntityGibbedEvent args)
    {
        CleanMaster(uid, component);
    }

    #endregion
}
