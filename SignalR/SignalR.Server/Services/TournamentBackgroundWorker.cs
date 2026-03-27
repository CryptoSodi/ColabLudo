using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SignalR.Server.Services
{
    public class TournamentBackgroundWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TournamentBackgroundWorker> _logger;

        public TournamentBackgroundWorker(IServiceProvider serviceProvider, ILogger<TournamentBackgroundWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Tournament Background Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var tournamentService = scope.ServiceProvider.GetRequiredService<TournamentService>();
                        await tournamentService.ProcessTournamentLifecycle();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing tournament lifecycle.");
                }

                // Check every 10 minutes (adjust as needed)
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }

            _logger.LogInformation("Tournament Background Worker is stopping.");
        }
    }
}
