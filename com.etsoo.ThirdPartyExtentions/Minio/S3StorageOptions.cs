using com.etsoo.Utils.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace com.etsoo.ThirdPartyExtentions.Minio
{
    /// <summary>
    /// Minio / S3 storage options
    /// </summary>
    public record S3StorageOptions : StorageOptions
    {
        [Required]
        public string AccessKey { get; set; } = string.Empty;

        [Required]
        public string SecretKey { get; set; } = string.Empty;

        [Required]
        [Url]
        public string Endpoint { get; set; } = string.Empty;

        public ServiceLifetime? Lifetime { get; set; }
    }

    [OptionsValidator]
    public partial class ValidateS3StorageOptions : IValidateOptions<S3StorageOptions>
    {
    }
}
