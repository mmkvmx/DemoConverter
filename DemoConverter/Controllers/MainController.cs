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
        public IActionResult Convert([FromQuery] string cacheKey, [FromQuery] double placeMarginGorizontal = 0, [FromQuery] double placeMarginVertical = 0, [FromQuery] double placeSizeWidth = 0, [FromQuery] double placeSizeHeight = 0, [FromQuery] bool updateCircleToRect = false, [FromQuery] bool clearCss = false, [FromQuery] string customCss = "")
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
                _svgService.ModifySvg(xmlDoc, placeMarginGorizontal, placeMarginVertical, placeSizeWidth, placeSizeHeight, updateCircleToRect);
                //объединяем сектора в единый блок с id="sectors"
                _svgService.MergeBlocks(xmlDoc, IdBlockType.Sectors);
                //объединяем места в единый блок с id="seats"
                _svgService.MergeBlocks(xmlDoc, IdBlockType.Seats);
                _svgService.ChangeAttributes(xmlDoc, "id", "seats", "places");

                var svgContent = xmlDoc.OuterXml;
                string newPlaces = _zipService.EditPlaces(places);
                string newSectors = _zipService.EditSectors(sectors);

                //очищаем папку converted перед сохранением
                _zipService.ClearConvertedFolder();

                //сохраняем схему
                _zipService.SaveFileAsync("Scheme", svgContent, "svg");
                //сохраняем файлы мест и секторов
                _zipService.SaveFileAsync("Sectors", newSectors, "txt");
                _zipService.SaveFileAsync("Places", newPlaces, "txt");

                // создаем архив
                string[] fileNames = new string[]
                {
                    "Places.txt",
                    "Scheme.svg",
                    "Sectors.txt"
                };
                string zipFileName = "converted";
                _zipService.CreateZipAsync(fileNames, zipFileName);
                //для предпросмотра схемы на клиенте возвращаем xml
                return Content(svgContent, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при конвертации: " + ex.Message);
            }
        }

        [HttpGet("download")]
        public IActionResult DownloadZip()
        {
            string folderPath = Path.Combine(_env.WebRootPath, "files", "converted");
            string zipName = "converted.zip";
            string zipPath = Path.Combine(folderPath, zipName);

            if (!System.IO.File.Exists(zipPath))
            {
                return NotFound("ZIP-файл не найден.");
            }

            return PhysicalFile(zipPath, "application/zip", zipName);
        }

    }
}
