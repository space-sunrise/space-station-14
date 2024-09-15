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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PetOnInteractComponent, BeforeInteractHandEvent>(OnPetAttempt);
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

        // Объявляем перменную, чтобы несколько раз не передавать ее длинную расшифровку.
        var petEntity = (args.Target, pet);

        // Пытаемся задать питомцу хозязина и проверяем, получилось ли.
        if (!TrySetMaster(petEntity, args.User))
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
    public bool TrySetMaster(Entity<PettableOnInteractComponent> pet, EntityUid master, bool overrideMaster = false)
    {
        // По умолчанию сменить владельца после приручения нельзя
        if (pet.Comp.Master != null && !overrideMaster)
            return false;

        // Меняем владельца
        pet.Comp.Master = master;
        Dirty(pet);

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
        _popup.PopupClient("Вы успешно приручаете " + MetaData(pet).EntityName, pet.Owner, master.Value);

        // Ивент информирует, что питомцу требуется обновление логики НПС. А она только на сервере
        RaiseLocalEvent(pet, new PetSetAILogicEvent(pet.Comp.CurrentOrder));
    }

    #endregion

}
