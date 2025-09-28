using Robust.Shared.GameStates;

namespace Content.Shared.ResearchRig.Components;

/// <summary>
/// Component that modifies armor coefficients based on proximity to research equipment.
/// When near research equipment, provides enhanced protection similar to RD hardsuit.
/// When away from equipment, reverts to basic EVA suit protection (except speed).
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ResearchRigProximityArmorComponent : Component
{
    /// <summary>
    /// Range in tiles to check for research equipment
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ProximityRange = 10f;

    /// <summary>
    /// Whether the wearer is currently near research equipment
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public bool IsNearResearchEquipment = false;

    /// <summary>
    /// Enhanced armor coefficients when near research equipment (RD suit stats)
    /// </summary>
    [DataField]
    public Dictionary<string, float> EnhancedArmorCoefficients = new()
    {
        {"Blunt", 0.6f},
        {"Slash", 0.8f},
        {"Piercing", 0.9f},
        {"Heat", 0.3f},
        {"Radiation", 0.2f},
        {"Caustic", 0.2f}
    };

    /// <summary>
    /// Base armor coefficients when away from research equipment (EVA suit stats)
    /// </summary>
    [DataField]
    public Dictionary<string, float> BaseArmorCoefficients = new()
    {
        {"Blunt", 0.9f},
        {"Slash", 0.9f},
        {"Piercing", 0.95f},
        {"Heat", 0.9f},
        {"Radiation", 0.8f},
        {"Caustic", 0.8f}
    };

    /// <summary>
    /// Enhanced explosion resistance when near research equipment
    /// </summary>
    [DataField]
    public float EnhancedExplosionCoefficient = 0.3f;

    /// <summary>
    /// Base explosion resistance when away from research equipment
    /// </summary>
    [DataField]
    public float BaseExplosionCoefficient = 0.7f;

    /// <summary>
    /// Enhanced pressure protection when near research equipment
    /// </summary>
    [DataField]
    public float EnhancedHighPressureMultiplier = 0.02f;

    /// <summary>
    /// Base pressure protection when away from research equipment
    /// </summary>
    [DataField]
    public float BaseHighPressureMultiplier = 0.6f;

    /// <summary>
    /// List of prototype IDs for research equipment to check proximity to
    /// </summary>
    [DataField]
    public HashSet<string> ResearchEquipmentPrototypes = new()
    {
        // Anomaly research equipment
        "AnomalyScanner",
        "AnomalyLocator",
        "AnomalyLocatorWide",
        "AnomalySynchronizer",
        "AnomalyVessel",
        "AnomalyVesselExperimental",
        "ArtifactAnalyzer",
        "ArtifactCrusher",
        "AnalysisComputer",
        "TechDiskComputer",

        // Research machines
        "ResearchAndDevelopmentServer",
        "ResearchAndDevelopmentPointSource",
        "ExosuitFabricator",
        "Protolathe",
        "CircuitImprinter",
        "UniformPrinter",
        "OreProcessor",
        "Biofabricator",
        "SecurityTechFab",
        "MedicalTechFab",
        "AutolatheHyperConvection",
        "Autolathe",

        // Anomaly types
        "AnomalyPyroclastic",
        "AnomalyCryogenic",
        "AnomalyElectrochemical",
        "AnomalyGravitational",
        "AnomalyFlora",
        "AnomalyBluespace",
        "AnomalyRock",
        "AnomalyLiquid",
        "AnomalyFlesh",
        "AnomalyIce",
        "AnomalyShadow",
        "AnomalyTech"
    };

    /// <summary>
    /// Time since last proximity check
    /// </summary>
    [DataField]
    public float TimeSinceLastCheck = 0f;

    /// <summary>
    /// How often to check for proximity (in seconds)
    /// </summary>
    [DataField]
    public float CheckInterval = 1f;
}
