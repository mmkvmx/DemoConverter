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

        public MainController(IZipService zipService, ISvgService svgService, IMemoryCache cache, ILogger<MainController> logger)
        {
            _zipService = zipService;
            _svgService = svgService;
            _cache = cache;
            _logger = logger;
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
        public IActionResult Convert([FromQuery] string cacheKey)
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

                var svgContent = xmlDoc.OuterXml;
                var svgBytes = System.Text.Encoding.UTF8.GetBytes(svgContent);
                var fileName = $"converted_{DateTime.Now:yyyyMMddHHmmss}.svg";

                return Content(svgContent, "image/svg+xml");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Ошибка при конвертации: " + ex.Message); 
            }
        }
    }
}
