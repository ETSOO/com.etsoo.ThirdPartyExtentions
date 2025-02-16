using com.etsoo.Utils.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        /// Add S3 storage client
        /// 添加S3存储客户端
        /// </summary>
        /// <param name="services">Services</param>
        /// <param name="configuration">Configuration</param>
        /// <param name="s3">Is S3 client or compatible storage client</param>
        /// <returns></returns>
        public static IServiceCollection AddS3StorageClient(this IServiceCollection services, IConfigurationSection configuration, bool s3 = false)
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
                        .WithEndpoint(endpoint.Host)
                        .WithSSL(endpoint.Scheme == "https")
                    ;

                    if (options.Timeout.HasValue)
                    {
                        client.WithTimeout(options.Timeout.Value);
                    }
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
