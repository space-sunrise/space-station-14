using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Content.Server.GameTicking.Events;
using Content.Server.Materials;
using Content.Server.Station.Systems;
using Content.Shared._Sunrise.CopyMachine;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Materials;
using Content.Shared.Paper;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.CopyMachine;

public sealed class CopyMachineSystem : EntitySystem
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
    [Dependency] private readonly LabelSystem _labelSystem = default!;

    private TimeSpan _roundStartTime;
    private readonly Dictionary<string, string> _docCache = new();

    private static readonly Regex DocRegex =
        new("<Document>(.*?)</Document>", RegexOptions.Singleline | RegexOptions.Compiled);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<CopyMachineComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<CopyMachineComponent, CopyMachinePrintMessage>(OnPrintMessage);
        SubscribeLocalEvent<CopyMachineComponent, CopyMachineCopyMessage>(OnCopyMessage);
        SubscribeLocalEvent<CopyMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CopyMachineComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<CopyMachineComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<CopyMachineComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
        SubscribeLocalEvent<CopyMachineComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);
        SubscribeLocalEvent<CopyMachineComponent, StrappedEvent>(OnStasisStrapped);
        SubscribeLocalEvent<CopyMachineComponent, UnstrappedEvent>(OnStasisUnstrapped);
        SubscribeLocalEvent<CopyMachineComponent, GotEmaggedEvent>(OnEmagged);

        _configManager.OnValueChanged(SunriseCCVars.CopyMachineTemplatePool, _ =>
        {
            CacheAllDocuments();
            RebuildAllPrinters();
        }, false);

        CacheAllDocuments();
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        _roundStartTime = _timing.CurTime;
    }

    private void CacheAllDocuments()
    {
        _docCache.Clear();

        var pool = _configManager.GetCVar(SunriseCCVars.CopyMachineTemplatePool);

        if (!_proto.TryIndex<DocTemplatePoolPrototype>(pool, out var poolProto))
            return;

        foreach (var template in poolProto.Templates)
        {
            if (!_proto.TryIndex(template, out var templateProto))
                continue;

            using var file = _resourceManager.ContentFileReadText(templateProto.Content);
            var fileText = file.ReadToEnd();
            var match = DocRegex.Match(fileText);
            var content = match.Success ? match.Groups[1].Value.Trim() : fileText.Trim();

            _docCache[templateProto.ID] = content;
        }
    }

    // Мгновенная смена шаблонов в принтерах при смене CVar-а
    private void RebuildAllPrinters()
    {
        var query = EntityQueryEnumerator<CopyMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdatePrinterTemplates(uid, comp);
        }
    }

    private void UpdatePrinterTemplates(EntityUid uid, CopyMachineComponent comp)
    {
        comp.Templates.Clear();

        var isEmagged = HasComp<EmaggedComponent>(uid);

        var pool = _configManager.GetCVar(SunriseCCVars.CopyMachineTemplatePool);

        if (!_proto.TryIndex<DocTemplatePoolPrototype>(pool, out var poolProto))
            return;

        foreach (var template in poolProto.Templates)
        {
            if (!_proto.TryIndex(template, out var templateProto))
                continue;

            if (templateProto.IsPublic || isEmagged)
                comp.Templates.Add(templateProto.ID);
        }

        UpdateUserInterface(uid, comp);
    }

    private void OnUiOpened(EntityUid uid, CopyMachineComponent comp, BoundUIOpenedEvent args) => UpdateUserInterface(uid, comp);
    private void OnSolutionChanged(EntityUid uid, CopyMachineComponent comp, SolutionContainerChangedEvent args) => UpdateUserInterface(uid, comp);
    private void OnItemRemoved(EntityUid uid, CopyMachineComponent comp, EntRemovedFromContainerMessage args) => UpdateUserInterface(uid, comp);
    private void OnStasisStrapped(EntityUid uid, CopyMachineComponent comp, ref StrappedEvent args) => UpdateUserInterface(uid, comp);
    private void OnStasisUnstrapped(EntityUid uid, CopyMachineComponent comp, ref UnstrappedEvent args) => UpdateUserInterface(uid, comp);
    private void OnMaterialAmountChanged(EntityUid uid, CopyMachineComponent comp, ref MaterialAmountChangedEvent args) => UpdateUserInterface(uid, comp);

    private void OnItemInserted(EntityUid uid, CopyMachineComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != CopyMachineComponent.CopySlotId)
            return;

        UpdateUserInterface(uid, comp);
    }

    private void OnPrintMessage(EntityUid uid, CopyMachineComponent comp, CopyMachinePrintMessage msg)
    {
        if (comp.JobQueue.Count >= comp.MaxQueueSize)
            return;

        if (!TryConsumeResources(uid, comp))
            return;

        comp.JobQueue.Enqueue((CopyMachineJobType.Print, msg.TemplateId));
        UpdateUserInterface(uid, comp);
    }

    private void OnCopyMessage(EntityUid uid, CopyMachineComponent comp, CopyMachineCopyMessage msg)
    {
        if (comp.JobQueue.Count >= comp.MaxQueueSize)
            return;

        if (!TryConsumeResources(uid, comp))
            return;

        comp.JobQueue.Enqueue((CopyMachineJobType.Copy, null));
        UpdateUserInterface(uid, comp);
    }

    private void OnMapInit(EntityUid uid, CopyMachineComponent comp, MapInitEvent args)
    {
        _itemSlotsSystem.AddItemSlot(uid, CopyMachineComponent.CopySlotId, comp.CopySlot);

        UpdatePrinterTemplates(uid, comp);

        UpdateUserInterface(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var enumerator = EntityQueryEnumerator<CopyMachineComponent>();
        var curTime = _timing.CurTime;

        while (enumerator.MoveNext(out var uid, out var comp))
        {
            if (!comp.IsProcessing && comp.JobQueue.Count > 0)
            {
                var (type, templateId) = comp.JobQueue.Dequeue();
                comp.IsProcessing = true;
                comp.NextPrintTime = curTime + TimeSpan.FromSeconds(comp.JobDuration);

                string jobTitle = type == CopyMachineJobType.Print && templateId != null &&
                                  _proto.TryIndex<DocTemplatePrototype>(templateId, out var proto)
                    ? Loc.GetString(proto.Name)
                    : templateId ?? "Документ";

                comp.CurrentJobView = new CopyMachineJobView(jobTitle, type, templateId);
                UpdateUserInterface(uid, comp);
                _audioSystem.PlayPvs(comp.PrintSound, uid);
            }

            if (comp.IsProcessing && curTime >= comp.NextPrintTime)
            {
                var jobView = comp.CurrentJobView;
                if (jobView != null)
                {
                    var type = jobView.Type;
                    var templateId = jobView.TemplateId;
                    _ = type switch
                    {
                        CopyMachineJobType.Print when templateId != null => TryPrintInternal(uid, comp, templateId),
                        CopyMachineJobType.Copy => TryCopyInternal(uid, comp),
                        _ => false
                    };
                }
                comp.IsProcessing = false;
                comp.CurrentJobView = null;
                comp.NextPrintTime = TimeSpan.Zero;
                UpdateUserInterface(uid, comp);
            }
        }
    }

    private bool TryConsumeResources(EntityUid uid, CopyMachineComponent comp)
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

    private bool TryPrintInternal(EntityUid uid, CopyMachineComponent comp, string templateId)
    {
        var paper = Spawn(comp.PaperProtoId, Transform(uid).Coordinates);

        if (!TryComp<PaperComponent>(paper, out var paperComp) ||
            !_docCache.TryGetValue(templateId, out var content) ||
            !_proto.TryIndex<DocTemplatePrototype>(templateId, out var templateProto))
            return false;

        var offsetHours = _configManager.GetCVar(SunriseCCVars.CopyMachineTimeOffsetHours);
        var offsetYears = _configManager.GetCVar(SunriseCCVars.CopyMachineYearOffset);

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
        if (templateProto.Header != null)
            _paperSystem.SetImageContent((paper, paperComp), templateProto.Header);
        return true;
    }

    private bool TryCopyInternal(EntityUid uid, CopyMachineComponent comp)
    {
        var paper = Spawn(comp.PaperProtoId, Transform(uid).Coordinates);
        if (!TryComp<PaperComponent>(paper, out var paperComp))
            return false;

        if (TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count != 0)
        {
            var buckled = strap.BuckledEntities.First();
            if (TryComp<HumanoidAppearanceComponent>(buckled, out var humanoidAppearance))
            {
                var buttTexture = _proto.TryIndex(humanoidAppearance.Species, out var species) ? species.ButtScan : null;
                if (buttTexture == null)
                    return false;
                _paperSystem.SetImageContent((paper, paperComp), buttTexture, new Vector2(15, 15));
                paperComp.EditingDisabled = true;
                return true;
            }
        }

        if (comp.CopySlot.HasItem && TryComp<PaperComponent>(comp.CopySlot.Item, out var srcPaper))
        {
            _paperSystem.SetContent((paper, paperComp), srcPaper.Content);
            if (srcPaper.ImageContent != null)
                _paperSystem.SetImageContent((paper, paperComp), srcPaper.ImageContent, srcPaper.ImageScale);
            paperComp.EditingDisabled = srcPaper.EditingDisabled;

            if (srcPaper.StampState != null && srcPaper.StampedBy != null)
                foreach (var stamp in srcPaper.StampedBy)
                    _paperSystem.TryStamp((paper, paperComp), stamp, srcPaper.StampState);

            if (TryComp<LabelComponent>(comp.CopySlot.Item, out var srcLabel) && !string.IsNullOrWhiteSpace(srcLabel.CurrentLabel))
                _labelSystem.Label(paper, srcLabel.CurrentLabel);

            return true;
        }

        return false;
    }

    public void UpdateUserInterface(EntityUid uid, CopyMachineComponent comp)
    {
        if (!_solution.TryGetSolution(uid, comp.Solution, out _, out var solution))
            return;

        float incVolume = solution.TryGetReagentQuantity(new ReagentId(comp.IncReagentProto, null), out var inc) ? inc.Value : 0;
        var availablePaper = _materialStorage.GetMaterialAmount(uid, comp.PaperMaterial);

        var state = new CopyMachineBoundUserInterfaceState(
            paperCount: availablePaper / 100,
            inkAmount: incVolume / 100,
            templates: comp.Templates.Select(t => t.ToString()).ToList(),
            canCopy: CanCopy(uid, comp),
            currentJob: comp.CurrentJobView,
            queue: comp.JobQueue.Select(j =>
            {
                string title = j.Type == CopyMachineJobType.Print && j.TemplateId != null &&
                               _proto.TryIndex<DocTemplatePrototype>(j.TemplateId, out var proto)
                    ? Loc.GetString(proto.Name)
                    : j.TemplateId ?? "Документ";
                return new CopyMachineJobView(title, j.Type, j.TemplateId);
            }).ToList()
        );

        _userInterfaceSystem.SetUiState(uid, CopyMachineUiKey.Key, state);
    }

    public bool CanCopy(EntityUid uid, CopyMachineComponent comp)
    {
        var hasCopyPaper = comp.CopySlot.HasItem;
        var hasBuckleUser = TryComp<StrapComponent>(uid, out var strap) && strap.BuckledEntities.Count != 0 &&
                            TryComp<HumanoidAppearanceComponent>(strap.BuckledEntities.First(), out _);

        return hasBuckleUser || hasCopyPaper;
    }

    private void OnEmagged(EntityUid uid, CopyMachineComponent component, ref GotEmaggedEvent args)
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
}
