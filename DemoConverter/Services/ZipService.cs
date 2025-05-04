using DemoConverter.Models;
using System.IO.Compression;

//Улучшаем эффективность кода, не сохраняя временные файлы на диске
namespace DemoConverter.Services
{
    public class ZipService : IZipService
    {
        public async Task<VenueData> ExtractZipAsync(IFormFile zipFile)
        {
            // Проверяем, что файл не пуст и является zip-архивом
            if (zipFile == null || zipFile.Length == 0)
            {
                throw new ArgumentException("Файл пуст или не передан");
            }
            if (!zipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Файл должен быть в формате ZIP");
            }

            var requiredFiles = new[] { "Scheme.svg", "Places.txt", "Sectors.txt" };

            try
            {
                using (var archive = new ZipArchive(zipFile.OpenReadStream()))
                {
                    // Проверяем наличие всех файлов в архиве
                    foreach (var file in requiredFiles)
                    {
                        if (!archive.Entries.Any(e => e.Name.Equals(file, StringComparison.OrdinalIgnoreCase)))
                        {
                            throw new ArgumentException($"Файл {file} не найден в архиве");
                        }
                    }

                    // Локальная функция для чтения содержимого файла
                    async Task<string> ReadEntryAsync(string name)
                    {
                        var entry = archive.Entries.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                        if (entry == null)
                        {
                            throw new ArgumentException($"Файл {name} не найден в архиве");
                        }
                        using var reader = new StreamReader(entry.Open());
                        return await reader.ReadToEndAsync();
                    }

                    // Создаём объект VenueData
                    var venueData = new VenueData
                    {
                        Svg = await ReadEntryAsync("Scheme.svg"),
                        Places = await ReadEntryAsync("Places.txt"),
                        Sectors = await ReadEntryAsync("Sectors.txt")
                    };

                    return venueData;
                }
            }
            catch (InvalidDataException ex)
            {
                throw new ArgumentException("Невозможно открыть ZIP-архив", ex);
            }
        }

        public async Task<byte[]> CreateZipAsync(VenueData data)
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                // Добавляем файлы в архив
                async Task AddFileAsync(string name, string content)
                {
                    var entry = archive.CreateEntry(name);
                    using var entryStream = entry.Open();
                    using var writer = new StreamWriter(entryStream);
                    await writer.WriteAsync(content);
                }
                await AddFileAsync("Scheme.svg", data.Svg);
                await AddFileAsync("Places.txt", data.Places);
                await AddFileAsync("Sectors.txt", data.Sectors);
            }

            memoryStream.Position = 0;
            return memoryStream.ToArray();
        }
    }
}
