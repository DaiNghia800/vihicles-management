namespace Public_Transport.Models.Entities
{
    public class TripMapVM
    {
        public int TripId { get; set; }
        public string RouteName { get; set; }
        public double StartLat { get; set; }
        public double StartLng { get; set; }
        public double EndLat { get; set; }
        public double EndLng { get; set; }

        public List<MapStationVM> Stations { get; set; }
    }

}
