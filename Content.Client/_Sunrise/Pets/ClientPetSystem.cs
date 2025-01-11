using Content.Shared._Sunrise.Pets;

namespace Content.Client._Sunrise.Pets;

public sealed class ClientPetSystem : EntitySystem
{
    public void RaiseFuckingEvent(EntityUid pet, PetBaseEvent args)
    {
        args.Entity = GetNetEntity(pet);
        RaiseNetworkEvent(args);

    }
}
