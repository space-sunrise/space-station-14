using Content.Shared._Sunrise.BloodCult.Components;
using Content.Shared._Sunrise.BloodCult.Items;
using Content.Shared.Actions;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.BloodCult.UI.SpellSelector;

public sealed class SpellSelectorBUI : BoundUserInterface
{
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private BloodCultMenu? _menu;

    public SpellSelectorBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<BloodCultMenu>();
        _menu.SetEntity(Owner);
        _menu.OnClose += Close;

        var protoMan = IoCManager.Resolve<IPrototypeManager>();

        foreach (var action in BloodCultistComponent.CultistActions)
        {
            if (!protoMan.TryIndex(action, out var proto))
                continue;

            SpriteSpecifier? icon;
            if (action.StartsWith("InstantAction") && proto.TryGetComponent(out InstantActionComponent? instantComp))
                icon = instantComp.Icon;
            else
            {
                if (!proto.TryGetComponent(out EntityTargetActionComponent? targetComp))
                    continue;
                icon = targetComp.Icon;
            }

            if (icon == null)
                continue;

            var texture = icon.Frame0();
            var button = _menu.AddButton(proto.Name, texture);

            button.OnPressed += _ =>
            {
                SendMessage(new CultSpellProviderSelectedBuiMessage(action));
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
