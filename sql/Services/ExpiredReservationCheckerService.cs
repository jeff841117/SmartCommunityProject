using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using sql.Models;

namespace sql.Services
{
    public class ExpiredReservationCheckerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpiredReservationCheckerService> _logger;

        public ExpiredReservationCheckerService(IServiceProvider serviceProvider, ILogger<ExpiredReservationCheckerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("過期預約檢查服務已啟動");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var reservationService = scope.ServiceProvider.GetRequiredService<ReservationService>();

                        // 每分鐘檢查一次過期預約
                        reservationService.AutoCompleteExpiredReservations();
                        _logger.LogInformation("已檢查過期預約");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "檢查過期預約時發生錯誤");
                }

                // 等待1分鐘
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("過期預約檢查服務已停止");
        }
    }
}

