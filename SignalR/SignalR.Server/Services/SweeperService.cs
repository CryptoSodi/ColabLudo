namespace SignalR.Server.Services
{
    /// Background worker that calls the sweeper method on a fixed interval.
    /// <summary>
    /// ctor for the background sweeper service.
    /// </summary>
    public class SweeperService(CryptoHelper cryptoHelper) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("SweeperService starting...");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    cryptoHelper.SweepAllSubAccounts();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sweeping sub-accounts: {ex.Message}");
                }
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}