namespace SpotFinder.Web.Services
{
    public class MapStateService
    {
        public double Latitude { get; set; } = 50.4501;
        public double Longitude { get; set; } = 30.5234;
        public int Zoom { get; set; } = 13;

        public void UpdateState(double lat, double lon, int zoom)
        {
            Latitude = lat;
            Longitude = lon;
            Zoom = zoom;
        }
    }
}
