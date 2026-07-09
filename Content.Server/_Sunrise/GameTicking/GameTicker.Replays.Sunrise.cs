using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Transfer;
using Content.Shared.CCVar;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    private void UploadReplayToS3(IWritableDirProvider directory, ResPath path)
    {
        if (!_cfg.GetCVar(CCVars.ReplayS3UploadEnabled))
            return;

        var endpoint = _cfg.GetCVar(CCVars.ReplayS3Endpoint);
        var bucket = _cfg.GetCVar(CCVars.ReplayS3Bucket);
        var accessKey = _cfg.GetCVar(CCVars.ReplayS3AccessKey);
        var secretKey = _cfg.GetCVar(CCVars.ReplayS3SecretKey);

        if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey))
        {
            _sawmillReplays.Error("S3 загрузка включена, но учетные данные или бакет не настроены!");
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                _sawmillReplays.Info($"Начало загрузки реплея {path} в S3 бакет '{bucket}'...");

                using var fileStream = directory.OpenRead(path);
                var fileName = path.Filename;

                var config = new AmazonS3Config
                {
                    ServiceURL = endpoint,
                    ForcePathStyle = true
                };

                using var client = new AmazonS3Client(accessKey, secretKey, config);
                using var fileTransferUtility = new TransferUtility(client);

                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = fileStream,
                    BucketName = bucket,
                    Key = fileName,
                    CannedACL = S3CannedACL.PublicRead
                };

                await fileTransferUtility.UploadAsync(uploadRequest);
                _sawmillReplays.Info($"Реплей {fileName} успешно загружен в S3.");

                directory.Delete(path);
                _sawmillReplays.Info($"Локальный файл реплея {fileName} успешно удален.");
            }
            catch (Exception e)
            {
                _sawmillReplays.Error($"Ошибка при загрузке реплея {path} в S3: {e}");
            }
        });
    }

    private void CleanupTempReplays()
    {
        try
        {
            var tempDir = _cfg.GetCVar(CCVars.ReplayAutoRecordTempDir);
            if (string.IsNullOrEmpty(tempDir))
                return;

            var tempPath = new ResPath(tempDir);
            if (!_resourceManager.UserData.Exists(tempPath))
                return;

            _sawmillReplays.Info($"Очистка временной папки реплеев: {tempPath}");
            var (files, _) = _resourceManager.UserData.Find($"{tempDir}/*", false);
            foreach (var file in files)
            {
                _sawmillReplays.Debug($"Удаление брошенного временного файла реплея: {file}");
                _resourceManager.UserData.Delete(file);
            }
        }
        catch (Exception e)
        {
            _sawmillReplays.Error($"Ошибка при очистке временной папки реплеев: {e}");
        }
    }
}
