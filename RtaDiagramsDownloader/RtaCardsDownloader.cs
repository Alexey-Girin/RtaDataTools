using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace rtadatatool
{
    public class RtaCardsDownloader
    {
        private const string rtaExportedCardsDirectory = @".\RtaExportedCards";

        private string urlRtaCardsGenerate;

        private string urlRtaCardsExport;

        private DateOnly rtaStartDate;

        private DateOnly rtaEndDate;

        private readonly DateOnly rtaMinDate = new DateOnly(2015, 1, 1);

        private readonly DateOnly rtaMaxDate = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);

        public RtaCardsDownloader()
        {
            try
            {
                urlRtaCardsGenerate = ConfigurationManager.AppSettings["urlRtaCardsGenerate"].ToString();
                urlRtaCardsExport = ConfigurationManager.AppSettings["urlRtaCardsExport"].ToString();
                rtaStartDate = new DateOnly(
                    int.Parse(ConfigurationManager.AppSettings["rtaStartYear"]),
                    int.Parse(ConfigurationManager.AppSettings["rtaStartMonth"]),
                    1);
                rtaEndDate = new DateOnly(
                    int.Parse(ConfigurationManager.AppSettings["rtaEndYear"]),
                    int.Parse(ConfigurationManager.AppSettings["rtaEndMonth"]),
                    1);
            }
            catch (Exception)
            {
                throw new Exception("Ошибка определение параметров выгрузки карточек ДТП.");
            }

            if (rtaStartDate < rtaMinDate)
            {
                rtaStartDate = rtaMinDate;
            }

            if (rtaEndDate > rtaMaxDate)
            {
                rtaEndDate = rtaMaxDate;
            }
        }

        public async Task DownloadCards()
        {
            var currentStartDate = rtaStartDate;
            var currentEndDate = GetEndOfMonthDate(currentStartDate);

            ClearExportedCardsDirectory();

            while (currentStartDate <= rtaEndDate)
            {
                Thread.Sleep(5000);
                await DownloadCardsByPeriod(currentStartDate, currentEndDate);

                currentStartDate = currentStartDate.AddMonths(1);
                currentEndDate = GetEndOfMonthDate(currentStartDate);
            }
        }

        private async Task DownloadCardsByPeriod(DateOnly startDate, DateOnly endDate)
        {
            var dataPackNum = await GenerateCardsByPeriod(startDate, endDate);
            await ExportCardsGeneratedPack(dataPackNum, startDate, endDate);
        }

        private async Task ExportCardsGeneratedPack(string dataPackNum, DateOnly startDate, DateOnly endDate)
        {
            using (var client = new HttpClient())
            {
                await client.DownloadFile(
                    $"{urlRtaCardsExport}?data={dataPackNum}", 
                    $"{rtaExportedCardsDirectory}/" +
                    $"{startDate.ToString("dd_MM_yyyy")}-{endDate.ToString("dd_MM_yyyy")}.zip");
            }
        }

        private async Task<string> GenerateCardsByPeriod(DateOnly startDate, DateOnly endDate)
        {
            var rtaCardsPackNum = string.Empty;
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

                Console.WriteLine($"Response status code: {response.StatusCode}");
                string content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response content: {content}");

                var respData = JsonConvert.DeserializeObject<Models.CardsGenerateResponse>(content);
                rtaCardsPackNum = respData.data;
            }

            return rtaCardsPackNum;
        }

        private DateOnly GetEndOfMonthDate(DateOnly date)
            => date.AddDays(DateTime.DaysInMonth(date.Year, date.Month) - 1);

        private void ClearExportedCardsDirectory()
        {
            if (Directory.Exists(rtaExportedCardsDirectory))
            {
                Directory.Delete(rtaExportedCardsDirectory, true);
            }

            Directory.CreateDirectory(rtaExportedCardsDirectory);
        }
    }
}
