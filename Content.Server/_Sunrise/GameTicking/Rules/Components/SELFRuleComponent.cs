using Content.Server._Sunrise.GameTicking.Rules;

namespace Content.Server._Sunrise.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="SELFRuleSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(SELFRuleSystem))]
public sealed partial class SELFRuleComponent : Component;
