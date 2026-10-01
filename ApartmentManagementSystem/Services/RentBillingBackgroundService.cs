using ApartmentManagementSystem.Interfaces;

namespace ApartmentManagementSystem.Services
{
    // Runs billing once at startup and then every 24 hours:
    // creates new monthly rent charges and marks late ones as overdue.
    public class RentBillingBackgroundService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<RentBillingBackgroundService> _logger;

        public RentBillingBackgroundService(IServiceScopeFactory scopeFactory, TimeProvider timeProvider, ILogger<RentBillingBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(Interval, _timeProvider);
            do
            {
                await RunOnceAsync();
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task RunOnceAsync()
        {
            try
            {
                // BackgroundService is a singleton, but DbContext is scoped, so create a scope per run
                using var scope = _scopeFactory.CreateScope();
                var billing = scope.ServiceProvider.GetRequiredService<IBillingService>();

                var result = await billing.RunDailyBillingAsync(_timeProvider.GetLocalNow().DateTime);
                _logger.LogInformation("Billing run for {AsOf:yyyy-MM-dd}: {Created} charges created, {Overdue} marked overdue",
                    result.AsOf, result.ChargesCreated, result.ChargesMarkedOverdue);
            }
            catch (Exception ex)
            {
                // Don't let one failed run stop the job; it will try again next interval
                _logger.LogError(ex, "Billing run failed");
            }
        }
    }
}
