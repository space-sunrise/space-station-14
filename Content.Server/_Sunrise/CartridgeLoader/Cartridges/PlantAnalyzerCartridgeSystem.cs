using Content.Server.Botany.Components;
using Content.Shared.Botany.PlantAnalyzer; // Исправленное пространство имён
using Content.Shared.CartridgeLoader;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Content.Shared.UserInterface;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class PlantAnalyzerCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = null!;
    [Dependency] private readonly TagSystem _tagSystem = null!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = null!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantAnalyzerCartridgeComponent, CartridgeAddedEvent>(OnCartridgeAdded);
        SubscribeLocalEvent<PlantAnalyzerCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);
    }

    private void OnCartridgeAdded(Entity<PlantAnalyzerCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        var loader = args.Loader;

        // Добавляем основные компоненты анализатора
        EnsureComp<PlantAnalyzerComponent>(loader);
        _tagSystem.AddTag(loader, "PlantAnalyzer");

        // Добавляем компонент активации интерфейса
        var activatable = EnsureComp<ActivatableUIComponent>(loader);
        activatable.Key = PlantAnalyzerUiKey.Key;

        // Регистрируем интерфейс через систему UI
        _uiSystem.OpenUi(loader, PlantAnalyzerUiKey.Key);
    }

    private void OnCartridgeRemoved(Entity<PlantAnalyzerCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        var loader = args.Loader;

        if (!_cartridgeLoaderSystem.HasProgram<PlantAnalyzerCartridgeComponent>(loader))
        {
            RemComp<PlantAnalyzerComponent>(loader);
            _tagSystem.RemoveTag(loader, "PlantAnalyzer");
            RemComp<ActivatableUIComponent>(loader);

            // Закрываем интерфейс через систему UI
            _uiSystem.CloseUi(loader, PlantAnalyzerUiKey.Key);
        }
    }
}
