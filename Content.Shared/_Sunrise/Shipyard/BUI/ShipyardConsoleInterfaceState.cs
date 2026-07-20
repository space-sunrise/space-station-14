using Robust.Shared.Serialization;
using Robust.Shared.Maths;

namespace Content.Shared._Sunrise.Shipyard.BUI;

[Serializable, NetSerializable]
public sealed class ShipyardConsoleInterfaceState : BoundUserInterfaceState
{
    public readonly string AccountName;
    public readonly Color AccountColor;
    public readonly int Balance;
    public readonly string? CurrentShuttleName;
    public readonly int CurrentShuttlePrice;
    public readonly int CurrentShuttleSellValue;
    public readonly float SellRate;
    public readonly bool TransactionPending;
    public readonly List<ShipyardVesselData> Vessels;

    public ShipyardConsoleInterfaceState(
        string accountName,
        Color accountColor,
        int balance,
        string? currentShuttleName,
        int currentShuttlePrice,
        int currentShuttleSellValue,
        float sellRate,
        bool transactionPending,
        List<ShipyardVesselData> vessels)
    {
        AccountName = accountName;
        AccountColor = accountColor;
        Balance = balance;
        CurrentShuttleName = currentShuttleName;
        CurrentShuttlePrice = currentShuttlePrice;
        CurrentShuttleSellValue = currentShuttleSellValue;
        SellRate = sellRate;
        TransactionPending = transactionPending;
        Vessels = vessels;
    }
}

[Serializable, NetSerializable]
public readonly record struct ShipyardVesselData(string Id, string Name, string Description, int Price);
