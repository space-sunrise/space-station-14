using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared._Sunrise.Laws.Systems;

namespace Content.Shared._Sunrise.Laws.Components;

/// <summary>
///     Stores the current corporate law configuration for a station.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedStationCorporateLawSystem)), AutoGenerateComponentState]
public sealed partial class StationCorporateLawComponent : Component
{
    /// <summary>
    ///     General provisions and rights.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<CorporateLawPrototype>> Provisions = new();

    /// <summary>
    ///     Sentence-modifying factors.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<CorporateLawPrototype>> Circumstances = new();

    /// <summary>
    ///     Categorized legal articles (1xx-6xx).
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<CorporateLawSectionPrototype>> Articles = new();

    /// <summary>
    ///     Threshold at which the sentence becomes permanent/life.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int PermanentSentenceThreshold = 50;

    /// <summary>
    ///     The ID of the prototype this lawset was initialized from.
    ///     Useful for tracking changes or resetting.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? LawsetPrototype;
}
