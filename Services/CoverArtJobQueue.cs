using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MashupApi.Services
{
    public class CoverArtJobQueue : IBackgroundQueue<CoverArtJob>
    {
        private ConcurrentQueue<CoverArtJob> _workItems = new ConcurrentQueue<CoverArtJob>();
        private SemaphoreSlim _queuedItems = new SemaphoreSlim(0);
        private SemaphoreSlim _maxQueueSize;

        public CoverArtJobQueue()
        {
            _maxQueueSize = new SemaphoreSlim(10);
        }
        public async Task EnqueueAsync(CoverArtJob job, CancellationToken cancellationToken)
        {
            if (job == null)
                throw new ArgumentNullException(nameof(job));

            await _maxQueueSize.WaitAsync(cancellationToken);
            _workItems.Enqueue(job);
            _queuedItems.Release();
        }

        public async Task<(CoverArtJob job, Action callback)> DequeueAsync(CancellationToken cancellationToken)
        {
            // This ensures we can never dequeue unless the semaphore has been increased by a corresponding release.
            await _queuedItems.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var job);

            return (job, () => _maxQueueSize.Release());
        }
    }
}
