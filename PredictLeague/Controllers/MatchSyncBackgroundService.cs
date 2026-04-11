using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PredictLeague.Controllers
{
    public class MatchSyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MatchSyncBackgroundService> _logger;

        public MatchSyncBackgroundService(IServiceProvider serviceProvider, ILogger<MatchSyncBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Match Sync Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Match Sync Background Service is working.");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var matchService = scope.ServiceProvider.GetRequiredService<MatchService>();
                        int updated = await matchService.RefreshFinishedMatchesAsync();
                        
                        if (updated > 0)
                        {
                            _logger.LogInformation($"Successfully updated {updated} matches in background.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing match sync background service.");
                }

                // Изчакване между проверките (например 10 минути)
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }

            _logger.LogInformation("Match Sync Background Service is stopping.");
        }
    }
}
