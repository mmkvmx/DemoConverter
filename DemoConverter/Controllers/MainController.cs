using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DemoConverter.Models;
using DemoConverter.Services;
using System.Runtime.CompilerServices;

namespace DemoConverter.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MainController : ControllerBase
    {
        readonly IZipService _zipService;
        public MainController(IZipService zipService)
        {
            _zipService = zipService;
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

            // 2. Обрабатываем данные

            // 3. Создаём архив
            byte[] resultZip = await _zipService.CreateZipAsync(venueData);

            // 4. Отдаём как файл
            return File(resultZip, "application/zip", "converted.zip");
        }
    }
}
