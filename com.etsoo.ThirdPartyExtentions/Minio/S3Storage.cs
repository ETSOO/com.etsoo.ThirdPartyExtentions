using com.etsoo.HTTP;
using com.etsoo.Utils.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.DataModel.Tags;
using Minio.Exceptions;

namespace com.etsoo.ThirdPartyExtentions.Minio
{
    /// <summary>
    /// S3 / MinIO storage, make sure the bucket configured with 'Root' is created
    /// S3 / MinIO 存储，确保配置的存储桶'Root'值已创建
    /// </summary>
    public class S3Storage : StorageBase, IS3Storage
    {
        private readonly IMinioClientFactory _factory;
        private readonly S3StorageOptions _options;

        /// <summary>
        /// Constructor
        /// 构造函数
        /// </summary>
        /// <param name="factory">Client factory</param>
        /// <param name="options">Options</param>
        public S3Storage(IMinioClientFactory factory, S3StorageOptions options)
            : base(options.Root, options.URLRoot)
        {
            _factory = factory;
            _options = options;
        }

        [ActivatorUtilitiesConstructor]
        public S3Storage(IMinioClientFactory factory, IOptions<S3StorageOptions> options)
            : this(factory, options.Value)
        {

        }

        /// <summary>
        /// Local format path
        /// 本地格式化路径
        /// </summary>
        /// <param name="path">Path</param>
        /// <returns>Result</returns>
        protected string LocalFormatPath(string path)
        {
            return path.Replace('\\', '/').Trim(' ', '/');
        }

        /// <summary>
        /// Async copy file
        /// 异步复制文件
        /// </summary>
        /// <param name="srcPath">Source path</param>
        /// <param name="destPath">Destination path</param>
        /// <param name="tags">Tags to override</param>
        /// <param name="deleteSource">Is delete the source path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result</returns>
        public override async ValueTask<bool> CopyAsync(string srcPath, string destPath, IDictionary<string, string>? tags = null, bool deleteSource = false, CancellationToken cancellationToken = default)
        {
            var args = new CopyObjectArgs()
                .WithBucket(Root)
                .WithCopyObjectSource(new CopySourceObjectArgs().WithBucket(Root).WithObject(srcPath))
                .WithObject(destPath)
            ;

            if (tags != null) args.WithTagging(new Tagging(tags, true));

            using var client = _factory.CreateClient();
            await client.CopyObjectAsync(args, cancellationToken);

            return true;
        }

        /// <summary>
        /// Async delete file
        /// 异步删除文件
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public override async ValueTask<bool> DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            path = LocalFormatPath(path);

            var args = new RemoveObjectArgs()
                .WithBucket(Root)
                .WithObject(path)
            ;

            using var client = _factory.CreateClient();

