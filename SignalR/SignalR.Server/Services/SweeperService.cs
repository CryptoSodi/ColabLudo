namespace SignalR.Server.Services
{
    /// Background worker that calls the sweeper method on a fixed interval.
    public class SweeperService : BackgroundService
    {
        private readonly CryptoHelper _cryptoHelper;

        /// <summary>
        /// ctor for the background sweeper service.
        /// </summary>
        public SweeperService(CryptoHelper cryptoHelper)
        {
            _cryptoHelper = cryptoHelper;
        }

        /// <summary>
        /// Executes SweepAllSubAccountsAsync every 5 minutes until cancellation.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("SweeperService starting...");
            while (!stoppingToken.IsCancellationRequested)
            {
                    _cryptoHelper.SweepAllSubAccounts();
                try
                {
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