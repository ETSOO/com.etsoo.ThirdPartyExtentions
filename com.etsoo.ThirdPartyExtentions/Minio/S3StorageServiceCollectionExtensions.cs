using com.etsoo.Utils.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;

namespace com.etsoo.ThirdPartyExtentions.Minio
{
    public static class S3StorageServiceCollectionExtensions
    {
        public static IServiceCollection AddS3StorageClient(this IServiceCollection services, IConfigurationSection configuration)
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
                });
            });
            services.AddSingleton<IStorage, S3Storage>();

            return services;
        }
    }
}
