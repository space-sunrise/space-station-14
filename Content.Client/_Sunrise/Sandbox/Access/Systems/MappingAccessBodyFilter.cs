namespace Content.Client._Sunrise.Sandbox.Access.Systems;

/// <summary>
/// Filters mapping access labels by the body types they should be drawn for.
/// </summary>
public enum MappingAccessBodyFilter : byte
{
    /// <summary>
    /// Draws labels for both static and dynamic bodies.
    /// </summary>
    Both,
    /// <summary>
    /// Draws labels only for static bodies.
    /// </summary>
    Static,
    /// <summary>
    /// Draws labels only for dynamic bodies.
    /// </summary>
    Dynamic,
}
