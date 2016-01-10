using System;
using System.Threading;
using System.Threading.Tasks;

namespace Campr.Server.Lib.Infrastructure
{
    public class AsyncLock
    {
        public AsyncLock()
        {
            this.semaphore = new AsyncSemaphore();
            this.releaser = Task.FromResult(new Releaser(this));
        }

        public Task<Releaser> LockAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            var wait = this.semaphore.WaitAsync(cancellationToken);
            return wait.IsCompleted 
                ? this.releaser 
                : wait.ContinueWith((_, state) => new Releaser((AsyncLock)state),
                    this, 
                    cancellationToken,
                    TaskContinuationOptions.ExecuteSynchronously, 
                    TaskScheduler.Default);
        }

        private readonly AsyncSemaphore semaphore;
        private readonly Task<Releaser> releaser; 

        public struct Releaser : IDisposable
        {
            internal Releaser(AsyncLock toReleaseLock)
            {
                this.toReleaseLock = toReleaseLock;
            }

            private readonly AsyncLock toReleaseLock;

            public void Dispose()
            {
                this.toReleaseLock?.semaphore.Release();
            }
        } 
    }
}