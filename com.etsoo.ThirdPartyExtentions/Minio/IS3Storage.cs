using com.etsoo.Utils.Storage;

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
    }
}
