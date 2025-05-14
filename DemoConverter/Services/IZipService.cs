using DemoConverter.Models;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;
// Используем IFormFile из Microsoft.AspNetCore.Http для работы с файлами отправленных через HTTP
namespace DemoConverter.Services
{
    public interface IZipService
    {
        Task<VenueData> ExtractZipAsync(IFormFile zipFile);
        Task<byte[]> CreateZipAsync(VenueData data);
        List<SbPlace> GetPlacesFromText(string placesText);
        List<SbSector> GetSectorsFromText(string sectorsText);
        string EditPlaces(List<SbPlace> places);
        string EditSectors(List<SbSector> sectors);
    }
}
