using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Interaction;
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

    // Стандартный приказ, выдающийся при приручении
    private const PetOrderType DefaultOrder = PetOrderType.Follow;
    private const PetOrderType AttackOrder = PetOrderType.Attack;

    // Айди акшенов, которые будут выдаваться хозяину при появлении питомца.
    private const string OpenUiActionID = "PetOpenAllUiAction";
    private const string AttackTargetActionID = "PetAttackTargetAction";

    public override void Initialize()
    {
        base.Initialize();

        // Приручение
        SubscribeLocalEvent<PetOnInteractComponent, BeforeInteractHandEvent>(OnPetAttempt);

        // Управление
        SubscribeLocalEvent<PetOnInteractComponent, PetOpenAllUiEvent>(OnPetUiActionToggled);
        SubscribeLocalEvent<PetOnInteractComponent, PetAttackTargetEvent>(OnPetAttackToggled);
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
        Dirty(pet);

        // Добавляем питомца в список прирученных питомцев
        master.Comp.Pets.Add(pet);
        Dirty(master);

        // Вызываем ивент, который сообщает о смене владельца
        RaiseLocalEvent(pet, new PetMasterChanged { NewMaster = master });

        return true;
    }

    /// <summary>
    /// Работает с логикой поведения питомца после приручения
    /// </summary>
    /// <param name="pet">Питомец</param>
    private void Pet(Entity<PettableOnInteractComponent> pet)
    {
        // Проигрываем звук приручения, если он есть
        if (pet.Comp.PettingSuccessfulSound != null)
            _audio.PlayEntity(pet.Comp.PettingSuccessfulSound, pet, pet);

        // Необходимая проверка, чтобы еще раз убедиться, что хозяин имеется. Даже с учетом того, что он не может не иметься
        // Так как метод вызывается после присваивания хозяина.
        var master = pet.Comp.Master;
        if (!master.HasValue)
            return;

        // Показываем игроку попап об успешном приручении
        _popup.PopupClient($"Вы успешно приручаете {MetaData(pet).EntityName}", pet.Owner, master.Value);

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

        _actions.AddAction(master, OpenUiActionID);
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

        _actions.AddAction(master, AttackTargetActionID);
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

}
