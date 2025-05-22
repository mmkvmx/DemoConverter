using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DemoConverter.Models;
using DemoConverter.Services;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using System.Xml;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DemoConverter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MainController : ControllerBase
    {
        private readonly ILogger<MainController> _logger;
        private readonly IMemoryCache _cache;
        readonly IZipService _zipService;
        readonly ISvgService _svgService;
        readonly IWebHostEnvironment _env;
        public MainController(IZipService zipService, ISvgService svgService, IMemoryCache cache, ILogger<MainController> logger, IWebHostEnvironment env)
        {
            _zipService = zipService;
            _svgService = svgService;
            _cache = cache;
            _logger = logger;
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Файл не передан");
            }
            VenueData venueData;
            try
            {
                venueData = await _zipService.ExtractZipAsync(file);
                var cacheKey = Guid.NewGuid().ToString();

                _cache.Set(cacheKey, venueData, TimeSpan.FromMinutes(20));

                return Ok(new { cacheKey, message = "Файл загружен. Готов к конвертации." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidDataException ex)
            {
                return BadRequest("Ошибка в формате ZIP-архива: " + ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Внутренняя ошибка сервера: " + ex.Message);
            }
        }
        [HttpPost("convert")]
        public IActionResult Convert([FromQuery] string cacheKey, [FromQuery] double placeMarginGorizontal = 0, [FromQuery] double placeMarginVertical = 0, [FromQuery] double placeSizeWidth = 0, [FromQuery] double placeSizeHeight = 0, [FromQuery] bool updateCircleToRect = false, [FromQuery] bool clearCss = false, [FromQuery] string customCss = "", [FromQuery] bool rectFill = false, [FromQuery] double cornerRadius = 0, [FromQuery] double fontSize = 9, [FromQuery] int fontWeight = 600)
        {
            if (!_cache.TryGetValue(cacheKey, out VenueData venueData))
                return BadRequest("Данные не найдены или устарели");

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(venueData.Svg);
                var sectors = venueData.SectorsList;
                var places = venueData.PlacesList;

                _svgService.MarkSectors(xmlDoc, sectors);
                _svgService.MarkPlaces(xmlDoc, places);
                _svgService.ClearSvgXmlDoc(xmlDoc, clearCss, customCss);
                _svgService.ModifySvg(xmlDoc, placeMarginGorizontal, placeMarginVertical, placeSizeWidth, placeSizeHeight, updateCircleToRect, rectFill, cornerRadius, fontSize, fontWeight);
                //объединяем сектора в единый блок с id="sectors"
                _svgService.MergeBlocks(xmlDoc, IdBlockType.Sectors);
                //объединяем места в единый блок с id="seats"
                _svgService.MergeBlocks(xmlDoc, IdBlockType.Seats);
                _svgService.ChangeAttributes(xmlDoc, "id", "seats", "places");

                var svgContent = xmlDoc.OuterXml;
                string newPlaces = _zipService.EditPlaces(places);
                string newSectors = _zipService.EditSectors(sectors);

                // сохраняем обратно
                venueData.Svg = svgContent;
                venueData.PlacesRaw = newPlaces;
                venueData.SectorsRaw = newSectors;
                // проверить
                venueData.PlacesList = _zipService.GetPlacesFromText(newPlaces);
                venueData.SectorsList = _zipService.GetSectorsFromText(newSectors);

                _cache.Set(cacheKey, venueData); // обязательно обновляем кэш

                //для предпросмотра схемы на клиенте возвращаем xml
                return Content(svgContent, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при конвертации: " + ex.Message);
            }
        }

        [HttpPost("edit")]
        public IActionResult Edit([FromQuery] string cacheKey, [FromQuery] double placeMarginGorizontal = 0, [FromQuery] double placeMarginVertical = 0, [FromQuery] double placeSizeWidth = 0, [FromQuery] double placeSizeHeight = 0, [FromQuery] bool rectFill = false, [FromQuery] double cornerRadius = 0, [FromQuery] double fontSize = 9, [FromQuery] int fontWeight = 600)
        {
            if (!_cache.TryGetValue(cacheKey, out VenueData venueData))
                return BadRequest("Данные не найдены или устарели");
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(venueData.Svg);
                _svgService.EditPlaces(xmlDoc, placeMarginGorizontal, placeMarginVertical, placeSizeWidth, placeSizeHeight, cornerRadius, rectFill, fontSize, fontWeight);
                var svgContent = xmlDoc.OuterXml;
                // сохраняем обратно
                venueData.Svg = svgContent;
                _cache.Set(cacheKey, venueData); // обязательно обновляем кэш
                //для предпросмотра схемы на клиенте возвращаем xml
                return Content(svgContent, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при редактировании: " + ex.Message);
            }
        }

        [HttpPost("delete")]
        public IActionResult Delete([FromQuery] string cacheKey, [FromQuery] string elementName)
        {
            if (!_cache.TryGetValue(cacheKey, out VenueData venueData))
                return BadRequest("Данные не найдены");

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(venueData.Svg);

            _svgService.DeleteXmlElement(xmlDoc, elementName);

            venueData.Svg = xmlDoc.OuterXml;
            _cache.Set(cacheKey, venueData);

            string svgContent = venueData.Svg;
            return Content(svgContent, "image/svg+xml");
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadZip([FromQuery] string cacheKey)
        {
            if (!_cache.TryGetValue(cacheKey, out VenueData venueData))
                return NotFound("Данные не найдены или устарели.");

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(venueData.Svg);

            // Сохраняем файлы
            await _zipService.SaveUpdatedFilesAsync(venueData, venueData.PlacesList, venueData.SectorsList, xmlDoc);

            // Создаём ZIP
            string[] filesToZip = { "Scheme.svg", "Places.txt", "Sectors.txt" };
            string zipPath = await _zipService.CreateZipAsync(filesToZip, "converted");

            if (!System.IO.File.Exists(zipPath))
                return NotFound("ZIP-файл не найден.");

            return PhysicalFile(zipPath, "application/zip", "converted.zip");
        }


    }
}
