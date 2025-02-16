using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;
using Minio.DataModel.Args;

namespace com.etsoo.ThirdPartyExtentions.Minio
{
    /// <summary>
    /// S3 storage health check
    /// S3 存储健康检查
    /// </summary>
    public class S3StorageHealthCheck : IHealthCheck
    {
        private readonly IMinioClientFactory _factory;
        private readonly string _bucket;

        public S3StorageHealthCheck(IMinioClientFactory factory, string bucket)
        {
            _factory = factory;
            _bucket = bucket;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var bucketExistsArgs = new BucketExistsArgs().WithBucket(_bucket);

                using var client = _factory.CreateClient();

                await client.BucketExistsAsync(bucketExistsArgs, cancellationToken);

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(exception: ex);
            }
        }
    }
}
