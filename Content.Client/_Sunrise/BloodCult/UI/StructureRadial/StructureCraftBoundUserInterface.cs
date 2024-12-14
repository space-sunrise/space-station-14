using Content.Client.Construction;
using Content.Client.Resources;
using Content.Client.UserInterface.Controls;
using Content.Shared._Sunrise.BloodCult.Structures;
using Content.Shared.Construction.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.BloodCult.UI.StructureRadial;

public sealed class StructureCraftBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IClyde _displayManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;

    private BloodCultMenu? _menu;

    public StructureCraftBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    private void CreateUI()
    {
        if (_menu != null)
            ResetUI();

        _menu = this.CreateWindow<BloodCultMenu>();

        foreach (var prototype in _prototypeManager.EnumeratePrototypes<CultStructurePrototype>())
        {
            var texture = IoCManager.Resolve<IResourceCache>().GetTexture(prototype.Icon);
            var radialButton = _menu.AddButton(prototype.StructureName, texture);
            radialButton.OnPressed += _ =>
            {
                Select(prototype.StructureId);
            };
        }

        var vpSize = _displayManager.ScreenSize;
        _menu.OpenCenteredAt(_inputManager.MouseScreenPosition.Position / vpSize);
    }

    private void ResetUI()
    {
        _menu?.Close();
        _menu = null;
    }

    protected override void Open()
    {
        base.Open();

        CreateUI();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        ResetUI();
    }

    private void Select(string id)
    {
        CreateBlueprint(id);
        ResetUI();
        Close();
    }

    private void CreateBlueprint(string id)
    {
        var newObj = new PlacementInformation
        {
            Range = 2,
            IsTile = false,
            EntityType = id,
            PlacementOption = "SnapgridCenter"
        };

        _prototypeManager.TryIndex<ConstructionPrototype>(id, out var construct);

        if (construct == null)
            return;

        var player = _player.LocalSession?.AttachedEntity;

        if (player == null)
            return;

        // Хуйня которая не работает
        // if (construct.ID == "CultPylon" && CheckForStructure(player, id))
        // {
        //     var popup = _entMan.System<SharedPopupSystem>();
        //     popup.PopupClient(Loc.GetString("cult-structure-craft-another-structure-nearby"), player.Value, player.Value);
        //     return;
        // }

        var constructSystem = _systemManager.GetEntitySystem<ConstructionSystem>();
        var hijack = new ConstructionPlacementHijack(constructSystem, construct);

        _placement.BeginPlacing(newObj, hijack);
    }

    private bool CheckForStructure(EntityUid? uid, string id)
    {
        if (uid == null)
            return false;

        if (!_entMan.TryGetComponent<TransformComponent>(uid, out var transform))
            return false;

        var lookupSystem = _entMan.System<EntityLookupSystem>();
        var entities = lookupSystem.GetEntitiesInRange(transform.Coordinates, 15f);
        foreach (var ent in entities)
        {
            if (!_entMan.TryGetComponent<MetaDataComponent>(ent, out var metadata))
                continue;

            if (metadata.EntityPrototype?.ID == id)
                return true;
        }

        return false;
    }
}
