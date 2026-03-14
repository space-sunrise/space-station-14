namespace Content.Shared._Sunrise.NetTextures;

/// <summary>
/// Shared protocol limits for the NetTextures pipeline.
/// </summary>
public static class NetTextureConstants
{
    public const int MaxChunkSize = 64 * 1024;
    public const uint MaxTransferPathLength = 4 * 1024;
    public const uint MaxTransferFileSize = 128 * 1024 * 1024;
    public const uint MaxTransferPayloadLength = MaxTransferPathLength + MaxTransferFileSize;
    public const int MaxFallbackChunkCount =
        (int) ((MaxTransferFileSize + (uint) MaxChunkSize - 1u) / (uint) MaxChunkSize);
}
