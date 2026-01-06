namespace Content.Packaging;

public sealed class SharedPackaging
{
    /// <summary>
    /// Resources ignored for both client and server packaging.
    /// </summary>
    public static readonly IReadOnlySet<string> AdditionalIgnoredResources = new HashSet<string>
    {
        // MapRenderer outputs into Resources. Avoid these getting included in packaging.
        "MapImages",
    };

    /// <summary>
    /// Resources ignored only for client packaging (but included in server).
    /// These resources are loaded dynamically from server to client.
    /// </summary>
    public static readonly IReadOnlySet<string> ClientOnlyIgnoredResources = new HashSet<string>
    {
        // All _Sunrise resources are loaded dynamically from server
        "_Sunrise",
    };
}
