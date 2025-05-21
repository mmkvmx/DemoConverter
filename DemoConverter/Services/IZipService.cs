using DemoConverter.Models;
using Microsoft.AspNetCore.Http;
using System.IO.Compression;
using System.Xml;
// Используем IFormFile из Microsoft.AspNetCore.Http для работы с файлами отправленных через HTTP
namespace DemoConverter.Services
{
    public interface IZipService
    {
        Task<VenueData> ExtractZipAsync(IFormFile zipFile);
        Task<string> CreateZipAsync(string[] fileNames, string zipFileName);
        List<SbPlace> GetPlacesFromText(string placesText);
        List<SbSector> GetSectorsFromText(string sectorsText);
        string EditPlaces(List<SbPlace> places);
        string EditSectors(List<SbSector> sectors);
        Task SaveFileAsync(string fileName, string content, string extension);
        Task SaveUpdatedFilesAsync(VenueData data, List<SbPlace> places, List<SbSector> sectors, XmlDocument xmlDoc);
        void ClearConvertedFolder();
    }
}
