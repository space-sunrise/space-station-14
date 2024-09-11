using Content.Shared._Sunrise.Pets;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Client._Sunrise.Pets;

[UsedImplicitly]
public sealed class PetControlBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    private PetControlMenu? _menu;
    private ClientPetSystem? _clientPetSystem;

    public PetControlBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);

        _clientPetSystem = _entityManager.System<ClientPetSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<PetControlMenu>();
        _menu.SetEntity(Owner);
        _menu.SendPetSystemMessageAction += SendPetSystemMessage;

        // Open the menu, centered on the mouse
        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    public void SendPetSystemMessage(PetBaseEvent ev)
    {
        // May god forgive us
        // Чтобы реализовать поддержку ивентов вместо сообщений буи мне пришлось использовать систему-пустышку.

        _clientPetSystem?.RaiseFuckingEvent(Owner, ev);
    }
}
