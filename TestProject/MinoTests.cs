using com.etsoo.ThirdPartyExtentions.Minio;
using com.etsoo.Utils;
using com.etsoo.Utils.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;
using System.Text;

namespace TestProject
{
    [TestClass]
    public class MinoTests
    {
        readonly static IS3Storage storage;
        readonly static IStorage localStorage;
        readonly static HealthCheckService hcService;

        static MinoTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var minioSection = configuration.GetSection("Minio");

            var storageOptions = configuration.GetSection("Storage").Get<StorageOptions>() ?? throw new Exception("Storage configuration not found");

            var services = new ServiceCollection();

            services.AddLogging();
            services.AddHttpClient();

            var ls = new LocalStorage(storageOptions);
            services.AddSingleton<IStorage>(ls);

            services.AddS3StorageClient(minioSection, (client, sp) =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var hc = factory.CreateClient();
                hc.DefaultRequestHeaders.ConnectionClose = true;
                client.WithHttpClient(hc);
            }, true);

            services.AddHealthChecks()
                .AddS3Storage()
                .AddLocalStorage("C:\\test\\", 50000000);

            var serviceProvider = services.BuildServiceProvider();
            storage = serviceProvider.GetRequiredService<IS3Storage>();
            localStorage = serviceProvider.GetRequiredService<IStorage>();
            hcService = serviceProvider.GetRequiredService<HealthCheckService>();
        }

        [ClassCleanup()]
        public static async Task ClassCleanup()
        {
            await storage.DeleteFolderAsync("/test");
        }

        [TestMethod]
        public async Task FileExistsAsyncFalseTest()
        {
            // Act
            var result = await storage.FileExistsAsync("/test/a/test.txt");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task GetWriteStreamAsyncTest()
        {
            // Arrange
            var file = "/test/b/test.txt";
            await storage.DeleteAsync(file);
            var content = "Hello, world!";
            await using var stream = await storage.GetWriteStreamAsync(file);
            Assert.IsNotNull(stream);

            // Act
            var bytes = Encoding.UTF8.GetBytes(content);
            await stream.WriteAsync(bytes);
            await stream.DisposeAsync();

            // Assert
            var contentStream = await storage.ReadAsync(file);
            Assert.IsNotNull(contentStream);
            Assert.AreEqual(0, contentStream.Position);
            Assert.IsTrue(contentStream.Length == bytes.Length);

            var contentActual = SharedUtils.StreamToString(contentStream);
            Assert.AreEqual(content, contentActual);
        }

        [TestMethod]
        public async Task S3HealthCheckTest()
        {
            // Act
            var result = await hcService.CheckHealthAsync();

            // Assert
            Assert.IsTrue(result.Entries.ContainsKey(S3StorageServiceCollectionExtensions.S3StorageKey));
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
        }
    }
}