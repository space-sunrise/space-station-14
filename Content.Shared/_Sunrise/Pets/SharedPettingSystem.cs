using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._Sunrise.Pets;

public sealed class SharedPettingSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PetOnInteractComponent, InteractHandEvent>(OnPetAttempt);
    }

    private void OnPetAttempt(EntityUid uid, PetOnInteractComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<PettableOnInteractComponent>(args.Target, out var pet))
            return;

        var petEntity = (args.Target, pet);

        if (!TrySetMaster(petEntity, args.User))
            return;

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
        RaiseLocalEvent(pet, new PetMasterChanged{NewMaster = master});

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

        // Ивент информирует, что питомцу требуется обновление логики НПС. А она только на сервере
        RaiseLocalEvent(pet, new PetSetAILogicEvent(pet.Comp.CurrentOrder));
    }

}