            try
            {
                await client.RemoveObjectAsync(args, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Async delete files
        /// 异步删除多个文件
        /// </summary>
        /// <param name="paths">Paths</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public async ValueTask<bool> DeleteAsync(IList<string> paths, CancellationToken cancellationToken = default)
        {
            var args = new RemoveObjectsArgs()
                .WithBucket(Root)
                .WithObjects(paths)
            ;

            using var client = _factory.CreateClient();

            try
            {
                await client.RemoveObjectsAsync(args, cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Async delete folder
        /// 异步删除目录
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="recursive">Recursive</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public override async ValueTask<bool> DeleteFolderAsync(string path, bool recursive = false, CancellationToken cancellationToken = default)
        {
            var entries = await ListEntriesAsync(path, true, cancellationToken);
            if (entries == null) return false;

            return await DeleteAsync(entries.Select(e => e.FullName).ToList(), cancellationToken);
        }

        /// <summary>
        /// Async stat object
        /// 异步获取对象信息
        /// </summary>
        /// <param name="path">Object path</param>
        /// <param name="logger">Logger</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        public async Task<ObjectStat?> StatObjectAsync(string path, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            path = LocalFormatPath(path);

            var args = new StatObjectArgs()
                .WithBucket(Root)
                .WithObject(path)
            ;

            using var client = _factory.CreateClient();

            try
            {
                var obj = await client.StatObjectAsync(args, cancellationToken);
                return obj;
            }
            catch (ObjectNotFoundException)
            {
                return null;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Error checking file {path} existence", path);
                throw;
            }
        }

        /// <summary>
        /// Async check file exists
        /// 异步检查文件是否存在
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result</returns>
        public override async ValueTask<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            var obj = await StatObjectAsync(path, null, cancellationToken);
            return obj != null;
        }

        /// <summary>
        /// Async get write stream
        /// 异步获取写入流
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="writeCase">Write case</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stream</returns>
        public override async ValueTask<Stream?> GetWriteStreamAsync(string path, WriteCase writeCase = WriteCase.CreateNew, CancellationToken cancellationToken = default)
        {
            var exists = await FileExistsAsync(path, cancellationToken);
            if (exists && writeCase == WriteCase.CreateNew) return null;

            var newStream = new S3StorageStream(async (stream) =>
            {
                await WriteAsync(path, stream, writeCase, null, cancellationToken);
            });

            if (exists)
            {
                var stream = await ReadAsync(path, cancellationToken);
                if (stream != null)
                {
                    await stream.CopyToAsync(newStream, cancellationToken);
                }
            }

            return newStream;
        }

        /// <summary>
        /// Async list entries under the path
        /// 异步列出路径下的条目
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result</returns>
        public override async ValueTask<IEnumerable<StorageEntry>?> ListEntriesAsync(string path, CancellationToken cancellationToken = default)
        {
            return await ListEntriesAsync(path, false, cancellationToken);
        }

        /// <summary>
        /// Async list entries under the path
        /// 异步列出路径下的条目
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="recursive">Recursive</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result</returns>
        public async ValueTask<IEnumerable<StorageEntry>?> ListEntriesAsync(string path, bool recursive, CancellationToken cancellationToken = default)
        {
            path = LocalFormatPath(path);

            var args = new ListObjectsArgs()
                .WithBucket(Root)
                .WithPrefix(path) // No directory in Minio / S3, use prefix
                .WithRecursive(recursive)
            ;

            using var client = _factory.CreateClient();

            var result = new List<StorageEntry>();

            var entries = client.ListObjectsEnumAsync(args, cancellationToken);
            await foreach (var entry in entries)
            {
                var size = Convert.ToInt64(entry.Size);
                var time = entry.LastModifiedDateTime ?? DateTime.Now;
                var se = new StorageEntry()
                {
                    Name = Path.GetFileName(entry.Key),
                    FullName = entry.Key,
                    Size = size,
                    IsFile = !entry.IsDir,
                    CreationTime = time,
                    LastWriteTime = time
                };
                result.Add(se);
            }

            return result;
        }

        /// <summary>
        /// Async read file
        /// 异步读文件
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stream</returns>
        public override ValueTask<Stream?> ReadAsync(string path, CancellationToken cancellationToken = default)
        {
            return ReadAsync(path, null, cancellationToken);
        }

        /// <summary>
        /// Async read file
        /// 异步读文件
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="callback">Callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stream</returns>
        public async ValueTask<Stream?> ReadAsync(string path, Action<ObjectStat, Stream>? callback = null, CancellationToken cancellationToken = default)
        {
            path = LocalFormatPath(path);

            var contentStream = new MemoryStream();

            var args = new GetObjectArgs()
                .WithBucket(Root)
                .WithObject(path)
                .WithCallbackStream((stream, token) => stream.CopyToAsync(contentStream, token))
            ;

            using var client = _factory.CreateClient();

            var obj = await client.GetObjectAsync(args, cancellationToken);

            callback?.Invoke(obj, contentStream);

            contentStream.Seek(0, SeekOrigin.Begin);

            return contentStream;
        }

        /// <summary>
        /// Async read tags
        /// 异步读取标签
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result</returns>
        public override async ValueTask<IDictionary<string, string>?> ReadTagsAsync(string path, CancellationToken cancellationToken = default)
        {
            path = LocalFormatPath(path);

            using var client = _factory.CreateClient();
            var args = new GetObjectTagsArgs()
                .WithBucket(Root)
                .WithObject(path)
            ;

            var tagging = await client.GetObjectTagsAsync(args, cancellationToken);
            return tagging.Tags;
        }

        /// <summary>
        /// Async write file
        /// 异步写文件
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="stream">Stream</param>
        /// <param name="writeCase">Write case</param>
        /// <param name="tags">Tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public override async ValueTask<bool> WriteAsync(string path, Stream stream, WriteCase writeCase = WriteCase.CreateNew, IDictionary<string, string>? tags = null, CancellationToken cancellationToken = default)
        {
            path = LocalFormatPath(path);

            var exists = await FileExistsAsync(path, cancellationToken);
            if (exists && writeCase == WriteCase.CreateNew) return false;

            if (exists && writeCase == WriteCase.Appending)
            {
                var contentStream = await ReadAsync(path, cancellationToken);
                if (contentStream != null)
                {
                    contentStream.Seek(0, SeekOrigin.End);
                    await stream.CopyToAsync(contentStream, cancellationToken);
                    stream = contentStream;
                }
            }

            if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);

            var args = new PutObjectArgs()
                .WithBucket(Root)
                .WithObject(path)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
            ;

            if (tags != null) args.WithTagging(new Tagging(tags, true));

            var mimeType = MimeTypeMap.TryGetMimeType(Path.GetExtension(path));
            if (!string.IsNullOrEmpty(mimeType)) args.WithContentType(mimeType);

            using var client = _factory.CreateClient();
            var response = await client.PutObjectAsync(args, cancellationToken);

            return response.Etag != null;
        }

        /// <summary>
        /// Async write tags
        /// 异步写入标签
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="tags">Tags</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result</returns>
        public override async ValueTask<bool> WriteTagsAsync(string path, IDictionary<string, string> tags, CancellationToken cancellationToken = default)
        {
            path = LocalFormatPath(path);

            using var client = _factory.CreateClient();

            if (tags.Count == 0)
            {
                var args = new RemoveObjectTagsArgs()
                    .WithBucket(Root)
                    .WithObject(path)
                ;

                await client.RemoveObjectTagsAsync(args, cancellationToken);
            }
            else
            {
                var args = new SetObjectTagsArgs()
                    .WithBucket(Root)
                    .WithObject(path)
                    .WithTagging(new Tagging(tags, true))
                ;

                await client.SetObjectTagsAsync(args, cancellationToken);
            }

            return true;
        }
    }
}
