using Content.Shared._Sunrise.BloodCult.Items;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.BloodCult.UI.BloodSpellSelector;

public sealed class BloodSpellSelectorBUI : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    private BloodCultMenu? _menu;

    public BloodSpellSelectorBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<BloodCultMenu>();

        var protoMan = IoCManager.Resolve<IPrototypeManager>();
        var entityMan = IoCManager.Resolve<IEntityManager>();
        var sprite = entityMan.System<SpriteSystem>();

        if (protoMan.TryIndex("CultBloodOrb", out EntityPrototype? bloodOrb))
        {
            var texture = sprite.GetPrototypeIcon(bloodOrb);
            var button = _menu.AddButton($"{bloodOrb.Name} (50)", texture.Default);

            button.OnPressed += _ =>
            {
                SendMessage(new CultBloodSpellCreateOrbBuiMessage());
                Close();
            };
        }

        if (protoMan.TryIndex("BloodSpear", out EntityPrototype? bloodSpear))
        {
            var texture = sprite.GetPrototypeIcon(bloodSpear);
            var button = _menu.AddButton($"{bloodSpear.Name} (150)", texture.Default);

            button.OnPressed += _ =>
            {
                SendMessage(new CultBloodSpellCreateBloodSpearBuiMessage());
                Close();
            };
        }

        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _menu?.Close();
    }
}
