using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Administration.Managers;
using Content.Server.DoAfter;
using Content.Shared._Sunrise.DynamicAppearance;
using Content.Shared._Sunrise.TTS;
using Content.Shared.CCVar;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Verbs;
using Content.Sunrise.Interfaces.Shared;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAdminManager _admin = default!;

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
        if (component.AllowedFields == DynamicAppearanceFields.None)
            return;

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

        var ignoreRestrictions = HasAdminOverride(ent.Owner, actor);
        var allowed = ignoreRestrictions ? DynamicAppearanceFields.All : ent.Comp1.AllowedFields;

        // ── Resolve sponsor markings for this session ──
        HashSet<string>? sponsorProtos = null;
        if (_sponsorsManager != null && TryComp<ActorComponent>(actor, out var actorComp))
        {
            if (_sponsorsManager.TryGetPrototypes(actorComp.PlayerSession.UserId, out var sp))
                sponsorProtos = [.. sp];
        }

        var targetSpecies = humanoid.Species;
        if (allowed.HasFlag(DynamicAppearanceFields.Species)
            && TryGetValidSpecies(state.Species, sponsorProtos, ignoreRestrictions, out var speciesId))
        {
            targetSpecies = speciesId;
        }

        var speciesChanged = targetSpecies != humanoid.Species;
        if (speciesChanged)
            _humanoid.SetSpecies(ent, targetSpecies, false, humanoid);

        if (!_prototypeManager.TryIndex<SpeciesPrototype>(humanoid.Species, out var speciesProto))
            return;

        if (speciesChanged)
            _humanoid.SetBodyType(ent, humanoid.BodyType, false, humanoid);

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

        var sexChanged = targetSex != humanoid.Sex;
        if (sexChanged)
            _humanoid.SetSex(ent, targetSex, false, humanoid);

        // ── Markings ──
        // Always run species + sponsor filtering regardless of whitelist,
        // so malicious clients cannot inject illegal markings.
        var filteredMarkings = FilterMarkings(state.MarkingSet, humanoid.Species);
        if (!ignoreRestrictions)
            filteredMarkings = FilterSponsorMarkings(filteredMarkings, sponsorProtos);

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

        foreach (var (category, markings) in filteredMarkings.Markings)
        {
            var isHairCategory = category is MarkingCategories.Hair or MarkingCategories.FacialHair;

            if (isHairCategory && !allowed.HasFlag(DynamicAppearanceFields.Hair))
                continue;
            if (!isHairCategory && !allowed.HasFlag(DynamicAppearanceFields.Markings))
                continue;

            mergedMarkings.AddRange(markings.Select(marking => new Marking(marking)));
        }

        var newSet = new MarkingSet(mergedMarkings, speciesProto.MarkingPoints);
        newSet.EnsureSexes(humanoid.Sex);
        humanoid.MarkingSet = newSet;

        // ── Sex ──
        if (allowed.HasFlag(DynamicAppearanceFields.Sex) || speciesChanged)
        {
            // TTS voice is gated behind Sex: the voice picker re-filters on each sex change.
            var targetVoice = humanoid.Voice;
            if (!string.IsNullOrEmpty(state.Voice)
                && TryGetValidVoice(state.Voice, targetSex, sponsorProtos, ignoreRestrictions, out var voiceId))
            {
                targetVoice = voiceId;
            }
            else if (sexChanged || speciesChanged)
            {
                targetVoice = SharedHumanoidAppearanceSystem.DefaultSexVoice[targetSex];
            }

            if (!string.IsNullOrEmpty(targetVoice) && targetVoice != humanoid.Voice)
                _humanoid.SetTTSVoice(ent, targetVoice, humanoid);
        }

        // ── Skin color ──
        if (allowed.HasFlag(DynamicAppearanceFields.SkinColor))
            _humanoid.SetSkinColor(ent, state.SkinColor, humanoid: humanoid);
        else if (speciesChanged)
            _humanoid.SetSkinColor(ent, humanoid.SkinColor, false, true, humanoid);

        // ── Eye color ──
        if (allowed.HasFlag(DynamicAppearanceFields.EyeColor))
            humanoid.EyeColor = state.EyeColor;

        // Preview applies species defaults through `LoadProfile()`, so mirror that here.
        // This ensures species-specific default markings are actually present in-game
        // after a species swap, even if the client draft itself doesn't contain them.
        humanoid.MarkingSet.EnsureDefault(humanoid.SkinColor, humanoid.EyeColor);

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

        Dirty(ent, humanoid);
        SendState(ent, humanoid, ent.Comp1);
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

        if (ignoreRestrictions)
            return true;

        if (!voiceProto.RoundStart)
            return false;

        if (voiceProto.SponsorOnly && (sponsorProtos == null || !sponsorProtos.Contains(requestedVoice)))
            return false;

        return sex == Sex.Unsexed
            || voiceProto.Sex == sex
            || voiceProto.Sex == Sex.Unsexed;
    }

    /// <summary>
    /// Filters a marking set to only keep markings allowed for the given species.
    /// </summary>
    private MarkingSet FilterMarkings(MarkingSet originalSet, string species)
    {
        var filtered = new MarkingSet();

        foreach (var (category, markings) in originalSet.Markings)
        {
            foreach (var marking in markings)
            {
                if (!_prototypeManager.TryIndex<MarkingPrototype>(marking.MarkingId, out var proto))
                    continue;

                if (proto.SpeciesRestrictions == null
                    || proto.SpeciesRestrictions.Count == 0
                    || proto.SpeciesRestrictions.Contains(species))
                {
                    filtered.AddBack(category, marking);
                }
            }
        }

        return filtered;
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
