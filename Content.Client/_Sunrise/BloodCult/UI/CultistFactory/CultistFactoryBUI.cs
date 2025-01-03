using Content.Client.UserInterface.Controls;
using Content.Shared._Sunrise.BloodCult;
using Content.Shared._Sunrise.BloodCult.UI;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.BloodCult.UI.CultistFactory;

public sealed class CultistFactoryBUI : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    private BloodCultMenu? _menu;

    private bool _updated = false;

    public CultistFactoryBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }
    private void ResetUI()
    {
        _menu?.Close();
        _menu = null;
        _updated = false;
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<BloodCultMenu>();

        if (State != null)
            UpdateState(State);
    }

    private void PopulateRadial(IReadOnlyCollection<string> ids)
    {
        var spriteSys = _entityManager.EntitySysManager.GetEntitySystem<SpriteSystem>();

        foreach (var id in ids)
        {
            if (!_prototypeManager.TryIndex<CultistFactoryProductionPrototype>(id, out var prototype))
                return;

            if (_menu == null)
                continue;

            if (prototype.Icon == null)
                continue;

            var button = _menu.AddButton(prototype.Name, spriteSys.Frame0(prototype.Icon));
            button.OnPressed += _ =>
            {
                Select(id);
            };
        }
    }

    private void Select(string id)
    {
        SendMessage(new CultistFactoryItemSelectedMessage(id));
        ResetUI();
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ResetUI();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_updated)
            return;

        if (state is CultistFactoryBUIState newState)
        {
            PopulateRadial(newState.Ids);
        }

        if (_menu == null)
            return;

        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
        _updated = true;
    }
}
