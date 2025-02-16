using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace com.etsoo.ThirdPartyExtentions.Minio
{
    /// <summary>
    /// Local storage health check
    /// 本地存储健康检查
    /// </summary>
    public class LocalStorageHealthCheck : IHealthCheck
    {
        private readonly string _driveName;
        private readonly long _minBytes;

        /// <summary>
        /// Constructor
        /// 构造函数
        /// </summary>
        /// <param name="root">Root path</param>
        /// <param name="minMB">Minimum MB</param>
        public LocalStorageHealthCheck(string root, long? minMB = null)
        {
            minMB ??= 100;
            if (minMB <= 0)
            {
                throw new ArgumentException("Minimum MB should be greater than 0");
            }
            _minBytes = minMB.Value * 1024 * 1024;

            var f = new FileInfo(root);
            var driveName = Path.GetPathRoot(f.FullName);
            if (string.IsNullOrEmpty(driveName))
            {
                throw new ArgumentException("Root path is not valid");
            }
            _driveName = driveName;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var drive = new DriveInfo(_driveName);
                    if (!drive.IsReady)
                    {
                        return HealthCheckResult.Unhealthy($"Drive {_driveName} is not ready");
                    }

                    if (drive.AvailableFreeSpace < _minBytes)
                    {
                        var mb = drive.AvailableFreeSpace / 1024 / 1024;
                        return HealthCheckResult.Unhealthy($"Drive {_driveName} has {mb}MB space");
                    }
                    else if (drive.AvailableFreeSpace < _minBytes * 2)
                    {
                        return HealthCheckResult.Degraded($"Drive {_driveName} has limited free space");
                    }

                    return HealthCheckResult.Healthy();
                }
                catch (Exception ex)
                {
                    return HealthCheckResult.Unhealthy(exception: ex);
                }
            }, cancellationToken);
        }
    }
}
