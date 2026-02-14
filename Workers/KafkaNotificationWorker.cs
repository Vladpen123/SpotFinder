using Confluent.Kafka;
using SpotFinder.Shared.Kafka;
using SpotFinder.Shared.Kafka.KafkaEvents;
using SpotFinder.Web.Services;
using System.Text.Json;

namespace SpotFinder.Web.Workers
{
    public class KafkaNotificationWorker(IConsumer<string, string> consumer, NotificationService notificationService, ILogger<KafkaNotificationWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            consumer.Subscribe([KafkaTopics.Billing, KafkaTopics.Bookings, KafkaTopics.Spots]);
            logger.LogInformation("🔔 Web Notification Worker started.");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(stoppingToken);
                        if (result == null || result.IsPartitionEOF) continue;

                        var topic = result.Topic;

                        var rawJson = result.Message.Value;
                        if (topic == KafkaTopics.Billing)
                        {
                            var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedEvent>(result.Message.Value);
                            if (paymentEvent != null && !string.IsNullOrEmpty(paymentEvent.BookingId))
                            {
                                // Уведомляем об оплате
                                notificationService.NotifyPaymentSuccess(paymentEvent.BookingId);
                            }
                        }
                        else if (topic == KafkaTopics.Bookings || topic == KafkaTopics.Spots)
                        {
                            // Нам не важно, кто и что забронировал, нам важно просто пнуть карту "Обновись"
                            logger.LogInformation("🔔 New Booking detected. Refreshing maps for everyone...");
                            notificationService.NotifyStateMapChanged();
                        }
                    }
                    catch (ConsumeException) { await Task.Delay(1000, stoppingToken); }
                    catch (Exception ex) { logger.LogError(ex, "Error in Notification Worker"); }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Notification Worker");
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}
