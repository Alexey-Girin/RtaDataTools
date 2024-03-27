using System.Configuration;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using RtaDiagramsDownloader.Models;

namespace rtadatatool
{
    public class RtaCardsDownloader
    {
        private const int reqDelay = 1000;

        private FileFormat downloadFormat;

        private string rtaExportedCardsDirectory;

        private string urlRtaCardsGenerate;

        private string urlRtaCardsExport;

        private DownloadSetting setting;

        private Logger logger = new Logger();

        public RtaCardsDownloader(FileFormat format, DownloadSetting setting)
        {
            downloadFormat = format;
            this.setting = setting;
            rtaExportedCardsDirectory = @$"./{Extensions.GetExportedCardsDirectoryByFormat(format, setting)}";
            urlRtaCardsGenerate = Extensions.GetCardsGenerateUrlByFormat(format)!;
            urlRtaCardsExport = ConfigurationManager.AppSettings["urlRtaCardsExport"]!.ToString();
        }

        public async Task DownloadCards()
        {
            var currentStartDate = setting.dateStart;
            var currentEndDate = GetEndOfMonthDate(currentStartDate);

            ClearDirectory(rtaExportedCardsDirectory);
            logger.Log($"Выгрузка карточек в формате {Extensions.GetFileExtByFormat(downloadFormat)}.");
            logger.Log($"Период: {setting.dateStart:dd.MM.yyyy} - {GetEndOfMonthDate(setting.dateEnd):dd.MM.yyyy}.");
            logger.Log($"Путь выгрузки: {rtaExportedCardsDirectory}.");

            while (currentStartDate <= setting.dateEnd)
            {
                Thread.Sleep(reqDelay);
                await DownloadCardsByPeriod(currentStartDate, currentEndDate);

                currentStartDate = currentStartDate.AddMonths(1);
                currentEndDate = GetEndOfMonthDate(currentStartDate);
            }
        }

        private async Task DownloadCardsByPeriod(DateOnly startDate, DateOnly endDate)
        {
            logger.Log($"Подпериод {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}");

            var dataPackNum = await GenerateCardsByPeriod(startDate, endDate);
            if (dataPackNum == null)
            {
                logger.Log($"ОШИБКА генерации пакета карточек!!!");
                return;
            }
            logger.Log($"ОК пакет карточек сгенерирован: {dataPackNum}.");


            if (!await ExportCardsGeneratedPack(dataPackNum, startDate, endDate))
            {
                logger.Log($"ОШИБКА не удалось скачать сгенерированный пакет карточек!!!");
                return;
            }
            logger.Log($"ОК пакет карточек выгружен.");
        }

        private async Task<bool> ExportCardsGeneratedPack(string dataPackNum, DateOnly startDate, DateOnly endDate)
        {
            var tmpPackZipPath = $"./{dataPackNum}.zip";
            using (var client = new HttpClient())
            {
                await client.DownloadFile($"{urlRtaCardsExport}?data={dataPackNum}", tmpPackZipPath);
            }

            if (!File.Exists(tmpPackZipPath))
            {
                return false;
            }

            using (ZipArchive archive = ZipFile.OpenRead(tmpPackZipPath))
            {
                var counter = 0;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith($".{downloadFormat}", StringComparison.OrdinalIgnoreCase))
                    {
                        counter++;
                        entry.ExtractToFile($"{rtaExportedCardsDirectory}/" +
                            $"{startDate:ddMMyyyy}_{endDate:ddMMyyyy}_{counter}.{downloadFormat}");
                    }
                }
            }

            File.Delete(tmpPackZipPath);
            return true;
        }

        private async Task<string?> GenerateCardsByPeriod(DateOnly startDate, DateOnly endDate)
        {
            string? rtaCardsPackNum = null;
            var requestData = $"{{\"data\":\"{{" +
                $"\\\"date_st\\\":\\\"{startDate.ToString("dd/MM/yyyy")}\\\"," +
                $"\\\"date_end\\\":\\\"{endDate.ToString("dd/MM/yyyy")}\\\"," +
                $"\\\"ParReg\\\":\\\"877\\\"," +
                $"\\\"order\\\":{{\\\"type\\\":1,\\\"fieldName\\\":\\\"dat\\\"}}," +
                $"\\\"reg\\\":[\\\"40\\\"]," +
                $"\\\"ind\\\":\\\"161\\\"," +
                $"\\\"exportType\\\":1}}\"}}";

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(
                    urlRtaCardsGenerate,
                    new StringContent(requestData, Encoding.UTF8, "application/json"));

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return null;
                }

                string content = await response.Content.ReadAsStringAsync();
                var respData = JsonConvert.DeserializeObject<Models.CardsGenerateResponse>(content);
                rtaCardsPackNum = respData?.data;
            }

            return rtaCardsPackNum;
        }

        private DateOnly GetEndOfMonthDate(DateOnly date)
            => date.AddDays(DateTime.DaysInMonth(date.Year, date.Month) - 1);

        private void ClearDirectory(string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }

            Directory.CreateDirectory(dir);
        }
    }
}
