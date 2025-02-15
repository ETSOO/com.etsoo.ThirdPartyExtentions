using com.etsoo.ThirdPartyExtentions.Minio;
using com.etsoo.Utils;
using com.etsoo.Utils.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace TestProject
{
    [TestClass]
    public class MinoTests
    {
        readonly static IStorage storage;

        static MinoTests()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var minioSection = configuration.GetSection("Minio");

            var services = new ServiceCollection();
            services.AddS3StorageClient(minioSection);

            var serviceProvider = services.BuildServiceProvider();
            storage = serviceProvider.GetRequiredService<IStorage>();
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
            await stream.WriteAsync(Encoding.UTF8.GetBytes(content));
            await stream.DisposeAsync();

            // Assert
            var contentStream = await storage.ReadAsync(file);
            Assert.IsNotNull(contentStream);
            Assert.IsTrue(contentStream.Position == 0);
            var contentActual = SharedUtils.StreamToString(contentStream);
            Assert.AreEqual(content, contentActual);
        }
    }
}