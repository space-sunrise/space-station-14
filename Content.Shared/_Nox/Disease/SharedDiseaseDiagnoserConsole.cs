// Developed by Nox for the Sunrise Station project.
// Author: KloopRe

using Robust.Shared.Serialization;

namespace Content.Shared._Nox.Disease;

[Serializable, NetSerializable]
public enum DiseaseDiagnoserConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DiseaseDiagnoserConsoleBoundUserInterfaceState : BoundUserInterfaceState
{
    public List<DiseaseStrainRecord> Strains = new();
    public int Points { get; }
    public bool DiagnoserConnected { get; }
    public bool DataServerConnected { get; }
    public bool SolutionAnalyzerConnected { get; }
    public bool DiagnoserInRange { get; }
    public bool DataServerInRange { get; }
    public bool SolutionAnalyzerInRange { get; }
    public DiseaseDiagnoserConsoleBoundUserInterfaceState(
        List<DiseaseStrainRecord>? strains = null,
        int points = 0,
        bool diagnoserConnected = false,
        bool dataServerConnected = false,
        bool solutionAnalyzerConnected = false,
        bool diagnoserInRange = false,
        bool dataServerInRange = false,
        bool solutionAnalyzerInRange = false)
    {
        Strains = strains ?? new List<DiseaseStrainRecord>();
        Points = points;
        DiagnoserConnected = diagnoserConnected;
        DataServerConnected = dataServerConnected;
        SolutionAnalyzerConnected = solutionAnalyzerConnected;
        DiagnoserInRange = diagnoserInRange;
        DataServerInRange = dataServerInRange;
        SolutionAnalyzerInRange = solutionAnalyzerInRange;
    }
}


[Serializable, NetSerializable]
public readonly struct DiseaseStrainRecord : IEquatable<DiseaseStrainRecord>
{
    public readonly string Strain;
    public readonly string Time;

    public DiseaseStrainRecord(string strain, string time)
    {
        Strain = strain;
        Time = time;
    }

    public bool Equals(DiseaseStrainRecord other) =>
        Strain == other.Strain && Time == other.Time;

    public override bool Equals(object? obj) =>
        obj is DiseaseStrainRecord other && Equals(other);

    public override int GetHashCode() =>
        HashCode.Combine(Strain, Time);
}


[Serializable, NetSerializable]
public enum UiButton : byte
{
    GenerateDisease,
    PrintReport,
    ScanDisease,
    StartAnalys,
    DeleteData
}

[Serializable, NetSerializable]
public sealed class UiButtonPressedMessage : BoundUserInterfaceMessage
{
    public readonly UiButton Button;
    public string? Strain { get; } = null;

    public UiButtonPressedMessage(UiButton button, string? strain)
    {
        Button = button;
        Strain = strain;
    }
}
