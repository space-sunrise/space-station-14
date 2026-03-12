using Content.Client._Sunrise.UserInterface.Radial;
using Content.Client.Resources;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;

namespace Content.Client.Vampire;

[UsedImplicitly]
public sealed class VampireMutationBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private RadialContainer? _menu;
    private bool _selected;
    private bool _isOpen;
    private SpriteSystem? _spriteSystem;

    public VampireMutationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entityManager.System<SpriteSystem>();
    }

    protected override void Open()
    {
        base.Open();
        _isOpen = true;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not VampireMutationBoundUserInterfaceState st)
            return;

        if (_menu != null)
        {
            _menu.Close();
            _menu = null;
        }

        _menu = new RadialContainer();
        
        _menu.Closed += () =>
        {
            if (_selected)
                return;

            Close();
        };

        foreach (var mutation in st.MutationList)
        {
            string texturePath = mutation switch
            {
                VampireMutationsType.None => "/Textures/Interface/Actions/actions_vampire.rsi/deathsembrace.png",
                VampireMutationsType.Hemomancer => "/Textures/Interface/Actions/actions_vampire.rsi/hemomancer.png",
                VampireMutationsType.Umbrae => "/Textures/Interface/Actions/actions_vampire.rsi/umbrae.png",
                VampireMutationsType.Gargantua => "/Textures/Interface/Actions/actions_vampire.rsi/gargantua.png",
                VampireMutationsType.Dantalion => "/Textures/Interface/Actions/actions_vampire.rsi/dantalion.png",
                VampireMutationsType.Bestia => "/Textures/Interface/Actions/actions_vampire.rsi/bestia.png",
                _ => "/Textures/Interface/Actions/actions_vampire.rsi/deathsembrace.png"
            };

            var texture = _resourceCache.GetTexture(texturePath);
            
            var name = Loc.GetString($"vampire-mutation-{mutation.ToString().ToLower()}-name");
            var tooltip = Loc.GetString($"vampire-mutation-{mutation.ToString().ToLower()}-info");
            
            var button = _menu.AddButton(name, texture);
            button.Content = "";
            button.Tooltip = tooltip;
            button.TooltipDelay = 0.01f;

            if (mutation == st.SelectedMutation)
            {
                button.Controller.StyleClasses.Add("selected");
            }

            button.Controller.OnPressed += _ =>
            {
                _selected = true;
                SendMessage(new VampireMutationPrototypeSelectedMessage(mutation));
                _menu?.Close();
                Close();
            };
        }

        if (_isOpen && st.MutationList.Count > 0)
        {
            _menu.OpenAttached(Owner);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _isOpen = false;
            _menu?.Close();
            _menu = null;
        }
    }
}