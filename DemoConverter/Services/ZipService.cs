using DemoConverter.Models;
using System.IO.Compression;
using System.Text.Json;

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
                    var placesText = await ReadEntryAsync("Places.txt");
                    var sectorsText = await ReadEntryAsync("Sectors.txt");

                    var venueData = new VenueData
                    {
                        Svg = await ReadEntryAsync("Scheme.svg"),
                        PlacesRaw = placesText,
                        SectorsRaw = sectorsText,
                        PlacesList = GetPlacesFromText(placesText),
                        SectorsList = GetSectorsFromText(sectorsText)
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
                await AddFileAsync("Places.txt", data.PlacesRaw);
                await AddFileAsync("Sectors.txt", data.SectorsRaw);
            }

            memoryStream.Position = 0;
            return memoryStream.ToArray();
        }

        public List<SbPlace> GetPlacesFromText(string placesText)
        {
            var placesList = new List<SbPlace>();
            using var reader = new StringReader(placesText);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',');

                if (parts.Length >= 5)
                {
                    try
                    {
                        int placeId = int.Parse(parts[0]);
                        int sectorId = int.Parse(parts[1]);
                        int? sectorChildId = string.IsNullOrWhiteSpace(parts[2]) ? null : int.Parse(parts[2]);
                        int? row = string.IsNullOrWhiteSpace(parts[3]) ? null : int.Parse(parts[3]);
                        int seat = int.Parse(parts[4]);

                        var place = new SbPlace
                        {
                            Id = placeId,
                            SectorId = sectorId,
                            SectorChildId = sectorChildId,
                            Row = row,
                            Seat = seat
                        };

                        placesList.Add(place);
                    }
                    catch (FormatException ex)
                    {
                        Console.WriteLine($"Некорректная строка: {line}. Ошибка: {ex.Message}");
                    }
                }
            }

            return placesList;
        }

        public List<SbSector> GetSectorsFromText(string sectorsText)
        {
            var sectorsList = new List<SbSector>();
            using var reader = new StringReader(sectorsText);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(',');

                if (parts.Length >= 5)
                {
                    if (int.TryParse(parts[0], out int sectorId) &&
                        int.TryParse(parts[3], out int sectorTypeInt) &&
                        int.TryParse(parts[4], out int sectorCapacity))
                    {
                        var sectorName = parts[1].Replace("\"", "").Trim();
                        if (string.IsNullOrEmpty(sectorName))
                        {
                            Console.WriteLine($"Имя сектора пустое, устанавливаем 'Основной'");
                            sectorName = "Основной";
                        }

                        var hallArea = parts[2].Replace("\"", "").Trim();
                        if (string.IsNullOrEmpty(hallArea))
                        {
                            Console.WriteLine($"Имя области сектора пустое, устанавливаем 'Основной'");
                            hallArea = "Основной";
                        }

                        if (Enum.IsDefined(typeof(SbSectorType), sectorTypeInt))
                        {
                            var sectorType = (SbSectorType)sectorTypeInt;

                            sectorsList.Add(new SbSector
                            {
                                Id = sectorId,
                                Name = sectorName,
                                HallArea = hallArea,
                                Type = sectorType,
                                Capacity = sectorCapacity
                            });
                        }
                        else
                        {
                            Console.WriteLine($"Неизвестный тип сектора: {sectorTypeInt}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка парсинга чисел в строке: {line}");
                    }
                }
            }

            return sectorsList;
        }

    }
}
