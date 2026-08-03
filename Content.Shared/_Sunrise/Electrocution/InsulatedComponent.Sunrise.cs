#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Shared.Electrocution;

public sealed partial class InsulatedComponent
{
    /// <summary>
    /// Prevents the wearer from operating guns without a large trigger guard.
    /// </summary>
    [DataField]
    public bool PreventOpperatinGuns;
}
