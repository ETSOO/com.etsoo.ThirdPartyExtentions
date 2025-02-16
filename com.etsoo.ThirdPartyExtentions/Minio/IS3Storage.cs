using com.etsoo.Utils.Storage;
using Microsoft.Extensions.Logging;
using Minio.DataModel;

namespace com.etsoo.ThirdPartyExtentions.Minio
{
    /// <summary>
    /// Minio / S3 storage interface
    /// </summary>
    public interface IS3Storage : IStorage
    {
        /// <summary>
        /// Async delete files
        /// 异步删除多个文件
        /// </summary>
        /// <param name="paths">Paths</param>
        /// <param name="cancellationToken">Cancellation token</param>
        ValueTask<bool> DeleteAsync(IList<string> paths, CancellationToken cancellationToken = default);

        /// <summary>
        /// Async list entries under the path
        /// 异步列出路径下的条目
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="recursive">Recursive</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result</returns>
        ValueTask<IEnumerable<StorageEntry>?> ListEntriesAsync(string path, bool recursive, CancellationToken cancellationToken = default);

        /// <summary>
        /// Async read file
        /// 异步读文件
        /// </summary>
        /// <param name="path">Path</param>
        /// <param name="callback">Callback</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stream</returns>
        ValueTask<Stream?> ReadAsync(string path, Action<ObjectStat, Stream>? callback = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Async stat object
        /// 异步获取对象信息
        /// </summary>
        /// <param name="path">Object path</param>
        /// <param name="logger">Logger</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns></returns>
        Task<ObjectStat?> StatObjectAsync(string path, ILogger? logger = null, CancellationToken cancellationToken = default);
    }
}
