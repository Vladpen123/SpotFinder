namespace SpotFinder.Web.Utils
{
    public static class Utils
    {
        public static string GetStatusBadge(string status) => status switch
        {
            "Paid" => "bg-success-subtle text-success border border-success-subtle",
            "Reserved" => "bg-warning-subtle text-warning border border-warning-subtle",
            "Cancelled" => "bg-danger-subtle text-danger",
            _ => "bg-secondary-subtle text-secondary"
        };
    }
}
