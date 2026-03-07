using System.Text.RegularExpressions;
using Content.Server.Administration.Managers;
using Content.Shared._Sunrise.DynamicAppearance;
using Content.Shared.CCVar;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Verbs;
using Content.Sunrise.Interfaces.Shared;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
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
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAdminManager _admin = default!;

    // Sunrise-Sponsors
    private ISharedSponsorsManager? _sponsorsManager;

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
        SubscribeLocalEvent<DynamicAppearanceComponent, GetVerbsEvent<AlternativeVerb>>(OnVerbsRequest);

        Subs.BuiEvents<DynamicAppearanceComponent>(DynamicAppearanceUiKey.Key, subs =>
        {
            subs.Event<DynamicAppearanceSaveMessage>(OnSaveMessage);
        });
    }

    #region Lifecycle

    private void OnComponentStartup(EntityUid uid, DynamicAppearanceComponent component, ComponentStartup args)
    {
        if (TryComp<UserInterfaceComponent>(uid, out var ui))
        {
            _ui.SetUi(uid, DynamicAppearanceUiKey.Key, new InterfaceData("Content.Client._Sunrise.DynamicAppearance.DynamicAppearanceBoundUserInterface"));
        }
    }

    private void OnComponentRemove(EntityUid uid, DynamicAppearanceComponent component, ComponentRemove args)
    {
        // Close the editor UI if open when the component is removed.
        _ui.CloseUi(uid, DynamicAppearanceUiKey.Key);
    }

    #endregion

    #region Verb

    private void OnVerbsRequest(EntityUid uid, DynamicAppearanceComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (args.User != uid && !_admin.IsAdmin(args.User))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("dynamic-appearance-verb"),
            Icon = new SpriteSpecifier.Rsi(new("/Textures/Mobs/Species/Slime/parts.rsi"), "head_m"),
            Act = () =>
            {
                _ui.OpenUi(uid, DynamicAppearanceUiKey.Key, actor.PlayerSession);
                SendState(uid, humanoid, component);
            },
            Priority = -2,
        });
    }

    #endregion

    #region BUI message handlers

    private void OnSaveMessage(Entity<DynamicAppearanceComponent> ent, ref DynamicAppearanceSaveMessage args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
            return;

        var allowed = ent.Comp.AllowedFields;

        // ── Resolve sponsor markings for this session ──
        HashSet<string>? sponsorProtos = null;
        if (_sponsorsManager != null && TryComp<ActorComponent>(ent, out var actor))
        {
            if (_sponsorsManager.TryGetPrototypes(actor.PlayerSession.UserId, out var sp))
                sponsorProtos = [.. sp];
        }

        // ── Markings ──
        // Always run species + sponsor filtering regardless of whitelist,
        // so malicious clients cannot inject illegal markings.
        var filteredMarkings = FilterMarkings(args.State.MarkingSet, humanoid.Species);
        filteredMarkings = FilterSponsorMarkings(filteredMarkings, sponsorProtos);

        // Only apply to the humanoid component for the categories the component allows.
        var newSet = new MarkingSet();
        foreach (var (category, markings) in filteredMarkings.Markings)
        {
            var isHairCategory = category is MarkingCategories.Hair or MarkingCategories.FacialHair;

            if (isHairCategory && !allowed.HasFlag(DynamicAppearanceFields.Hair))
                continue;
            if (!isHairCategory && !allowed.HasFlag(DynamicAppearanceFields.Markings))
                continue;

            foreach (var marking in markings)
                newSet.AddBack(category, marking);
        }
        humanoid.MarkingSet = newSet;

        // ── Sex ──
        if (allowed.HasFlag(DynamicAppearanceFields.Sex))
        {
            _humanoid.SetSex(ent, args.State.Sex, humanoid: humanoid);

            // TTS voice is gated behind Sex: the voice picker re-filters on each sex change.
            if (!string.IsNullOrEmpty(args.State.Voice))
                _humanoid.SetTTSVoice(ent, args.State.Voice, humanoid);
        }

        // ── Skin color ──
        if (allowed.HasFlag(DynamicAppearanceFields.SkinColor))
            _humanoid.SetSkinColor(ent, args.State.SkinColor, humanoid: humanoid);

        // ── Eye color ──
        if (allowed.HasFlag(DynamicAppearanceFields.EyeColor))
            humanoid.EyeColor = args.State.EyeColor;

        // ── Gender / Pronouns ──
        if (allowed.HasFlag(DynamicAppearanceFields.Pronouns))
            _humanoid.SetGender((ent.Owner, humanoid), args.State.Gender);

        // ── Size + Age (not individually gated — kept as before) ──
        if (_prototypeManager.TryIndex<SpeciesPrototype>(humanoid.Species, out var speciesProto))
        {
            humanoid.Width = Math.Clamp(args.State.Width, speciesProto.MinWidth, speciesProto.MaxWidth);
            humanoid.Height = Math.Clamp(args.State.Height, speciesProto.MinHeight, speciesProto.MaxHeight);
            humanoid.Age = Math.Clamp(args.State.Age, speciesProto.MinAge, speciesProto.MaxAge);
        }

        // ── Custom base layers ──
        humanoid.CustomBaseLayers.Clear();
        foreach (var (layer, info) in args.State.CustomBaseLayers)
            humanoid.CustomBaseLayers[layer] = info;

        // ── Name ──
        if (allowed.HasFlag(DynamicAppearanceFields.Name)
            && !string.IsNullOrWhiteSpace(args.State.Name))
        {
            var name = ValidateName(args.State.Name);
            if (!string.IsNullOrEmpty(name))
                _meta.SetEntityName(ent, name);
        }

        Dirty(ent, humanoid);
        SendState(ent, humanoid, ent.Comp);
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

    #endregion
}
