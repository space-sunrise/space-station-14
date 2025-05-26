using Content.Server._Sunrise.BloodCult;
using Content.Shared._Sunrise.Ghost;
using Content.Shared.Zombies;

namespace Content.Server._Sunrise.Ghost;

/// <summary>
/// Система, динамически выдающая <see cref="GhostPanelAntagonistMarkerComponent"/>
/// Некоторые антагонисты должны начать отображаться только в определенный момент, чтобы исключить лишнюю мета-информацию для игроков
/// </summary>
public sealed class GhostPanelAntagonistMarkerPinSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        #region Zombie

        SubscribeLocalEvent<MetaDataComponent, EntityZombifiedEvent>(OnZombify);

        #endregion

        #region Blood cult

        SubscribeLocalEvent<PentagramComponent, ComponentInit>(OnCultistAscent);
        SubscribeLocalEvent<PentagramComponent, ComponentRemove>(OnCultistDescent);

        #endregion
    }

    #region Zombie

    private void OnZombify(Entity<MetaDataComponent> ent, ref EntityZombifiedEvent args)
    {
        var marker = EnsureComp<GhostPanelAntagonistMarkerComponent>(ent);

        marker.Name = "ghost-panel-antagonist-zombie-name";
        marker.Description = "ghost-panel-antagonist-zombie-description";
        marker.Priority = 50;

        Dirty(ent.Owner, marker);
    }

    #endregion

    #region Blood cult

    private void OnCultistAscent(Entity<PentagramComponent> ent, ref ComponentInit args)
    {
        var marker = EnsureComp<GhostPanelAntagonistMarkerComponent>(ent);

        marker.Name = "ghost-panel-antagonist-cult-name";
        marker.Description = "ghost-panel-antagonist-cult-description";
        marker.Priority = 30;

        Dirty(ent.Owner, marker);
    }

    private void OnCultistDescent(Entity<PentagramComponent> ent, ref ComponentRemove args)
    {
        RemComp<GhostPanelAntagonistMarkerComponent>(ent);
    }

    #endregion
}
