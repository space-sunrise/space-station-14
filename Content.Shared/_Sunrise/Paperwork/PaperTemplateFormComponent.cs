using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Paperwork;

/// <summary>
/// Маркер, который разрешает использование интерактивных кнопок на бумаге
/// Используется в принтере
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PaperTemplateFormComponent : Component;
