using System.Linq;
using Content.Server.Materials;
using Content.Shared._Sunrise.PrinterDoc;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Humanoid;
using Content.Shared.Paper;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Materials;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using System.Text.RegularExpressions;

namespace Content.Server._Sunrise.PrinterDoc;

public sealed class PrinterDocSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;

    private readonly Dictionary<string, string> _docCache = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PrinterDocComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<PrinterDocComponent, PrinterDocPrintMessage>(OnPrintMessage);
        SubscribeLocalEvent<PrinterDocComponent, PrinterDocCopyMessage>(OnCopyMessage);
        SubscribeLocalEvent<PrinterDocComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PrinterDocComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<PrinterDocComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<PrinterDocComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
        SubscribeLocalEvent<PrinterDocComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);
        SubscribeLocalEvent<PrinterDocComponent, StrappedEvent>(OnStasisStrapped);
        SubscribeLocalEvent<PrinterDocComponent, UnstrappedEvent>(OnStasisUnstrapped);
        CacheAllDocuments();
    }

    private void CacheAllDocuments()
    {
        foreach (var template in _proto.EnumeratePrototypes<DocTemplatePrototype>())
        {
            if (!_resourceManager.ContentFileExists(template.Content))
                continue;

            using var file = _resourceManager.ContentFileReadText(template.Content);
            var fileText = file.ReadToEnd();
            var match = Regex.Match(fileText, @"<Document>(.*?)</Document>", RegexOptions.Singleline);
            var content = match.Success ? match.Groups[1].Value.Trim() : fileText.Trim();
            _docCache[template.ID] = content;
        }
    }

    private void OnStasisStrapped(EntityUid uid, PrinterDocComponent comp, ref StrappedEvent args)
    {
        UpdateUserInterface(uid, comp);
    }

    private void OnStasisUnstrapped(EntityUid uid, PrinterDocComponent comp, ref UnstrappedEvent args)
    {
        UpdateUserInterface(uid, comp);
    }

    private void OnMaterialAmountChanged(EntityUid uid, PrinterDocComponent comp, ref MaterialAmountChangedEvent args)
    {
        UpdateUserInterface(uid, comp);
    }

    private void OnItemInserted(EntityUid uid, PrinterDocComponent comp, EntInsertedIntoContainerMessage args)
    {
        UpdateUserInterface(uid, comp);
    }

    private void OnItemRemoved(EntityUid uid, PrinterDocComponent comp, EntRemovedFromContainerMessage args)
    {
        UpdateUserInterface(uid, comp);
    }

    private void OnUiOpened(EntityUid uid, PrinterDocComponent comp, BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, comp);
    }

    private void OnPrintMessage(EntityUid uid, PrinterDocComponent comp, PrinterDocPrintMessage msg)
    {
        TryPrint(uid, comp, msg.TemplateId);
    }

    private void OnCopyMessage(EntityUid uid, PrinterDocComponent comp, PrinterDocCopyMessage msg)
    {
        TryCopy(uid, comp);
    }

    private void OnMapInit(EntityUid uid, PrinterDocComponent comp, MapInitEvent args)
    {
        _itemSlotsSystem.AddItemSlot(uid, PrinterDocComponent.CopySlotId, comp.CopySlot);
        if (comp.Templates.Count == 0)
        {
            foreach (var template in _proto.EnumeratePrototypes<DocTemplatePrototype>())
            {
                comp.Templates.Add(template.ID);
            }
        }
        UpdateUserInterface(uid, comp);
    }

    private void OnSolutionChanged(EntityUid uid, PrinterDocComponent comp, SolutionContainerChangedEvent args)
    {
        UpdateUserInterface(uid, comp);
    }

    public bool TryPrint(EntityUid uid, PrinterDocComponent comp, string templateId)
    {
        if (!_solution.TryGetSolution(uid, comp.Solution, out _, out var solution))
            return false;

        if (!solution.TryGetReagentQuantity(new ReagentId(comp.IncReagentProto, null), out var incVolume))
            return false;

        if (incVolume < comp.IncCost)
            return false;

        if (!_materialStorage.TryChangeMaterialAmount(uid, comp.PaperMaterial, -comp.PaperCost))
            return false;

        solution.RemoveReagent(new ReagentId(comp.IncReagentProto, null), comp.IncCost);

        UpdateUserInterface(uid, comp);

        var coordinates = Transform(uid).Coordinates;
        var paper = Spawn(comp.PaperProtoId, coordinates);
        if (!TryComp<PaperComponent>(paper, out var paperComp))
            return false;

        if (!_docCache.TryGetValue(templateId, out var content))
            return false;

        _paperSystem.SetContent((paper, paperComp), content);
        return true;
    }

    public bool TryCopy(EntityUid uid, PrinterDocComponent comp)
    {
        if (!_solution.TryGetSolution(uid, comp.Solution, out _, out var solution))
            return false;

        if (!solution.TryGetReagentQuantity(new ReagentId(comp.IncReagentProto, null), out var incVolume))
            return false;

        if (incVolume < comp.IncCost)
            return false;

        if (!_materialStorage.TryChangeMaterialAmount(uid, comp.PaperMaterial, -comp.PaperCost))
            return false;

        solution.RemoveReagent(new ReagentId(comp.IncReagentProto, null), comp.IncCost);

        UpdateUserInterface(uid, comp);

        var coordinates = Transform(uid).Coordinates;
        var paper = Spawn(comp.PaperProtoId, coordinates);
        if (!TryComp<PaperComponent>(paper, out var paperComp))
            return false;

        if (TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count != 0)
        {
            var buckled = strap.BuckledEntities.First();

            if (TryComp<HumanoidAppearanceComponent>(buckled, out var humanoidAppearance))
            {
                var buttTexture = _proto.TryIndex(humanoidAppearance.Species, out var species)
                    ? species.ButtScanTexture
                    : null;

                var content = $"[tex path=\"{buttTexture}\" scale=15]";

                _paperSystem.SetContent((paper, paperComp), content);
                paperComp.EditingDisabled = true;
                return true;
            }
        }

        if (comp.CopySlot.HasItem)
        {
            if (TryComp<PaperComponent>(comp.CopySlot.Item, out var copyPaperComponent))
            {
                _paperSystem.SetContent((paper, paperComp), copyPaperComponent.Content);
                paperComp.EditingDisabled = copyPaperComponent.EditingDisabled;
                return true;
            }
        }

        return false;
    }

    public void UpdateUserInterface(EntityUid uid, PrinterDocComponent comp)
    {
        if (!_solution.TryGetSolution(uid, comp.Solution, out _, out var solution))
            return;

        float incVolume = 0;
        if (solution.TryGetReagentQuantity(new ReagentId(comp.IncReagentProto, null), out var inc))
            incVolume = inc.Value;

        var availablePaper = _materialStorage.GetMaterialAmount(uid, comp.PaperMaterial);

        var state = new PrinterDocBoundUserInterfaceState(
            availablePaper / 100,
            incVolume / 100,
            comp.Templates.Select(t => t.Id).ToList(),
            CanCopy(uid, comp)
        );
        _userInterfaceSystem.SetUiState(uid, PrinterDocUiKey.Key, state);
    }

    public bool CanCopy(EntityUid uid, PrinterDocComponent comp)
    {
        var hasCopyPaper = comp.CopySlot.HasItem;
        var hasBuckleUser = false;
        if (TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count != 0)
        {
            var buckled = strap.BuckledEntities.First();
            if (TryComp<HumanoidAppearanceComponent>(buckled, out var humanoidAppearance))
            {
                hasBuckleUser = true;
            }
        }

        return hasBuckleUser || hasCopyPaper;
    }
}
