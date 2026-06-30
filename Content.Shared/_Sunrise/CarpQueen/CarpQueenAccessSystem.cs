namespace Content.Shared._Sunrise.CarpQueen;

/// <summary>
/// Маркерная система, дающая серверным системам доступ к изменению компонентов королевы карпов.
/// Серверные системы, которым нужна запись, должны наследоваться от нее.
/// </summary>
public abstract class CarpQueenAccessSystem : EntitySystem
{
}

