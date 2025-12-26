namespace Content.Shared.Kitchen;

/// <summary>
/// Raised on an entity when it is inside a microwave and it starts cooking.
/// </summary>
public sealed class BeingMicrowavedEvent(EntityUid microwave, EntityUid? user, bool heating, bool irradiating) : HandledEntityEventArgs
{
    public EntityUid Microwave = microwave;
    public EntityUid? User = user;

    public bool BeingHeated = heating; //Sunrise-Edit
    public bool BeingIrradiated = irradiating; //Sunrise-Edit
}
