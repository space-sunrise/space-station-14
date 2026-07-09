using Robust.Shared.Configuration;

namespace Content.Shared._Sunrise.SunriseCCVars;

public sealed partial class SunriseCCVars
{
    /// <summary>
    ///     Включена ли автозагрузка реплеев в Wasabi S3 хранилище.
    /// </summary>
    public static readonly CVarDef<bool> ReplayS3UploadEnabled =
        CVarDef.Create("replay.s3_upload_enabled", false, CVar.SERVERONLY);

    /// <summary>
    ///     Эндпоинт Wasabi S3.
    /// </summary>
    public static readonly CVarDef<string> ReplayS3Endpoint =
        CVarDef.Create("replay.s3_endpoint", "https://s3.eu-central-1.wasabisys.com", CVar.SERVERONLY);

    /// <summary>
    ///     Имя S3 бакета для хранения реплеев.
    /// </summary>
    public static readonly CVarDef<string> ReplayS3Bucket =
        CVarDef.Create("replay.s3_bucket", "makuragames-stellar-stories-replays", CVar.SERVERONLY);

    /// <summary>
    ///     Access Key для авторизации в Wasabi S3.
    /// </summary>
    public static readonly CVarDef<string> ReplayS3AccessKey =
        CVarDef.Create("replay.s3_access_key", "", CVar.SERVERONLY);

    /// <summary>
    ///     Secret Access Key для авторизации в Wasabi S3.
    /// </summary>
    public static readonly CVarDef<string> ReplayS3SecretKey =
        CVarDef.Create("replay.s3_secret_key", "", CVar.SERVERONLY);
}
