using com.etsoo.Utils.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Minio;

namespace com.etsoo.ThirdPartyExtentions.Minio
{
    /// <summary>
    /// S3 storage service collection extensions
    /// S3存储服务集合扩展
    /// </summary>
    public static class S3StorageServiceCollectionExtensions
    {
        /// <summary>
        /// S3 storage health check key
        /// S3存储健康检查键
        /// </summary>
        public const string S3StorageKey = "S3Storage";

        /// <summary>
        /// Local storage key
        /// 本地存储键
        /// </summary>
        public const string LocalStorageKey = "LocalStorage";

        public static IHealthChecksBuilder AddLocalStorage(
            this IHealthChecksBuilder builder,
            string? root = null,
            int? minMB = null,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            return builder.Add(new HealthCheckRegistration(
                LocalStorageKey,
                sp =>
                {
                    root ??= sp.GetRequiredService<IOptions<StorageOptions>>().Value.Root;
                    return new LocalStorageHealthCheck(root, minMB);
                },
                failureStatus,
                tags,
                timeout));
        }

        /// <summary>
        /// Add S3 storage health check
        /// 添加 S3 存储健康检查
        /// </summary>
        /// <param name="builder">Healthcheck Builder</param>
        /// <param name="failureStatus">Failure status</param>
        /// <param name="tags">Tags</param>
        /// <param name="timeout">Timeout</param>
        /// <returns></returns>
        public static IHealthChecksBuilder AddS3Storage(
            this IHealthChecksBuilder builder,
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            return builder.Add(new HealthCheckRegistration(
                S3StorageKey,
                sp => new S3StorageHealthCheck(sp.GetRequiredService<IMinioClientFactory>(), sp.GetRequiredService<IOptions<S3StorageOptions>>().Value.Root),
                failureStatus,
                tags,
                timeout));
        }

        /// <summary>
        /// Add S3 storage client
        /// 添加S3存储客户端
        /// </summary>
        /// <param name="services">Services</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="clientSetup">Client setup</param>
        /// <param name="s3">Is S3 client or compatible storage client</param>
        /// <returns></returns>
        public static IServiceCollection AddS3StorageClient(this IServiceCollection services, IConfigurationSection configuration, Action<IMinioClient, IServiceProvider>? clientSetup = null, bool s3 = false)
        {
            services.AddSingleton<IValidateOptions<S3StorageOptions>, ValidateS3StorageOptions>();
            services.AddOptions<S3StorageOptions>().Bind(configuration).ValidateOnStart();
            services.AddSingleton<IMinioClientFactory>((provider) =>
            {
                var options = provider.GetRequiredService<IOptions<S3StorageOptions>>().Value;
                return new MinioClientFactory(client =>
                {
                    var endpoint = new Uri(options.Endpoint);
                    client.WithCredentials(options.AccessKey, options.SecretKey)
                        .WithEndpoint(endpoint)
                        .WithSSL(endpoint.Scheme == "https")
                    ;

                    if (options.Timeout.HasValue)
                    {
                        client.WithTimeout(options.Timeout.Value);
                    }

                    clientSetup?.Invoke(client, provider);
                });
            });

            if (s3)
            {
                services.AddSingleton<IS3Storage, S3Storage>();
            }
            else
            {
                services.AddSingleton<IStorage, S3Storage>();
            }

            return services;
        }
    }
}
