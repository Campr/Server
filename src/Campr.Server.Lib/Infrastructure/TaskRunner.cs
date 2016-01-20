using System;
using System.Threading;
using System.Threading.Tasks;

namespace Campr.Server.Lib.Infrastructure
{
    public class TaskRunner
    {
        public TaskRunner(Func<CancellationToken, Task> worker)
        {
            Ensure.Argument.IsNotNull(worker, nameof(worker));

            this.worker = worker;
            this.concurrencyLock = new AsyncLock();
        }

        private readonly Func<CancellationToken, Task> worker;
        private readonly AsyncLock concurrencyLock;
        private bool ranToCompletion;

        public async Task RunOnce(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Don't run this in parallel.
            using (await this.concurrencyLock.LockAsync(cancellationToken))
            {
                // If this did already run, stop here.
                if (this.ranToCompletion)
                    return;

                // Try to run the worker.
                await this.worker(cancellationToken);
                this.ranToCompletion = true;
            }
        }
    }
}