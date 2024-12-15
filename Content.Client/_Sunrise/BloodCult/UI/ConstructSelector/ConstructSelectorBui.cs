using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.UI;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._Sunrise.BloodCult.UI.ConstructSelector;

public sealed class ConstructSelectorBui : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    private SpriteSystem _spriteSystem = default!;

    private bool _selected;
    private BloodCultMenu? _menu;

    public ConstructSelectorBui(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<BloodCultMenu>();

        _spriteSystem = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();
        var shellComponent = _entityManager.GetComponent<ConstructShellComponent>(Owner);

        _menu.OnClose += () =>
        {
            if(_selected)
                return;

            SendMessage(new ConstructFormSelectedEvent(_random.Pick(shellComponent.ConstructForms)));
        };

        foreach (var form in shellComponent.ConstructForms)
        {
            var formPrototype = _prototypeManager.Index<EntityPrototype>(form);
            var button = _menu.AddButton(formPrototype.Name, _spriteSystem.GetPrototypeIcon(formPrototype).Default);

            button.OnPressed += _ =>
            {
                _selected = true;
                SendMessage(new ConstructFormSelectedEvent(form));
                _menu.Close();
            };
        }

        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }
}
