using DemoConverter.Models;
using System.IO.Compression;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text;

//Улучшаем эффективность кода, не сохраняя временные файлы на диске
namespace DemoConverter.Services
{
    public class ZipService : IZipService
    {
        private readonly IWebHostEnvironment _env;
        public ZipService(IWebHostEnvironment env)
        {
            _env = env;
        }
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
        public async Task<string> CreateZipAsync(string[] fileNames, string zipFileName)
        {
            string folderPath = Path.Combine(_env.WebRootPath, "files", "converted");
            string zipPath = Path.Combine(folderPath, $"{zipFileName}.zip");

            // Удаляем старый архив, если есть
            if (System.IO.File.Exists(zipPath))
                System.IO.File.Delete(zipPath);

            // Создаём ZIP
            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var fileName in fileNames)
                {
                    string filePath = Path.Combine(folderPath, fileName);

                    if (System.IO.File.Exists(filePath))
                    {
                        zip.CreateEntryFromFile(filePath, fileName); // имя внутри архива
                    }
                }
            }

            return zipPath; // Можно использовать, чтобы отдать файл
        }


        public List<SbPlace> GetPlacesFromText(string placesText)
        {
            var places = new List<SbPlace>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim,
            };

            using var reader = new StringReader(placesText);
            using var csv = new CsvReader(reader, config);

            while (csv.Read())
            {
                try
                {
                    var place = new SbPlace
                    {
                        // Используем TryGetField для безопасного извлечения значений
                        Id = csv.TryGetField(0, out string idVal) && int.TryParse(idVal, out var id) ? id : 0, // Проверка на валидность числа
                        SectorId = csv.TryGetField(1, out string sectorIdVal) && int.TryParse(sectorIdVal, out var sectorId) ? sectorId : 0,
                        SectorChildId = csv.TryGetField(2, out string scid) && int.TryParse(scid, out var scidVal) ? scidVal : (int?)null,
                        Row = csv.TryGetField(3, out string rowVal) && int.TryParse(rowVal, out var rowNum) ? rowNum : (int?)null,
                        Seat = csv.TryGetField(4, out string seatVal) && int.TryParse(seatVal, out var seatNum) ? seatNum : 0
                    };

                    places.Add(place);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка парсинга строки: {csv.Parser.RawRecord}. {ex.Message}");
                }
            }

            return places;
        }


        public List<SbSector> GetSectorsFromText(string sectorsText)
        {
            var sectors = new List<SbSector>();
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                MissingFieldFound = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim,
            };

            using var reader = new StringReader(sectorsText);
            using var csv = new CsvReader(reader, config);

            while (csv.Read())
            {
                try
                {
                    int sectorId = csv.TryGetField(0, out string sectorIdVal) && int.TryParse(sectorIdVal, out var sectorIdResult) ? sectorIdResult : 0;
                    string sectorName = csv.TryGetField(1, out string sectorNameVal) ? sectorNameVal?.Replace("\"", "")?.Trim() ?? "Основной" : "Основной";
                    string hallArea = csv.TryGetField(2, out string hallAreaVal) ? hallAreaVal?.Replace("\"", "")?.Trim() ?? "Основной" : "Основной";
                    int sectorTypeRaw = csv.TryGetField(3, out string sectorTypeRawVal) && int.TryParse(sectorTypeRawVal, out var sectorType) ? sectorType : 0;
                    int capacity = csv.TryGetField(4, out string capacityVal) && int.TryParse(capacityVal, out var capacityResult) ? capacityResult : 0;

                    if (!Enum.IsDefined(typeof(SbSectorType), sectorTypeRaw))
                    {
                        Console.WriteLine($"Неизвестный тип сектора: {sectorTypeRaw} в строке: {csv.Parser.RawRecord}");
                        continue;
                    }

                    var sector = new SbSector
                    {
                        Id = sectorId,
                        Name = sectorName,
                        HallArea = hallArea,
                        Type = (SbSectorType)sectorTypeRaw,
                        Capacity = capacity
                    };

                    sectors.Add(sector);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка парсинга строки: {csv.Parser.RawRecord}. {ex.Message}");
                }
            }

            return sectors;
        }

        public string EditPlaces(List<SbPlace> places)
        {
            var sb = new StringBuilder();

            foreach (var place in places)
            {
                if (place.SectorId > 0 || place.Row > 0 || place.Seat > 0)
                {
                    string line = $"{place.SectorId}\t{place.Row}\t{place.Seat}";
                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }

        public string EditSectors(List<SbSector> sectors)
        {
            var sb = new StringBuilder();

            Dictionary<SbSectorType, TsSectorType> sectorsSbType = new Dictionary<SbSectorType, TsSectorType>
        {
            { SbSectorType.Entrance, TsSectorType.Entrance }, //  у СБ входной имеет тип 2, а у нас 0
            { SbSectorType.WithPlaces, TsSectorType.WithPlaces }, //  c местами
        };


            foreach (var sector in sectors)
            {
                if (sectorsSbType.TryGetValue(sector.Type, out TsSectorType type) && !string.IsNullOrEmpty(sector.Name))
                {
                    string line = $"{sector.Id}\t{sector.Name}\t{(int)type}\t{sector.Capacity}";
                    sb.AppendLine(line);
                }
                else
                {
                    Console.Write($"Тип сектора  СБ: {sector.Type} не найден, сектор пропускаем");
                }
            }
            return sb.ToString();
        }

        public async Task SaveFileAsync(string fileName, string content, string extension)
        {
            // Путь к папке wwwroot/files/converted
            string folderPath = Path.Combine(_env.WebRootPath, "files", "converted");
            Directory.CreateDirectory(folderPath); // Создаём папку, если нет

            // Полный путь к файлу
            string fullPath = Path.Combine(folderPath, $"{fileName}.{extension}");

            // Асинхронно сохраняем файл
            await File.WriteAllTextAsync(fullPath, content);
        }

        //очистка папки converted
        public void ClearConvertedFolder()
        {
            string folderPath = Path.Combine(_env.WebRootPath, "files", "converted");

            if (!Directory.Exists(folderPath))
                return;

            var files = Directory.GetFiles(folderPath);

            foreach (var file in files)
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch (Exception ex)
                {
                    
                }
            }
        }
    }
}
