// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Content.Shared.Body.Prototypes;
using Content.Shared._Nox.Disease.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Nox.Disease;

[Serializable, NetSerializable]
public sealed class DiseaseEvolutionConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public int MutationPoints { get; }
    public int MultiPriceDeleteSymptom { get; }
    public bool DataServerConnected { get; }
    public bool SolutionAnalyzerConnected { get; }
    public bool DataServerInRange { get; }
    public bool SolutionAnalyzerInRange { get; }
    public bool HasDisease { get; }
    public List<ProtoId<DiseaseSymptomPrototype>> ActiveSymptoms = new();
    public List<ProtoId<BodyPrototype>> BodyWhitelist = new();

    // Статистика вируса
    public bool IsSentientDisease { get; }
    public float MaxThreshold { get; }
    public float Infectivity { get; }
    public int InfectedCount { get; }
    public int PointsPerSecond { get; }

    public DiseaseEvolutionConsoleBoundUserInterfaceState(
        int mutationPoints,
        int multyPriceDeleteSymptom,
        bool dataServerConnected,
        bool solutionAnalyzerConnected,
        bool dataServerInRange,
        bool solutionAnalyzerInRange,
        bool hasDisease = false,
        List<ProtoId<DiseaseSymptomPrototype>>? activeSymptoms = null,
        List<ProtoId<BodyPrototype>>? bodyWhitelist = null,
        float maxThreshold = 100f,
        float infectivity = 0f,
        int infectedCount = 0,
        int pointsPerSecond = 0,
        bool isSentientDisease = false)
    {
        MutationPoints = mutationPoints;
        MultiPriceDeleteSymptom = multyPriceDeleteSymptom;
        DataServerConnected = dataServerConnected;
        SolutionAnalyzerConnected = solutionAnalyzerConnected;
        DataServerInRange = dataServerInRange;
        SolutionAnalyzerInRange = solutionAnalyzerInRange;
        ActiveSymptoms = activeSymptoms ?? new List<ProtoId<DiseaseSymptomPrototype>>();
        BodyWhitelist = bodyWhitelist ?? new List<ProtoId<BodyPrototype>>();
        IsSentientDisease = isSentientDisease;
        HasDisease = hasDisease;
        MaxThreshold = maxThreshold;
        Infectivity = infectivity;
        InfectedCount = infectedCount;
        PointsPerSecond = pointsPerSecond;
        IsSentientDisease = isSentientDisease;
    }
}


[Serializable, NetSerializable]
public sealed class EvolutionConsoleUiButtonPressedMessage : BoundUserInterfaceMessage
{
    public readonly EvolutionConsoleUiButton Button;
    public string? Symptom { get; } = null;
    public string? Body { get; } = null;

    public EvolutionConsoleUiButtonPressedMessage(
        EvolutionConsoleUiButton button,
        string? symptom = null,
        string? body = null
        )
    {
        Button = button;
        Symptom = symptom;
        Body = body;
    }
}


[Serializable, NetSerializable]
public enum EvolutionConsoleUiButton : byte
{
    EvolutionSymptom,
    EvolutionBody,
    DeleteSymptom,
    DeleteBody
}

[Serializable, NetSerializable]
public enum DiseaseEvolutionConsoleUiKey : byte
{
    Key
}