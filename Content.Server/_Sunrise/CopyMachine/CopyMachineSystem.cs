using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Content.Server.Materials;
using Content.Server._Sunrise.Documents;
using Content.Shared._Sunrise.CopyMachine;
using Content.Shared._Sunrise.Paperwork;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Materials;
using Content.Shared.Paper;
using Content.Shared.Labels.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Sunrise.CopyMachine;

public sealed partial class CopyMachineSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly MaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly LabelSystem _label = default!;
    [Dependency] private readonly DocumentFormatSystem _documentFormat = default!;

    private readonly Dictionary<string, string> _documentContentByTemplateId = new();

    private static readonly Regex DocumentTagRegex =
        new("<Document>(.*?)</Document>", RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly Queue<EntityUid> _pendingUIUpdateQueue = new();
    private readonly HashSet<EntityUid> _pendingUIUpdateSet = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CopyMachineComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<CopyMachineComponent, CopyMachinePrintMessage>(OnPrintRequested);
        SubscribeLocalEvent<CopyMachineComponent, CopyMachineCopyMessage>(OnCopyRequested);
        SubscribeLocalEvent<CopyMachineComponent, CopyMachineCancelJobMessage>(OnCancelJobRequested);
        SubscribeLocalEvent<CopyMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CopyMachineComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<CopyMachineComponent, EntInsertedIntoContainerMessage>(OnEntityInsertedIntoCopySlot);
        SubscribeLocalEvent<CopyMachineComponent, EntRemovedFromContainerMessage>(OnEntityRemovedFromCopySlot);
        SubscribeLocalEvent<CopyMachineComponent, MaterialAmountChangedEvent>(OnMaterialAmountChanged);
        SubscribeLocalEvent<CopyMachineComponent, StrappedEvent>(OnEntityStrapped);
        SubscribeLocalEvent<CopyMachineComponent, UnstrappedEvent>(OnEntityUnstrapped);
        SubscribeLocalEvent<CopyMachineComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<CopyMachineComponent, ComponentShutdown>(OnShutdown);

        _configManager.OnValueChanged(SunriseCCVars.DocumentTemplatePool, _ =>
        {
            RebuildDocumentTemplateContentCache();
            RefreshAllCopyMachineTemplates();
        }, false);

        RebuildDocumentTemplateContentCache();
    }

    private void OnCancelJobRequested(EntityUid uid, CopyMachineComponent component, CopyMachineCancelJobMessage args)
    {
        if (args.Index < 0 || args.Index >= component.JobQueue.Count)
            return;

        var removed = false;
        var count = component.JobQueue.Count;
        for (var i = 0; i < count; i++)
        {
            var job = component.JobQueue.Dequeue();
            if (i == args.Index)
            {
                removed = true;
                continue;
            }

            component.JobQueue.Enqueue(job);
        }

        if (removed)
        {
            RefundInkAndPaper((uid, component));
            QueueUIUpdate(new Entity<CopyMachineComponent>(uid, component));
        }
    }

    private void RefundInkAndPaper(Entity<CopyMachineComponent> ent)
    {
        _materialStorage.TryChangeMaterialAmount(ent.Owner, ent.Comp.PaperMaterial, ent.Comp.PaperCost);

        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.Solution, out var solutionEntity, out _))
            return;

        var inkReagentId = new ReagentId(ent.Comp.IncReagentProto, null);
        _solutionContainer.TryAddReagent(solutionEntity.Value, inkReagentId, ent.Comp.IncCost, out _);
    }

    private bool TryGetConfiguredTemplatePool([NotNullWhen(true)] out DocTemplatePoolPrototype? templatePoolPrototype)
    {
        var templatePoolId = _configManager.GetCVar(SunriseCCVars.DocumentTemplatePool);
        return _prototypeManager.TryIndex(templatePoolId, out templatePoolPrototype);
    }

    private void RebuildDocumentTemplateContentCache()
    {
        _documentContentByTemplateId.Clear();

        if (!TryGetConfiguredTemplatePool(out var templatePoolPrototype))
            return;

        foreach (var templateId in templatePoolPrototype.Templates)
        {
            if (!_prototypeManager.TryIndex(templateId, out var templatePrototype))
                continue;

            using var file = _resourceManager.ContentFileReadText(templatePrototype.Content);
            var fileText = file.ReadToEnd();
            var match = DocumentTagRegex.Match(fileText);
            var content = match.Success ? match.Groups[1].Value.Trim() : fileText.Trim();

            _documentContentByTemplateId[templatePrototype.ID] = content;
        }
    }

    // Мгновенная смена шаблонов в принтерах при смене CVar-а
    private void RefreshAllCopyMachineTemplates()
    {
        var copyMachineEnumerator = EntityQueryEnumerator<CopyMachineComponent>();
        while (copyMachineEnumerator.MoveNext(out var copyMachineUid, out var copyMachineComponent))
        {
            UpdateAvailableTemplates((copyMachineUid, copyMachineComponent));
        }
    }

    private void UpdateAvailableTemplates(Entity<CopyMachineComponent> ent)
    {
        ent.Comp.Templates.Clear();

        var isEmagged = HasComp<EmaggedComponent>(ent.Owner);

        if (!TryGetConfiguredTemplatePool(out var templatePoolPrototype))
            return;

        foreach (var templateId in templatePoolPrototype.Templates)
        {
            if (!_prototypeManager.TryIndex(templateId, out var templatePrototype))
                continue;

            if (!IsTemplateComponentAllowed(ent, templatePrototype.Component))
                continue;

            if (templatePrototype.IsPublic || isEmagged)
                ent.Comp.Templates.Add(templatePrototype.ID);
        }

        QueueUIUpdate(ent);
    }

    private static bool IsTemplateComponentAllowed(Entity<CopyMachineComponent> ent, string component)
    {
        if (ent.Comp.TemplateComponentWhitelist.Count == 0)
            return true;

        return ent.Comp.TemplateComponentWhitelist.Contains(component);
    }

    private void OnShutdown(Entity<CopyMachineComponent> ent, ref ComponentShutdown args)
    {
        _cachedBuckledEntityByCopyMachineUid.Remove(ent.Owner);
        _pendingUIUpdateSet.Remove(ent.Owner);
    }

    private void OnUiOpened(Entity<CopyMachineComponent> ent, ref BoundUIOpenedEvent args) => UpdateUserInterface(ent);
    private void OnSolutionChanged(Entity<CopyMachineComponent> ent, ref SolutionContainerChangedEvent args) => QueueUIUpdate(ent);
    private void OnEntityRemovedFromCopySlot(Entity<CopyMachineComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != CopyMachineComponent.CopySlotId)
            return;

        QueueUIUpdate(ent);
    }

    private void OnMaterialAmountChanged(Entity<CopyMachineComponent> ent, ref MaterialAmountChangedEvent args) => QueueUIUpdate(ent);

    private void OnEntityInsertedIntoCopySlot(Entity<CopyMachineComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != CopyMachineComponent.CopySlotId)
            return;

        QueueUIUpdate(ent);
    }

    private void QueueUIUpdate(EntityUid copyMachineUid)
    {
        if (!_userInterface.IsUiOpen(copyMachineUid, CopyMachineUiKey.Key))
            return;

        if (_pendingUIUpdateSet.Add(copyMachineUid))
            _pendingUIUpdateQueue.Enqueue(copyMachineUid);
    }

    private void QueueUIUpdate(Entity<CopyMachineComponent> ent) => QueueUIUpdate(ent.Owner);

    private void FlushUIUpdates()
    {
        while (_pendingUIUpdateQueue.TryDequeue(out var copyMachineUid))
        {
            _pendingUIUpdateSet.Remove(copyMachineUid);

            if (!_userInterface.IsUiOpen(copyMachineUid, CopyMachineUiKey.Key))
                continue;

            if (!TryComp(copyMachineUid, out CopyMachineComponent? copyMachineComponent))
                continue;

            UpdateUserInterface(new Entity<CopyMachineComponent>(copyMachineUid, copyMachineComponent));
        }
    }

    private void OnPrintRequested(Entity<CopyMachineComponent> ent, ref CopyMachinePrintMessage message)
    {
        TryQueueJob(ent, CopyMachineJobType.Print, message.TemplateId);
    }

    private void OnCopyRequested(Entity<CopyMachineComponent> ent, ref CopyMachineCopyMessage message)
    {
        TryQueueJob(ent, CopyMachineJobType.Copy, null);
    }

    private void OnMapInit(Entity<CopyMachineComponent> ent, ref MapInitEvent args)
    {
        _itemSlots.AddItemSlot(ent.Owner, CopyMachineComponent.CopySlotId, ent.Comp.CopySlot);
        UpdateRunningAppearance(ent.Owner, false);

        UpdateAvailableTemplates(ent);

        QueueUIUpdate(ent);
    }

    private void OnEmagged(Entity<CopyMachineComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        args.Handled = true;

        ent.Comp.Templates.Clear();

        foreach (var templatePrototype in _prototypeManager.EnumeratePrototypes<DocTemplatePrototype>())
        {
            if (string.IsNullOrEmpty(templatePrototype.Component))
                continue;

            if (!IsTemplateComponentAllowed(ent, templatePrototype.Component))
                continue;

            ent.Comp.Templates.Add(templatePrototype.ID);
        }

        Dirty(ent);
        QueueUIUpdate(ent);
    }

    private void TryQueueJob(Entity<CopyMachineComponent> ent, CopyMachineJobType jobType, string? templateId)
    {
        if (ent.Comp.JobQueue.Count >= ent.Comp.MaxQueueSize)
            return;

        if (!TryConsumeInkAndPaper(ent))
            return;

        ent.Comp.JobQueue.Enqueue((jobType, templateId));

        QueueUIUpdate(ent);
    }

    private string GetJobDisplayTitle(CopyMachineJobType jobType, string? templateId)
    {
        if (jobType == CopyMachineJobType.Print &&
            templateId != null &&
            _prototypeManager.TryIndex<DocTemplatePrototype>(templateId, out var templatePrototype))
        {
            return Loc.GetString(templatePrototype.Name);
        }

        return templateId ?? Loc.GetString("copy-machine-default-document-title");
    }

    private void StartQueuedJob(Entity<CopyMachineComponent> ent, CopyMachineJobType jobType, string? templateId, TimeSpan currentTime)
    {
        ent.Comp.IsProcessing = true;
        ent.Comp.NextPrintTime = currentTime + TimeSpan.FromSeconds(ent.Comp.JobDuration);
        UpdateRunningAppearance(ent.Owner, true);

        ent.Comp.CurrentJobView = new CopyMachineJobView(GetJobDisplayTitle(jobType, templateId), jobType, templateId);

        QueueUIUpdate(ent);
        _audioSystem.PlayPvs(ent.Comp.PrintSound, ent.Owner);
    }

    private void CompleteCurrentJob(Entity<CopyMachineComponent> ent)
    {
        var jobView = ent.Comp.CurrentJobView;
        if (jobView != null)
        {
            var jobType = jobView.Type;
            var templateId = jobView.TemplateId;
            _ = jobType switch
            {
                CopyMachineJobType.Print when templateId != null => TryPrintFromTemplate(ent, templateId),
                CopyMachineJobType.Copy => TryCopyFromSlotOrButtScan(ent),
                _ => false
            };
        }

        ent.Comp.IsProcessing = false;
        ent.Comp.CurrentJobView = null;
        ent.Comp.NextPrintTime = TimeSpan.Zero;
        UpdateRunningAppearance(ent.Owner, false);

        QueueUIUpdate(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var copyMachineEnumerator = EntityQueryEnumerator<CopyMachineComponent>();
        var currentTime = _gameTiming.CurTime;

        while (copyMachineEnumerator.MoveNext(out var copyMachineUid, out var copyMachineComponent))
        {
            var ent = new Entity<CopyMachineComponent>(copyMachineUid, copyMachineComponent);

            if (!ent.Comp.IsProcessing && ent.Comp.JobQueue.Count > 0)
            {
                var (jobType, templateId) = ent.Comp.JobQueue.Dequeue();
                StartQueuedJob(ent, jobType, templateId, currentTime);
            }

            if (ent.Comp.IsProcessing && currentTime >= ent.Comp.NextPrintTime)
                CompleteCurrentJob(ent);
        }

        FlushUIUpdates();
    }

    private bool TryConsumeInkAndPaper(Entity<CopyMachineComponent> ent)
    {
        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
            return false;

        var inkReagentId = new ReagentId(ent.Comp.IncReagentProto, null);

        if (!solution.TryGetReagentQuantity(inkReagentId, out var availableInkVolume) || availableInkVolume < ent.Comp.IncCost)
            return false;

        if (!_materialStorage.TryChangeMaterialAmount(ent.Owner, ent.Comp.PaperMaterial, -ent.Comp.PaperCost))
            return false;

        solution.RemoveReagent(inkReagentId, ent.Comp.IncCost);

        return true;
    }

    private bool TryPrintFromTemplate(Entity<CopyMachineComponent> ent, string templateId)
    {
        var paperEntity = Spawn(ent.Comp.PaperProtoId, Transform(ent.Owner).Coordinates);

        if (!TryComp<PaperComponent>(paperEntity, out var paperComponent) ||
            !_documentContentByTemplateId.TryGetValue(templateId, out var templateContent) ||
            !_prototypeManager.TryIndex<DocTemplatePrototype>(templateId, out var templatePrototype))
            return false;

        // ЕДИНАЯ система форматирования документов
        templateContent = _documentFormat.Format(templateContent, ent.Owner);

        if (PaperInteractiveTagParsing.ContainsInteractiveTags(templateContent))
        {
            EnsureComp<PaperTemplateFormComponent>(paperEntity);
        }
        _paper.SetContent((paperEntity, paperComponent), templateContent);

        if (templatePrototype.Header != null)
            _paper.SetImageContent((paperEntity, paperComponent), templatePrototype.Header);

        return true;
    }

    public void UpdateUserInterface(Entity<CopyMachineComponent> ent)
    {
        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
            return;

        var inkReagentId = new ReagentId(ent.Comp.IncReagentProto, null);
        float availableInkVolume = solution.TryGetReagentQuantity(inkReagentId, out var inkQuantity) ? inkQuantity.Value : 0;
        var availablePaperAmount = _materialStorage.GetMaterialAmount(ent.Owner, ent.Comp.PaperMaterial);

        var availableTemplateIds = new List<string>(ent.Comp.Templates.Count);
        foreach (var template in ent.Comp.Templates)
        {
            availableTemplateIds.Add(template.ToString());
        }

        var queuedJobs = new List<CopyMachineJobView>(ent.Comp.JobQueue.Count);
        foreach (var job in ent.Comp.JobQueue)
        {
            queuedJobs.Add(new CopyMachineJobView(GetJobDisplayTitle(job.Type, job.TemplateId), job.Type, job.TemplateId));
        }

        var state = new CopyMachineBoundUserInterfaceState(
            paperCount: availablePaperAmount / 100,
            inkAmount: availableInkVolume / 100,
            templates: availableTemplateIds,
            canCopy: CanCopy(ent),
            currentJob: ent.Comp.CurrentJobView,
            queue: queuedJobs
        );

        _userInterface.SetUiState(ent.Owner, CopyMachineUiKey.Key, state);
    }

    public bool CanCopy(Entity<CopyMachineComponent> ent)
    {
        var hasPaperInCopySlot = ent.Comp.CopySlot.HasItem;
        var hasStrappedHumanoid = TryGetBuckledHumanoidAppearance(ent.Owner, out _);

        return hasStrappedHumanoid || hasPaperInCopySlot;
    }

    private void UpdateRunningAppearance(EntityUid uid, bool isRunning)
    {
        _appearance.SetData(uid, CopyMachineVisuals.IsRunning, isRunning);
    }
}
