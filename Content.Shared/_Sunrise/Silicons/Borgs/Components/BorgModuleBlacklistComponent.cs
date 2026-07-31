using Content.Shared.Whitelist;

namespace Content.Shared._Sunrise.Silicons.Borgs.Components;

/// <summary>
/// Запрещает устанавливать в борга модули из указанного blacklist.
/// </summary>
[RegisterComponent]
public sealed partial class BorgModuleBlacklistComponent : Component
{
    /// <summary>
    /// Модули, установка которых запрещена.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Blacklist = new();
}
