using Robust.Shared.Serialization.Manager.Attributes;

#pragma warning disable IDE0130 // Пространство имён соответствует расширяемому компоненту.
namespace Content.Server.EnergyDome;

public sealed partial class EnergyDomeGeneratorComponent
{
    /// <summary>
    /// Разрешает переносить активный купол на нового владельца генератора вместо его отключения.
    /// </summary>
    [DataField]
    public bool TransferDomeOnParentChange;
}
