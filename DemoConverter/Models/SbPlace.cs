namespace DemoConverter.Models
{
    public class SbPlace
    {
        public int Id { get; set; }
        public required int SectorId { get; set; }
        public int? SectorChildId { get; set; }
        public int? Row { get; set; }
        public int Seat { get; set; }
    }
}
