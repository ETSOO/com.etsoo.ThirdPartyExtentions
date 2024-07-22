
namespace com.etsoo.ThirdPartyExtentions.Minio
{
    internal delegate Task StreamDispose(Stream stream);

    /// <summary>
    /// Minio / S3 storage stream
    /// </summary>
    internal class S3StorageStream : MemoryStream
    {
        readonly StreamDispose _streamDispose;

        public S3StorageStream(StreamDispose streamDispose)
        {
            _streamDispose = streamDispose;
        }

        protected override void Dispose(bool disposing)
        {
            if (CanRead)
            {
                _streamDispose(this).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (CanRead)
            {
                await _streamDispose(this);
            }
            await base.DisposeAsync();
        }
    }
}
