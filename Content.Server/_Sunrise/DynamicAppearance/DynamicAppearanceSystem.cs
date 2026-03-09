using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Administration.Managers;
using Content.Server.Cloning;
using Content.Server.Access.Systems;
using Content.Server.DoAfter;
using Content.Server.Inventory;
using Content.Server.StationRecords.Systems;
using Content.Server._Sunrise.Mood;
using Content.Shared._Sunrise;
using Content.Shared._Sunrise.DynamicAppearance;
using Content.Shared._Sunrise.Mood;
using Content.Shared._Sunrise.MarkingEffects;
using Content.Shared._Sunrise.TTS;
using Content.Shared.Buckle;
using Content.Shared.CCVar;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Implants.Components;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StationRecords;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Content.Sunrise.Interfaces.Shared;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Sunrise.DynamicAppearance;

/// <summary>
/// Allows entities with <see cref="DynamicAppearanceComponent"/> to edit their
/// appearance in-round through a BUI (markings, skin color, eye color, etc.).
/// </summary>
public sealed class DynamicAppearanceSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly IdCardSystem _idCard = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly MoodSystem _mood = default!;
    [Dependency] private readonly CloningSystem _cloning = default!;

    // Sunrise-Sponsors
    private ISharedSponsorsManager? _sponsorsManager;
    private readonly HashSet<(EntityUid Target, NetUserId UserId)> _adminOverrides = [];

    /// <summary>Same regex as <see cref="HumanoidCharacterProfile"/> for character name validation.</summary>
    private static readonly Regex RestrictedNameRegex = new("[^А-Яа-яA-Za-zёЁ0-9 ,\\-,'.]");
    private static readonly Regex ICNameCaseRegex = new(@"^(?<word>\w)|\b(?<word>\w)(?=\w*$)");

    public override void Initialize()
    {
        base.Initialize();

        // Sunrise-Sponsors
        IoCManager.Instance!.TryResolveType(out _sponsorsManager);

        SubscribeLocalEvent<DynamicAppearanceComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<DynamicAppearanceComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<DynamicAppearanceComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<DynamicAppearanceComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<DynamicAppearanceComponent, BoundUserInterfaceMessageAttempt>(OnUiMessageAttempt);
        SubscribeLocalEvent<DynamicAppearanceComponent, DynamicAppearanceSaveDoAfterEvent>(OnSaveDoAfter);
        SubscribeLocalEvent<DynamicAppearanceComponent, GetVerbsEvent<AlternativeVerb>>(OnVerbsRequest);

        Subs.BuiEvents<DynamicAppearanceComponent>(DynamicAppearanceUiKey.Key, subs =>
        {
            subs.Event<DynamicAppearanceSaveMessage>(OnSaveMessage);
            subs.Event<DynamicAppearanceSetAdminOverrideMessage>(OnAdminOverrideMessage);
        });
    }

    #region Lifecycle

    private void OnComponentStartup(EntityUid uid, DynamicAppearanceComponent component, ComponentStartup args)
    {
        if (TryComp<UserInterfaceComponent>(uid, out var ui))
        {
            var interfaceData = new InterfaceData("Content.Client._Sunrise.DynamicAppearance.DynamicAppearanceBoundUserInterface");
            // Используется своя проверка (т.е. только админ или владелец BUI может его открыть).
            interfaceData.InteractionRange = -1f;
            _ui.SetUi(uid, DynamicAppearanceUiKey.Key, interfaceData);
        }
    }

    private void OnComponentRemove(EntityUid uid, DynamicAppearanceComponent component, ComponentRemove args)
    {
        _adminOverrides.RemoveWhere(entry => entry.Target == uid);

        // Close the editor UI if open when the component is removed.
        _ui.CloseUi(uid, DynamicAppearanceUiKey.Key);
    }

    private void OnUiOpened(Entity<DynamicAppearanceComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(DynamicAppearanceUiKey.Key))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;

        SendState(ent.Owner, humanoid, ent.Comp);
        SendPermissions(ent.Owner, args.Actor);
    }

    private void OnUiClosed(Entity<DynamicAppearanceComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(DynamicAppearanceUiKey.Key))
            return;

        ClearAdminOverride(ent.Owner, args.Actor);
    }

    private void OnUiMessageAttempt(Entity<DynamicAppearanceComponent> ent, ref BoundUserInterfaceMessageAttempt args)
    {
        if (args.UiKey is not DynamicAppearanceUiKey.Key
            || args.Message is not OpenBoundInterfaceMessage)
        {
            return;
        }

        if (_admin.IsAdmin(args.Actor))
            return;

        if (!CanOpenAppearanceUi(ent, args.Actor))
            args.Cancel();
    }

    #endregion

    #region Verb

    private void OnVerbsRequest(EntityUid uid, DynamicAppearanceComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!CanOpenAppearanceUi((uid, component), args.User))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("dynamic-appearance-verb"),
            IconEntity = GetNetEntity(uid),
            Act = () => _ui.OpenUi(uid, DynamicAppearanceUiKey.Key, actor.PlayerSession),
            Priority = -2,
        });
    }

    #endregion

    #region BUI message handlers

    private void OnSaveMessage(Entity<DynamicAppearanceComponent> ent, ref DynamicAppearanceSaveMessage args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;

        if (!TrySaveAppearance((ent.Owner, ent.Comp, humanoid), args.Actor, args.State))
            return;
    }

    private void OnSaveDoAfter(Entity<DynamicAppearanceComponent> ent, ref DynamicAppearanceSaveDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;

        if (!TrySaveAppearance((ent.Owner, ent.Comp, humanoid), args.User, args.State, fromDoAfter: true))
            return;

        args.Handled = true;
    }

    private bool TrySaveAppearance(Entity<DynamicAppearanceComponent, HumanoidAppearanceComponent> ent, EntityUid actor, DynamicAppearanceState state, bool fromDoAfter = false)
    {
        if (!CanSaveAppearance(ent, actor))
            return false;

        if (!fromDoAfter
            && !ShouldBypassSaveDelay(actor)
            && ent.Comp1.SaveDelay > TimeSpan.Zero)
        {
            var doAfter = new DynamicAppearanceSaveDoAfterEvent(state);
            var doAfterArgs = new DoAfterArgs(EntityManager, actor, ent.Comp1.SaveDelay, doAfter, ent.Owner, target: ent.Owner)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                DuplicateCondition = DuplicateConditions.SameTarget,
            };

            return _doAfter.TryStartDoAfter(doAfterArgs);
        }

        DoSaveAppearance(ent, actor, state);
        return true;
    }

    private bool CanSaveAppearance(Entity<DynamicAppearanceComponent, HumanoidAppearanceComponent> ent, EntityUid actor)
    {
        if (_admin.IsAdmin(actor))
            return true;

        return actor == ent.Owner;
    }

    private bool ShouldBypassSaveDelay(EntityUid actor)
    {
        return _admin.IsAdmin(actor);
    }

    private void DoSaveAppearance(Entity<DynamicAppearanceComponent, HumanoidAppearanceComponent> ent, EntityUid actor, DynamicAppearanceState state)
    {
        var humanoid = ent.Comp2;
        var actorSession = TryComp<ActorComponent>(actor, out var actorComp)
            ? actorComp.PlayerSession
            : null;

        var ignoreRestrictions = HasAdminOverride(ent.Owner, actor);
        var allowed = ignoreRestrictions ? DynamicAppearanceFields.All : ent.Comp1.AllowedFields;

        // ── Resolve sponsor markings for this session ──
        HashSet<string>? sponsorProtos = null;
        if (_sponsorsManager != null && TryComp<ActorComponent>(ent.Owner, out var targetActorComp))
        {
            if (_sponsorsManager.TryGetPrototypes(targetActorComp.PlayerSession.UserId, out var sp))
                sponsorProtos = [.. sp];
        }

        var targetSpecies = humanoid.Species;
        if (allowed.HasFlag(DynamicAppearanceFields.Species)
            && TryGetValidSpecies(state.Species, sponsorProtos, ignoreRestrictions, out var speciesId))
        {
            targetSpecies = speciesId;
        }

        var speciesChanged = targetSpecies != humanoid.Species;
        if (!_prototypeManager.TryIndex<SpeciesPrototype>(targetSpecies, out var speciesProto))
            return;

        var skinStrategy = _prototypeManager.Index(speciesProto.SkinColoration).Strategy;
        var targetSkinColor = skinStrategy.EnsureVerified(
            allowed.HasFlag(DynamicAppearanceFields.SkinColor)
                ? state.SkinColor
                : humanoid.SkinColor);
        var targetEyeColor = allowed.HasFlag(DynamicAppearanceFields.EyeColor)
            ? state.EyeColor
            : humanoid.EyeColor;

        var targetSex = humanoid.Sex;
        if (allowed.HasFlag(DynamicAppearanceFields.Sex))
        {
            targetSex = speciesProto.Sexes.Contains(state.Sex)
                ? state.Sex
                : speciesProto.Sexes[0];
        }
        else if (speciesChanged && !speciesProto.Sexes.Contains(targetSex))
        {
            targetSex = speciesProto.Sexes[0];
        }

        var requestedBodyType = allowed.HasFlag(DynamicAppearanceFields.BodyType)
            ? state.BodyType
            : (string)humanoid.BodyType;
        var targetBodyType = ResolveValidBodyType(speciesProto, targetSex, requestedBodyType);
        var reopenedOnReplacement = false;

        if (speciesChanged)
        {
            var swapped = RespawnAsSpeciesPrototype(ent, speciesProto);
            if (swapped == null)
                return;

            ent = swapped.Value;
            humanoid = ent.Comp2;
            reopenedOnReplacement = actorSession != null;
        }

        var sexChanged = targetSex != humanoid.Sex;
        if (sexChanged)
            _humanoid.SetSex(ent, targetSex, false, humanoid);

        if (speciesChanged || sexChanged || targetBodyType != humanoid.BodyType)
            _humanoid.SetBodyType(ent, targetBodyType, false, humanoid);

        // ── Markings ──
        // Always normalize and filter incoming markings regardless of whitelist,
        // so malicious clients cannot inject malformed or illegal payloads.
        var sanitizedMarkings = SanitizeIncomingMarkings(
            state.MarkingSet,
            humanoid.Species,
            targetSex,
            targetSkinColor,
            sponsorProtos,
            ignoreRestrictions);

        // Only apply to the humanoid component for the categories the component allows.
        // Categories outside the allowed set are preserved from the current appearance.
        var mergedMarkings = new List<Marking>();
        foreach (var (category, markings) in humanoid.MarkingSet.Markings)
        {
            var isHairCategory = category is MarkingCategories.Hair or MarkingCategories.FacialHair;

            if (isHairCategory && allowed.HasFlag(DynamicAppearanceFields.Hair))
                continue;
            if (!isHairCategory && allowed.HasFlag(DynamicAppearanceFields.Markings))
                continue;

            mergedMarkings.AddRange(markings.Select(marking => new Marking(marking)));
        }

        foreach (var (category, markings) in sanitizedMarkings.Markings)
        {
            var isHairCategory = category is MarkingCategories.Hair or MarkingCategories.FacialHair;

            if (isHairCategory && !allowed.HasFlag(DynamicAppearanceFields.Hair))
                continue;
            if (!isHairCategory && !allowed.HasFlag(DynamicAppearanceFields.Markings))
                continue;

            mergedMarkings.AddRange(markings.Select(marking => new Marking(marking)));
        }

        var newSet = new MarkingSet(mergedMarkings, speciesProto.MarkingPoints, _markingManager, _prototypeManager);
        newSet.EnsureValid(_markingManager);
        newSet.EnsureSpecies(humanoid.Species, targetSkinColor, _markingManager, _prototypeManager);
        newSet.EnsureSexes(targetSex, _markingManager);
        if (speciesChanged)
            newSet = ReapplyMarkingsForSpecies(newSet, speciesProto, humanoid.Species, targetSex, targetSkinColor, targetEyeColor);

        humanoid.MarkingSet = newSet;

        // ── Voice ──
        var targetVoice = humanoid.Voice;
        // Если поле разрешено для изменения и клиент указал верный голос (с проверкой на новый пол), то применяем его.
        if (
            allowed.HasFlag(DynamicAppearanceFields.Voice)
            && !string.IsNullOrEmpty(state.Voice)
            && TryGetValidVoice(state.Voice, targetSex, sponsorProtos, ignoreRestrictions, out var voiceId)
        )
        {
            targetVoice = voiceId;
        }
        // Иначе проверяем изменился ли пол, и если да, то подходит ли текущий голос под новый пол. Если нет, то ставим дефолтный голос для нового пола.
        else if (
            sexChanged
            && !(
                _prototypeManager.TryIndex<TTSVoicePrototype>(targetVoice, out var voiceProto)
                && ValidateVoiceSex(voiceProto, targetSex)
            )
        )
        {
            targetVoice = SharedHumanoidAppearanceSystem.DefaultSexVoice[targetSex];
        }

        if (!string.IsNullOrEmpty(targetVoice) && targetVoice != humanoid.Voice)
            _humanoid.SetTTSVoice(ent, targetVoice, humanoid);

        // ── Skin color ──
        if (allowed.HasFlag(DynamicAppearanceFields.SkinColor) || speciesChanged)
            _humanoid.SetSkinColor(ent, targetSkinColor, humanoid: humanoid);

        // ── Eye color ──
        humanoid.EyeColor = targetEyeColor;

        // Preview applies species defaults through `LoadProfile()`, so mirror that here.
        // This ensures species-specific default markings are actually present in-game
        // after a species swap, even if the client draft itself doesn't contain them.
        humanoid.MarkingSet.EnsureDefault(humanoid.SkinColor, humanoid.EyeColor, _markingManager);

        // ── Gender / Pronouns ──
        if (allowed.HasFlag(DynamicAppearanceFields.Pronouns))
            _humanoid.SetGender((ent.Owner, humanoid), state.Gender);

        // ── Size + Age ──
        if (allowed.HasFlag(DynamicAppearanceFields.Age) || speciesChanged)
            humanoid.Age = Math.Clamp(allowed.HasFlag(DynamicAppearanceFields.Age) ? state.Age : humanoid.Age, speciesProto.MinAge, speciesProto.MaxAge);

        if (allowed.HasFlag(DynamicAppearanceFields.Size) || speciesChanged)
        {
            var width = allowed.HasFlag(DynamicAppearanceFields.Size) ? state.Width : humanoid.Width;
            var height = allowed.HasFlag(DynamicAppearanceFields.Size) ? state.Height : humanoid.Height;

            humanoid.Width = Math.Clamp(width, speciesProto.MinWidth, speciesProto.MaxWidth);
            humanoid.Height = Math.Clamp(height, speciesProto.MinHeight, speciesProto.MaxHeight);
        }

        // ── Name ──
        if (allowed.HasFlag(DynamicAppearanceFields.Name)
            && !string.IsNullOrWhiteSpace(state.Name))
        {
            var name = ValidateName(state.Name);
            if (!string.IsNullOrEmpty(name))
                _meta.SetEntityName(ent, name);
        }

        UpdateStationRecord(ent);
        Dirty(ent, humanoid);
        SendState(ent, humanoid, ent.Comp1);

        if (reopenedOnReplacement && actorSession != null)
            _ui.OpenUi(ent.Owner, DynamicAppearanceUiKey.Key, actorSession);
    }

    private void OnAdminOverrideMessage(Entity<DynamicAppearanceComponent> ent, ref DynamicAppearanceSetAdminOverrideMessage args)
    {
        if (!TryComp<ActorComponent>(args.Actor, out var actor))
            return;

        if (!_admin.IsAdmin(actor.PlayerSession))
        {
            ClearAdminOverride(ent.Owner, args.Actor);
            SendPermissions(ent.Owner, args.Actor);
            return;
        }

        SetAdminOverride(ent.Owner, actor.PlayerSession.UserId, args.Enabled);
        SendPermissions(ent.Owner, args.Actor);
    }

    #endregion

    #region Helpers

    private Entity<DynamicAppearanceComponent, HumanoidAppearanceComponent>? RespawnAsSpeciesPrototype(
        Entity<DynamicAppearanceComponent, HumanoidAppearanceComponent> ent,
        SpeciesPrototype speciesProto)
    {
        var uid = ent.Owner;
        _ui.CloseUi(uid, DynamicAppearanceUiKey.Key);
        _buckle.TryUnbuckle(uid, uid, true);

        var transform = Transform(uid);
        var replacement = Spawn(
            speciesProto.Prototype,
            _transform.GetMapCoordinates(uid, transform),
            rotation: _transform.GetWorldRotation(uid));

        if (_container.TryGetContainingContainer((uid, transform, null), out var container))
            _container.Insert(replacement, container);

        if (!TryComp<HumanoidAppearanceComponent>(replacement, out var replacementHumanoid))
        {
            QueueDel(replacement);
            return null;
        }

        var replacementDynamic = EnsureComp<DynamicAppearanceComponent>(replacement);
        replacementDynamic.AllowedFields = ent.Comp1.AllowedFields;
        replacementDynamic.SaveDelay = ent.Comp1.SaveDelay;

        _meta.SetEntityName(replacement, MetaData(uid).EntityName);
        TransferSpeciesSwapState(uid, replacement);
        MoveAdminOverrides(uid, replacement);

        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _mind.TransferTo(mindId, replacement, mind: mind);

        QueueDel(uid);
        Dirty(replacement, replacementDynamic);
        Dirty(replacement, replacementHumanoid);

        return (replacement, replacementDynamic, replacementHumanoid);
    }

    private void TransferSpeciesSwapState(EntityUid source, EntityUid target)
    {
        TransferDamage(source, target);
        TransferInventory(source, target);
        TransferNutrition(source, target);
        TransferMood(source, target);
        TransferRecordKey(source, target);

        if (TryComp<StorageComponent>(source, out var sourceStorage)
            && TryComp<StorageComponent>(target, out var targetStorage))
        {
            _cloning.CopyStorage((source, sourceStorage), (target, targetStorage));
        }

        if (TryComp<ImplantedComponent>(source, out var sourceImplants))
            _cloning.CopyImplants((source, sourceImplants), target, copyStorage: true);

        if (TryComp<StatusEffectContainerComponent>(source, out var sourceStatus)
            && TryComp<StatusEffectContainerComponent>(target, out var targetStatus))
        {
            _cloning.CopyStatusEffects((source, sourceStatus), (target, targetStatus));
        }
    }

    private void TransferDamage(EntityUid source, EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var targetDamage)
            || !_mobThreshold.GetScaledDamage(source, target, out var damage)
            || damage == null)
        {
            return;
        }

        _damageable.SetDamage((target, targetDamage), damage);
    }

    private void TransferInventory(EntityUid source, EntityUid target)
    {
        EntityUid? idItem = null;
        var pocketItems = new List<EntityUid>();
        if (TryComp<InventoryComponent>(source, out var sourceInventory))
        {
            _inventory.TryGetSlotEntity(source, "id", out idItem, sourceInventory);

            if (_inventory.TryGetContainerSlotEnumerator((source, sourceInventory), out var sourceSlots, SlotFlags.POCKET))
            {
                while (sourceSlots.NextItem(out var item))
                {
                    pocketItems.Add(item);
                }
            }
        }

        _inventory.TransferEntityInventories(source, target);

        foreach (var held in _hands.EnumerateHeld(source))
        {
            _hands.TryDrop(source, held, checkActionBlocker: false);
            _hands.TryPickupAnyHand(target, held, checkActionBlocker: false);
        }

        if (!TryComp<InventoryComponent>(target, out var targetInventory))
            return;

        if (idItem != null
            && !Deleted(idItem.Value)
            && !IsInInventory(target, targetInventory, idItem.Value))
        {
            _inventory.TryEquip(target, idItem.Value, "id", true, true, inventory: targetInventory, triggerHandContact: true);
        }

        if (pocketItems.Count == 0)
            return;

        var targetPocketSlots = targetInventory.Slots
            .Where(slot => slot.SlotFlags.HasFlag(SlotFlags.POCKET))
            .ToArray();

        foreach (var item in pocketItems)
        {
            if (Deleted(item) || IsInInventory(target, targetInventory, item))
                continue;

            foreach (var slot in targetPocketSlots)
            {
                if (_inventory.TryGetSlotEntity(target, slot.Name, out _, targetInventory))
                    continue;

                if (_inventory.TryEquip(target, item, slot.Name, true, true, inventory: targetInventory, triggerHandContact: true))
                    break;
            }
        }
    }

    private bool IsInInventory(EntityUid owner, InventoryComponent inventory, EntityUid item)
    {
        var slots = _inventory.GetSlotEnumerator((owner, inventory));
        while (slots.NextItem(out var inventoryItem))
        {
            if (inventoryItem == item)
                return true;
        }

        return false;
    }

    private void TransferNutrition(EntityUid source, EntityUid target)
    {
        if (TryComp<HungerComponent>(source, out var sourceHunger)
            && TryComp<HungerComponent>(target, out var targetHunger))
        {
            _hunger.SetHunger(target, _hunger.GetHunger(sourceHunger), targetHunger);
        }

        if (TryComp<ThirstComponent>(source, out var sourceThirst)
            && TryComp<ThirstComponent>(target, out var targetThirst))
        {
            _thirst.SetThirst(target, targetThirst, sourceThirst.CurrentThirst);
        }
    }

    private void TransferMood(EntityUid source, EntityUid target)
    {
        if (!TryComp<MoodComponent>(source, out var sourceMood)
            || !TryComp<MoodComponent>(target, out var targetMood))
        {
            return;
        }

        targetMood.CurrentMoodLevel = sourceMood.CurrentMoodLevel;
        targetMood.CurrentMoodThreshold = sourceMood.CurrentMoodThreshold;
        targetMood.LastThreshold = sourceMood.LastThreshold;
        targetMood.CritThresholdBeforeModify = sourceMood.CritThresholdBeforeModify;

        targetMood.CategorisedEffects.Clear();
        foreach (var (category, effect) in sourceMood.CategorisedEffects)
        {
            targetMood.CategorisedEffects[category] = effect;
        }

        targetMood.UncategorisedEffects.Clear();
        foreach (var (effectId, effectValue) in sourceMood.UncategorisedEffects)
        {
            targetMood.UncategorisedEffects[effectId] = effectValue;
        }

        var netMood = EnsureComp<NetMoodComponent>(target);
        netMood.CurrentMoodLevel = sourceMood.CurrentMoodLevel;
        netMood.NeutralMoodThreshold = sourceMood.MoodThresholds.GetValueOrDefault(MoodThreshold.Neutral);

        _mood.UpdateAppearance(target, targetMood);
    }

    private void TransferRecordKey(EntityUid source, EntityUid target)
    {
        if (!TryComp<StationRecordKeyStorageComponent>(source, out var sourceKey)
            || sourceKey.Key == null)
        {
            return;
        }

        EnsureComp<StationRecordKeyStorageComponent>(target).Key = sourceKey.Key;
    }

    private void MoveAdminOverrides(EntityUid source, EntityUid target)
    {
        var toMove = _adminOverrides.Where(entry => entry.Target == source).ToArray();
        foreach (var entry in toMove)
        {
            _adminOverrides.Remove(entry);
            _adminOverrides.Add((target, entry.UserId));
        }
    }

    private void UpdateStationRecord(Entity<DynamicAppearanceComponent, HumanoidAppearanceComponent> ent)
    {
        if (!TryGetStationRecordKey(ent.Owner, out var key)
            || !_stationRecords.TryGetRecord<GeneralStationRecord>(key, out var record))
        {
            return;
        }

        var meta = MetaData(ent.Owner);
        record.Name = meta.EntityName;
        record.Age = ent.Comp2.Age;
        record.Species = ent.Comp2.Species;
        record.Gender = ent.Comp2.Gender;
        record.HumanoidProfile = BuildHumanoidProfile(record.HumanoidProfile, ent.Comp2, meta.EntityName);

        _stationRecords.Synchronize(key);
    }

    private bool TryGetStationRecordKey(EntityUid uid, out StationRecordKey key)
    {
        if (TryComp<StationRecordKeyStorageComponent>(uid, out var keyStorage)
            && keyStorage.Key is { } directKey)
        {
            key = directKey;
            return true;
        }

        if (_idCard.TryFindIdCard(uid, out var idCard)
            && TryComp<StationRecordKeyStorageComponent>(idCard.Owner, out keyStorage)
            && keyStorage.Key is { } cardKey)
        {
            key = cardKey;
            return true;
        }

        key = StationRecordKey.Invalid;
        return false;
    }

    private HumanoidCharacterProfile BuildHumanoidProfile(HumanoidCharacterProfile? currentProfile, HumanoidAppearanceComponent humanoid, string name)
    {
        var profile = currentProfile != null
            ? new HumanoidCharacterProfile(currentProfile)
            : HumanoidCharacterProfile.DefaultWithSpecies(humanoid.Species);

        var hairMarking = humanoid.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var hairList)
            ? hairList.FirstOrDefault()
            : null;

        var facialHairMarking = humanoid.MarkingSet.TryGetCategory(MarkingCategories.FacialHair, out var facialList)
            ? facialList.FirstOrDefault()
            : null;

        var hairId = hairMarking?.MarkingId ?? HairStyles.DefaultHairStyle;
        var hairColor = hairMarking != null && hairMarking.MarkingColors.Count > 0
            ? hairMarking.MarkingColors[0]
            : Color.Black;

        var facialId = facialHairMarking?.MarkingId ?? HairStyles.DefaultFacialHairStyle;
        var facialColor = facialHairMarking != null && facialHairMarking.MarkingColors.Count > 0
            ? facialHairMarking.MarkingColors[0]
            : Color.Black;

        var hairEffectType = MarkingEffectType.Color;
        MarkingEffect? hairEffect = null;
        if (hairMarking != null && hairMarking.MarkingEffects.Count > 0)
        {
            hairEffect = hairMarking.MarkingEffects[0].Clone();
            hairEffectType = hairEffect.Type;
        }

        var facialEffectType = MarkingEffectType.Color;
        MarkingEffect? facialEffect = null;
        if (facialHairMarking != null && facialHairMarking.MarkingEffects.Count > 0)
        {
            facialEffect = facialHairMarking.MarkingEffects[0].Clone();
            facialEffectType = facialEffect.Type;
        }

        var allMarkings = humanoid.MarkingSet.GetForwardEnumerator().Select(marking => new Marking(marking)).ToList();

        var appearance = new HumanoidCharacterAppearance(
            hairId,
            hairColor,
            facialId,
            facialColor,
            humanoid.EyeColor,
            humanoid.SkinColor,
            allMarkings,
            hairEffectType,
            hairEffect,
            facialEffectType,
            facialEffect,
            humanoid.Width,
            humanoid.Height);

        return profile
            .WithName(name)
            .WithSpecies(humanoid.Species)
            .WithSex(humanoid.Sex)
            .WithGender(humanoid.Gender)
            .WithAge(humanoid.Age)
            .WithVoice(humanoid.Voice)
            .WithBodyType(humanoid.BodyType)
            .WithCharacterAppearance(appearance);
    }

    private void SendState(EntityUid uid, HumanoidAppearanceComponent humanoid, DynamicAppearanceComponent component)
    {
        var meta = MetaData(uid);
        _ui.SetUiState(uid, DynamicAppearanceUiKey.Key,
            new DynamicAppearanceBUIState(
                new DynamicAppearanceState(
                    humanoid.MarkingSet,
                    humanoid.Species,
                    humanoid.Sex,
                    humanoid.Age,
                    humanoid.Gender,
                    humanoid.Voice,
                    humanoid.SkinColor,
                    humanoid.EyeColor,
                    humanoid.CustomBaseLayers,
                    humanoid.BodyType,
                    humanoid.Width,
                    humanoid.Height,
                    meta.EntityName
                ),
                GetNetEntity(uid),
                component.AllowedFields
            ));
    }

    private void SendPermissions(EntityUid uid, EntityUid actor)
    {
        if (!TryComp<ActorComponent>(actor, out var actorComp))
            return;

        var canOverride = _admin.IsAdmin(actorComp.PlayerSession);
        var overrideActive = canOverride && _adminOverrides.Contains((uid, actorComp.PlayerSession.UserId));

        _ui.ServerSendUiMessage(uid,
            DynamicAppearanceUiKey.Key,
            new DynamicAppearancePermissionsMessage(canOverride, overrideActive),
            actor);
    }

    /// <summary>
    /// Applies character name validation rules mirroring those in
    /// <see cref="HumanoidCharacterProfile.EnsureValid"/>.
    /// Returns the sanitised name, or an empty string if it cannot be fixed.
    /// </summary>
    private string ValidateName(string raw)
    {
        var maxLen = _cfg.GetCVar(CCVars.MaxNameLength);
        var name = raw.Trim();

        if (name.Length > maxLen)
            name = name[..maxLen];

        if (_cfg.GetCVar(CCVars.RestrictedNames))
            name = RestrictedNameRegex.Replace(name, string.Empty);

        if (_cfg.GetCVar(CCVars.ICNameCase))
            name = ICNameCaseRegex.Replace(name, m => m.Groups["word"].Value.ToUpper());

        return name.Trim();
    }

    private bool CanOpenAppearanceUi(Entity<DynamicAppearanceComponent> ent, EntityUid actor)
    {
        if (_admin.IsAdmin(actor))
            return true;

        if (ent.Comp.AllowedFields == DynamicAppearanceFields.None)
            return false;

        return actor == ent.Owner;
    }

    private bool TryGetValidSpecies(string requestedSpecies, HashSet<string>? sponsorProtos, bool ignoreRestrictions, out string species)
    {
        species = requestedSpecies;

        if (!_prototypeManager.TryIndex<SpeciesPrototype>(requestedSpecies, out var speciesProto))
            return false;

        if (ignoreRestrictions)
            return true;

        if (!speciesProto.RoundStart)
            return false;

        if (speciesProto.SponsorOnly && (sponsorProtos == null || !sponsorProtos.Contains(requestedSpecies)))
            return false;

        return true;
    }

    private bool TryGetValidVoice(string requestedVoice, Sex sex, HashSet<string>? sponsorProtos, bool ignoreRestrictions, out string voice)
    {
        voice = requestedVoice;

        if (!_prototypeManager.TryIndex<TTSVoicePrototype>(requestedVoice, out var voiceProto))
            return false;

        if (!ValidateVoiceSex(voiceProto, sex))
            return false;

        if (ignoreRestrictions)
            return true;

        if (!voiceProto.RoundStart)
            return false;

        if (voiceProto.SponsorOnly && (sponsorProtos == null || !sponsorProtos.Contains(requestedVoice)))
            return false;

        return true;
    }

    private string ResolveValidBodyType(SpeciesPrototype speciesProto, Sex sex, string? requestedBodyType)
    {
        if (!string.IsNullOrEmpty(requestedBodyType)
            && TryGetValidBodyType(speciesProto, sex, requestedBodyType, out var bodyType))
        {
            return bodyType;
        }

        foreach (var speciesBodyType in speciesProto.BodyTypes)
        {
            if (TryGetValidBodyType(speciesProto, sex, speciesBodyType, out bodyType))
                return bodyType;
        }

        return speciesProto.BodyTypes.FirstOrDefault() ?? SharedHumanoidAppearanceSystem.DefaultBodyType;
    }

    private bool TryGetValidBodyType(SpeciesPrototype speciesProto, Sex sex, string requestedBodyType, out string bodyType)
    {
        bodyType = requestedBodyType;

        if (!speciesProto.BodyTypes.Contains(requestedBodyType))
            return false;

        if (!_prototypeManager.TryIndex<BodyTypePrototype>(requestedBodyType, out var bodyTypeProto))
            return false;

        if (bodyTypeProto.SexRestrictions.Contains(sex.ToString()))
            return false;

        return true;
    }

    private bool ValidateVoiceSex(TTSVoicePrototype voiceProto, Sex sex)
    {
        return sex == Sex.Unsexed
            || voiceProto.Sex == sex
            || voiceProto.Sex == Sex.Unsexed;
    }

    private MarkingSet ReapplyMarkingsForSpecies(
        MarkingSet source,
        SpeciesPrototype speciesProto,
        string species,
        Sex sex,
        Color skinColor,
        Color eyeColor)
    {
        var rebuilt = new MarkingSet(speciesProto.MarkingPoints, _markingManager, _prototypeManager);
        var forcedMarkings = new List<(Marking Source, MarkingPrototype Prototype)>();

        foreach (var (_, markings) in source.Markings)
        {
            foreach (var marking in markings)
            {
                if (!_prototypeManager.TryIndex<MarkingPrototype>(marking.MarkingId, out var prototype))
                    continue;

                if (prototype.ForcedColoring)
                {
                    forcedMarkings.Add((marking, prototype));
                    continue;
                }

                rebuilt.AddBack(prototype.MarkingCategory, RebuildMarking(marking, prototype, species, skinColor, eyeColor, rebuilt));
            }
        }

        foreach (var (sourceMarking, prototype) in forcedMarkings)
        {
            rebuilt.AddBack(prototype.MarkingCategory, RebuildMarking(sourceMarking, prototype, species, skinColor, eyeColor, rebuilt, preserveColors: false));
        }

        rebuilt.EnsureValid(_markingManager);
        rebuilt.EnsureSpecies(species, skinColor, _markingManager, _prototypeManager);
        rebuilt.EnsureSexes(sex, _markingManager);
        return rebuilt;
    }

    private Marking RebuildMarking(
        Marking source,
        MarkingPrototype prototype,
        string species,
        Color skinColor,
        Color eyeColor,
        MarkingSet rebuiltSet,
        bool preserveColors = true)
    {
        var rebuilt = prototype.AsMarking();
        rebuilt.Forced = source.Forced;
        rebuilt.Visible = source.Visible;

        var matchesSkin = _markingManager.MustMatchSkin(species, prototype.BodyPart, out _, _prototypeManager);
        var colors = preserveColors
            ? source.MarkingColors
            : MarkingColoring.GetMarkingLayerColors(prototype, skinColor, eyeColor, rebuiltSet);

        for (var i = 0; i < rebuilt.MarkingColors.Count && i < colors.Count; i++)
        {
            rebuilt.SetColor(i, NormalizeMarkingColor(colors[i], matchesSkin));
        }

        for (var i = 0; i < rebuilt.MarkingEffects.Count && i < source.MarkingEffects.Count; i++)
        {
            rebuilt.SetMarkingEffect(i, NormalizeMarkingEffect(source.MarkingEffects[i], matchesSkin));
        }

        return rebuilt;
    }

    private Color NormalizeMarkingColor(Color color, bool matchesSkin)
    {
        return matchesSkin ? color : color.WithAlpha(1f);
    }

    private MarkingEffect NormalizeMarkingEffect(MarkingEffect effect, bool matchesSkin)
    {
        var clone = effect.Clone();
        if (matchesSkin)
            return clone;

        foreach (var (key, color) in clone.Colors.ToArray())
        {
            clone.Colors[key] = color.WithAlpha(1f);
        }

        return clone;
    }

    /// <summary>
    /// Deep-clones a client-supplied marking payload, repairs malformed entries,
    /// and strips anything the current species / sex / sponsor rules should not allow.
    /// </summary>
    private MarkingSet SanitizeIncomingMarkings(
        MarkingSet originalSet,
        string species,
        Sex sex,
        Color skinColor,
        HashSet<string>? sponsorProtos,
        bool ignoreRestrictions)
    {
        var sanitized = new MarkingSet(originalSet);
        sanitized.EnsureValid(_markingManager);
        sanitized.EnsureSpecies(species, skinColor, _markingManager, _prototypeManager);
        sanitized.EnsureSexes(sex, _markingManager);

        return ignoreRestrictions
            ? sanitized
            : FilterSponsorMarkings(sanitized, sponsorProtos);
    }

    /// <summary>
    /// Removes sponsor-only markings that the player does not own.
    /// When <paramref name="sponsorProtos"/> is <c>null</c> (no sponsor data), all sponsor-only
    /// markings are stripped.
    /// </summary>
    private MarkingSet FilterSponsorMarkings(MarkingSet original, HashSet<string>? sponsorProtos)
    {
        var filtered = new MarkingSet();

        foreach (var (category, markings) in original.Markings)
        {
            foreach (var marking in markings)
            {
                if (_prototypeManager.TryIndex<MarkingPrototype>(marking.MarkingId, out var proto)
                    && proto.SponsorOnly
                    && (sponsorProtos == null || !sponsorProtos.Contains(marking.MarkingId)))
                {
                    continue; // Strip the sponsor-only marking the player doesn't have.
                }

                filtered.AddBack(category, marking);
            }
        }

        return filtered;
    }

    private bool HasAdminOverride(EntityUid target, EntityUid actor)
    {
        if (!TryComp<ActorComponent>(actor, out var actorComp) || !_admin.IsAdmin(actorComp.PlayerSession))
            return false;

        return _adminOverrides.Contains((target, actorComp.PlayerSession.UserId));
    }

    private void SetAdminOverride(EntityUid target, NetUserId userId, bool enabled)
    {
        var key = (target, userId);

        if (enabled)
            _adminOverrides.Add(key);
        else
            _adminOverrides.Remove(key);
    }

    private void ClearAdminOverride(EntityUid target, EntityUid actor)
    {
        if (!TryComp<ActorComponent>(actor, out var actorComp))
            return;

        _adminOverrides.Remove((target, actorComp.PlayerSession.UserId));
    }

    #endregion
}
