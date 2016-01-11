using System;
using System.Threading;
using System.Threading.Tasks;
using Campr.Server.Lib.Infrastructure;
using Campr.Server.Lib.Services;

namespace Campr.Server.Lib.Helpers
{
    class RetryHelpers : IRetryHelpers
    {
        public RetryHelpers(ILoggingService loggingService)
        {
            Ensure.Argument.IsNotNull(loggingService, nameof(loggingService));
            this.loggingService = loggingService;
        }

        private readonly ILoggingService loggingService;
       
        public async Task RetryAsync(Func<Task> worker, CancellationToken cancellationToken = new CancellationToken())
        {
            var retryCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await worker();
                    return;
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (retryCount > 3)
                        throw;

                    this.loggingService.Exception(ex, "Exception was thrown, we'll retry.");
                }
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(10, retryCount)), cancellationToken);
                retryCount++;
            }
        }

        public async Task<T> RetryAsync<T>(Func<Task<T>> worker, CancellationToken cancellationToken = new CancellationToken())
        {
            var retryCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    return await worker();
                }
                catch (TaskCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (retryCount > 3)
                        throw;

                    this.loggingService.Exception(ex, "Exception was thrown, we'll retry.");
                }
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(10, retryCount)), cancellationToken);
                retryCount++;
            }
            return default(T);
        }
    }
}