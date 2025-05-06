namespace DemoConverter.Models
{
    public class VenueData
    {
        public string Svg { get; set; }
        public string PlacesRaw { get; set; }
        public string SectorsRaw { get; set; }
        public List<SbPlace> PlacesList { get; set; }
        public List<SbSector> SectorsList { get; set; }
    }
}
