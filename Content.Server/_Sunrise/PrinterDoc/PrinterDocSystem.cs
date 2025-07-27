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
using Robust.Shared.Timing;
using Content.Server.GameTicking.Events;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;
using Content.Server.Station.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Emag.Components;
using Content.Server.Station.Events;
using Robust.Server.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Tag;

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
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;



    private TimeSpan _roundStartTime;
    private readonly Dictionary<string, string> _docCache = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
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
        SubscribeLocalEvent<PrinterDocComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<StationInitializedEvent>(OnStationInitialized);

        CacheAllDocuments();
    }

    private void OnRoundStart(RoundStartingEvent ev) => _roundStartTime = _timing.CurTime;

    private void CacheAllDocuments()
    {
        foreach (var template in _proto.EnumeratePrototypes<DocTemplatePrototype>())
        {
            if (!_resourceManager.ContentFileExists(template.Content))
                continue;

            using var file = _resourceManager.ContentFileReadText(template.Content);
            var fileText = file.ReadToEnd();
            var match = Regex.Match(fileText, "<Document>(.*?)</Document>", RegexOptions.Singleline);
            var content = match.Success ? match.Groups[1].Value.Trim() : fileText.Trim();
            _docCache[template.ID] = content;
        }
    }

    private void OnUiOpened(EntityUid uid, PrinterDocComponent comp, BoundUIOpenedEvent args) => UpdateUserInterface(uid, comp);
    private void OnSolutionChanged(EntityUid uid, PrinterDocComponent comp, SolutionContainerChangedEvent args) => UpdateUserInterface(uid, comp);
    private void OnItemRemoved(EntityUid uid, PrinterDocComponent comp, EntRemovedFromContainerMessage args) => UpdateUserInterface(uid, comp);
    private void OnStasisStrapped(EntityUid uid, PrinterDocComponent comp, ref StrappedEvent args) => UpdateUserInterface(uid, comp);
    private void OnStasisUnstrapped(EntityUid uid, PrinterDocComponent comp, ref UnstrappedEvent args) => UpdateUserInterface(uid, comp);
    private void OnMaterialAmountChanged(EntityUid uid, PrinterDocComponent comp, ref MaterialAmountChangedEvent args) => UpdateUserInterface(uid, comp);

    private void OnItemInserted(EntityUid uid, PrinterDocComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != PrinterDocComponent.CopySlotId)
            return;

        if (!TryComp<PaperComponent>(args.Entity, out var paperComp))
        {
            UpdateUserInterface(uid, comp);
            return;
        }

        if (string.IsNullOrWhiteSpace(paperComp.Content))
        {
            Timer.Spawn(TimeSpan.FromMilliseconds(1), () =>
            {
                if (Deleted(uid) || Deleted(args.Entity))
                    return;

                if (!comp.CopySlot.HasItem || comp.CopySlot.Item != args.Entity)
                    return;

                if (TryComp<TagComponent>(args.Entity, out var tag) && tag.Tags.Contains("Paper"))
                {
                    _materialStorage.TryChangeMaterialAmount(uid, comp.PaperMaterial, 100);
                    QueueDel(args.Entity);
                    UpdateUserInterface(uid, comp);
                }
            });

            return;
        }

        UpdateUserInterface(uid, comp);
    }

    private void OnPrintMessage(EntityUid uid, PrinterDocComponent comp, PrinterDocPrintMessage msg)
    {
        if (comp.JobQueue.Count >= comp.MaxQueueSize)
            return;

        if (!TryConsumeResources(uid, comp))
            return;

        comp.JobQueue.Enqueue((PrinterJobType.Print, msg.TemplateId));
        UpdateUserInterface(uid, comp);
    }

    private void OnCopyMessage(EntityUid uid, PrinterDocComponent comp, PrinterDocCopyMessage msg)
    {
        if (comp.JobQueue.Count >= comp.MaxQueueSize)
            return;

        if (!TryConsumeResources(uid, comp))
            return;

        comp.JobQueue.Enqueue((PrinterJobType.Copy, null));
        UpdateUserInterface(uid, comp);
    }

    private void OnMapInit(EntityUid uid, PrinterDocComponent comp, MapInitEvent args)
    {
        _itemSlotsSystem.AddItemSlot(uid, PrinterDocComponent.CopySlotId, comp.CopySlot);

        if (comp.Templates.Count == 0)
        {
            var isEmagged = HasComp<EmaggedComponent>(uid);
            foreach (var template in _proto.EnumeratePrototypes<DocTemplatePrototype>())
            {
                if (!string.IsNullOrEmpty(template.Component) && (template.IsPublic || isEmagged))
                    comp.Templates.Add(template.ID);
            }
        }

        UpdateUserInterface(uid, comp);
    }


    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var enumerator = EntityQueryEnumerator<PrinterDocComponent>();

        while (enumerator.MoveNext(out var uid, out var comp))
        {
            if (comp.IsProcessing || comp.JobQueue.Count == 0)
                continue;

            var (type, templateId) = comp.JobQueue.Dequeue();
            comp.IsProcessing = true;

            string jobTitle = type == PrinterJobType.Print && templateId != null && _proto.TryIndex<DocTemplatePrototype>(templateId, out var proto)
                ? Loc.GetString(proto.Name)
                : templateId ?? "Документ";

            comp.CurrentJobView = new PrinterJobView(jobTitle, type);
            UpdateUserInterface(uid, comp);

            _audioSystem.PlayPvs(comp.PrintSound, uid);

            Timer.Spawn(TimeSpan.FromSeconds(comp.JobDuration), () =>
            {
                if (Deleted(uid))
                    return;

                bool success = type switch
                {
                    PrinterJobType.Print when templateId != null => TryPrintInternal(uid, comp, templateId),
                    PrinterJobType.Copy => TryCopyInternal(uid, comp),
                    _ => false
                };

                comp.IsProcessing = false;
                comp.CurrentJobView = null;
                UpdateUserInterface(uid, comp);
            });
        }
    }

    private bool TryConsumeResources(EntityUid uid, PrinterDocComponent comp)
    {
        if (!_solution.TryGetSolution(uid, comp.Solution, out _, out var solution))
            return false;

        if (!solution.TryGetReagentQuantity(new ReagentId(comp.IncReagentProto, null), out var incVolume) || incVolume < comp.IncCost)
            return false;

        if (!_materialStorage.TryChangeMaterialAmount(uid, comp.PaperMaterial, -comp.PaperCost))
            return false;

        solution.RemoveReagent(new ReagentId(comp.IncReagentProto, null), comp.IncCost);
        return true;
    }

    private bool TryPrintInternal(EntityUid uid, PrinterDocComponent comp, string templateId)
    {
        var paper = Spawn(comp.PaperProtoId, Transform(uid).Coordinates);

        if (!TryComp<PaperComponent>(paper, out var paperComp) || !_docCache.TryGetValue(templateId, out var content))
            return false;

        int offsetHours = 3;
        int offsetYears = 1000;

        try
        {
            offsetHours = _configManager.GetCVar(SunriseCCVars.PrinterDocTimeOffsetHours);
            offsetYears = _configManager.GetCVar(SunriseCCVars.PrinterDocYearOffset);
        }
        catch
        {
            // Без подключения к серверу эти CVar недоступны используем компоненты
        }

        var date = DateTime.UtcNow
            .AddHours(offsetHours)
            .AddYears(offsetYears)
            .ToString("dd.MM.yyyy");

        var shift = _timing.CurTime - _roundStartTime;
        var timeString = $"{shift:hh\\:mm} {date}";

        var station = _stationSystem.GetOwningStation(uid);
        var stationName = station is null ? string.Empty : Name(station.Value);

        content = content.Replace("{timeString}", timeString);
        content = content.Replace("{stationName}", stationName);

        _paperSystem.SetContent((paper, paperComp), content);
        return true;
    }



    private bool TryCopyInternal(EntityUid uid, PrinterDocComponent comp)
    {
        var paper = Spawn(comp.PaperProtoId, Transform(uid).Coordinates);

        if (!TryComp<PaperComponent>(paper, out var paperComp))
            return false;

        if (TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count != 0)
        {
            var buckled = strap.BuckledEntities.First();
            if (TryComp<HumanoidAppearanceComponent>(buckled, out var humanoidAppearance))
            {
                var buttTexture = _proto.TryIndex(humanoidAppearance.Species, out var species) ? species.ButtScanTexture : null;
                var content = $"[tex path=\"{buttTexture}\" scale=15]";
                _paperSystem.SetContent((paper, paperComp), content);
                paperComp.EditingDisabled = true;
                return true;
            }
        }

        if (comp.CopySlot.HasItem && TryComp<PaperComponent>(comp.CopySlot.Item, out var copyPaperComponent))
        {
            _paperSystem.SetContent((paper, paperComp), copyPaperComponent.Content);
            paperComp.EditingDisabled = copyPaperComponent.EditingDisabled;
            return true;
        }

        return false;
    }

    public void UpdateUserInterface(EntityUid uid, PrinterDocComponent comp)
    {
        if (!_solution.TryGetSolution(uid, comp.Solution, out _, out var solution))
            return;

        float incVolume = solution.TryGetReagentQuantity(new ReagentId(comp.IncReagentProto, null), out var inc) ? inc.Value : 0;
        var availablePaper = _materialStorage.GetMaterialAmount(uid, comp.PaperMaterial);

        var state = new PrinterDocBoundUserInterfaceState(
            paperCount: availablePaper / 100,
            inkAmount: incVolume / 100,
            templates: comp.Templates.Select(t => t.ToString()).ToList(),
            canCopy: CanCopy(uid, comp),
            currentJob: comp.CurrentJobView,
            queue: comp.JobQueue.Select(j =>
            {
                string title = j.Type == PrinterJobType.Print && j.TemplateId != null && _proto.TryIndex<DocTemplatePrototype>(j.TemplateId, out var proto)
                    ? Loc.GetString(proto.Name)
                    : j.TemplateId ?? "Документ";
                return new PrinterJobView(title, j.Type);
            }).ToList()
        );

        _userInterfaceSystem.SetUiState(uid, PrinterDocUiKey.Key, state);
    }

    public bool CanCopy(EntityUid uid, PrinterDocComponent comp)
    {
        var hasCopyPaper = comp.CopySlot.HasItem;
        var hasBuckleUser = TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count != 0 &&
                            TryComp<HumanoidAppearanceComponent>(strap.BuckledEntities.First(), out _);

        return hasBuckleUser || hasCopyPaper;
    }

    private void OnEmagged(EntityUid uid, PrinterDocComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        args.Handled = true;

        component.Templates.Clear();

        foreach (var template in _proto.EnumeratePrototypes<DocTemplatePrototype>())
        {
            if (!string.IsNullOrEmpty(template.Component))
                component.Templates.Add(template.ID);
        }

        Dirty(uid, component);
        UpdateUserInterface(uid, component);
    }
    private void OnStationInitialized(StationInitializedEvent args)
    {
        Timer.Spawn(TimeSpan.FromMilliseconds(100), () =>
        {
            var query = EntityQueryEnumerator<PrinterDocComponent>();

            while (query.MoveNext(out var uid, out var comp))
            {
                var owningStation = _stationSystem.GetOwningStation(uid);

                if (owningStation != args.Station)
                    continue;

                _materialStorage.TryChangeMaterialAmount(uid, comp.PaperMaterial, comp.InitialPaperAmount);

                if (_solution.TryGetSolution(uid, comp.Solution, out var solEnt, out var solution))
                {
                    var reagent = new ReagentId(comp.IncReagentProto, null);
                    solution.AddReagent(reagent, comp.InitialInkAmount);
                    _solution.UpdateChemicals(solEnt.Value, needsReactionsProcessing: false);
                }

                UpdateUserInterface(uid, comp);
            }
        });
    }
}
