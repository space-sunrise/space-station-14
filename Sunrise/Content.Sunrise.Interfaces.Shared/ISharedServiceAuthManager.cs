namespace Content.Sunrise.Interfaces.Shared;

public interface ISharedServiceAuthManager
{
    public void Initialize();
}

public sealed record ServiceLinkResponse(string Url, byte[] Qrcode);
public sealed record ServiceAuthDataResponse(string Url, byte[] Qrcode);

public enum ServiceType
{
    Discord,
    Telegram,
    Github
}

public sealed class ServiceAuthData(string url, byte[] qrcode, ServiceType serviceType)
{
    public string Url { get; set; } = url;
    public byte[] QrCode { get; set; } = qrcode;
    public ServiceType ServiceType { get; set; } = serviceType;
}

public sealed class LinkedServiceData(string username, ServiceType serviceType)
{
    public string Username { get; set; } = username;
    public ServiceType ServiceType { get; set; } = serviceType;
}
