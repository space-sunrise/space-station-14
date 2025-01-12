// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt
using Content.Shared._Sunrise.GhostTheme;
using Content.Shared.Ghost;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Server.GameObjects;
using Content.Sunrise.Interfaces.Shared;

namespace Content.Server._Sunrise.GhostTheme;

public sealed class GhostThemeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    private ISharedSponsorsManager? _sponsorsManager;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<GhostComponent, GhostThemeActionEvent>(OnGhostThemeChange);
        SubscribeLocalEvent<GhostComponent, GhostThemePrototypeSelectedMessage>(OnGhostThemeSelected);

        IoCManager.Instance!.TryResolveType(out _sponsorsManager); // Sunrise-Sponsors
    }

    private void TryOpenUi(EntityUid uid, EntityUid user, GhostComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!TryComp(user, out ActorComponent? actor))
            return;

        _uiSystem.TryToggleUi(uid, GhostThemeUiKey.Key, actor.PlayerSession);
        UpdateUi(uid, actor.PlayerSession, component);
    }

    private void OnGhostThemeChange(EntityUid uid, GhostComponent observerComponent, GhostThemeActionEvent args)
    {
        TryOpenUi(uid, args.Performer, observerComponent);
        args.Handled = true;
    }

    private void OnGhostThemeSelected(Entity<GhostComponent> ent, ref GhostThemePrototypeSelectedMessage msg)
    {
        if (!TryComp(msg.Actor, out ActorComponent? actorComp))
            return;

        List<string> ghostThemes = [];
        if (_sponsorsManager != null && _sponsorsManager.TryGetGhostThemes(actorComp.PlayerSession.UserId, out var sponsorGhostThemes))
        {
            ghostThemes.AddRange(sponsorGhostThemes);
        }

        if (!_prototypeManager.TryIndex<GhostThemePrototype>(msg.SelectedGhostTheme, out var ghostThemePrototype))
            return;

        if (!ghostThemes.Contains(ghostThemePrototype.ID) && ghostThemePrototype.SponsorOnly)
            return;

        _sponsorsManager?.SetCachedGhostTheme(actorComp.PlayerSession.UserId, ghostThemePrototype.ID);
        var ghostTheme = EnsureComp<GhostThemeComponent>(ent);
        ghostTheme.GhostTheme = msg.SelectedGhostTheme;
        Dirty(ent, ghostTheme);
    }

    private void UpdateUi(EntityUid uid, ICommonSession session, GhostComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        List<string> ghostThemes = [];
        if (_sponsorsManager != null && _sponsorsManager.TryGetGhostThemes(session.UserId, out var sponsorGhostThemes))
        {
            ghostThemes.AddRange(sponsorGhostThemes);
        }

        var ghostThemesPrototypes = _prototypeManager.EnumeratePrototypes<GhostThemePrototype>();

        var availableGhostThemes = new List<string>();

        foreach (var ghostThemePrototype in ghostThemesPrototypes)
        {
            if (!ghostThemes.Contains(ghostThemePrototype.ID) && ghostThemePrototype.SponsorOnly)
                continue;

            availableGhostThemes.Add(ghostThemePrototype.ID);
        }

        var state = new GhostThemeBoundUserInterfaceState(availableGhostThemes);

        _uiSystem.SetUiState(uid, GhostThemeUiKey.Key, state);
    }

    private void OnPlayerAttached(EntityUid uid, GhostComponent component, PlayerAttachedEvent args)
    {
        if (_sponsorsManager == null ||
            !_sponsorsManager.TryGetCachedGhostTheme(args.Player.UserId, out var ghostTheme))
            return;

        if (!_prototypeManager.TryIndex<GhostThemePrototype>(ghostTheme, out var ghostThemePrototype))
            return;

        EnsureComp<GhostThemeComponent>(uid).GhostTheme = ghostTheme;
    }
}
