namespace DemoConverter.Models
{
    public class SbSector
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string HallArea { get; set; }
        public SbSectorType Type { get; set; }
        public int Capacity { get; set; }
    }

    public enum SbSectorType
    {
        WithPlaces = 1,
        Entrance = 2
    }
}
