namespace SpotFinder.Web.Services
{
    public class NotificationService
    {
        public event Action<string?> PaymentConfirmed;
        public void NotifyPaymentSuccess(string bookingId) => PaymentConfirmed?.Invoke(bookingId);

        public event Action? MapStateChanged;
        public void NotifyStateMapChanged() => MapStateChanged?.Invoke();
    }
}
