using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Campr.Server.Lib.Infrastructure
{
    public class AsyncSemaphore
    {
        public AsyncSemaphore(uint initialCount = 1)
        {
            this.currentCount = initialCount;
        }

        private static readonly Task CompletedTask = Task.FromResult(true);

        private readonly Queue<TaskCompletionSource<bool>> waitersQueue = new Queue<TaskCompletionSource<bool>>(); 
        private uint currentCount;

        public uint Count
        {
            get
            {
                lock (this.waitersQueue)
                {
                    return this.currentCount;
                }
            }
        }

        public Task WaitAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            lock (this.waitersQueue)
            {
                // Make sure we're not already cancelled.
                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();

                // If we already have tokens, we can return immediatly.
                if (this.currentCount > 0)
                {
                    this.currentCount--;
                    return CompletedTask;
                }
                
                // Otherwise, create the waiter and queue it.
                var waiter = new TaskCompletionSource<bool>();

                // Register on the cancellation token for the waiter cancellation.
                cancellationToken.Register(() => waiter.TrySetCanceled());

                // Make sure we haven't been canceled in the meantime.
                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();
                
                this.waitersQueue.Enqueue(waiter);
                return waiter.Task;
            } 
        }

        public void Release()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (this.waitersQueue)
            {
                // Try and find a task to release.
                while (this.waitersQueue.Count > 0 && (toRelease == null || toRelease.Task.IsCompleted))
                    toRelease = this.waitersQueue.Dequeue();

                // If none was found, release a token.
                if (toRelease == null || toRelease.Task.IsCompleted)
                    this.currentCount++;

                // Release the task.
                toRelease?.TrySetResult(true);
            }
        }
    }
}