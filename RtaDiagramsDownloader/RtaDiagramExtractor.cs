using Spire.Xls;
using System.Configuration;

namespace RtaDiagramsDownloader
{
    public class RtaDiagramExtractor
    {
        private string rtaDiagramDirectory;

        private string directoryExportedCardsXls;

        private int emptyIdCounter = 0;

        private ILogger logger;

        private Tuple<int, int> rtaIdXlsCell;

        public List<string> RtaIdWithoutDiagramList { get; set; } = new List<string>();

        public List<string> RtaIdDiagramConfuseList { get; set; } = new List<string>();

        public RtaDiagramExtractor(DownloadSetting setting, ILogger logger)
        {
            this.logger = logger;

            rtaDiagramDirectory = @$"./{setting.DirectoryDiagramsByCards}";
            directoryExportedCardsXls = @$"./{setting.DirectoryExportedCardsXls}";

            var rtaIdRow = ConfigurationManager.AppSettings["rtaIdRow"]!;
            var rtaIdCol = ConfigurationManager.AppSettings["rtaIdColumn"]!;

            try
            {
                rtaIdXlsCell = new Tuple<int, int>(int.Parse(rtaIdRow), int.Parse(rtaIdCol));
            }
            catch (Exception)
            {
                throw new Exception("Ошибка заполнения rtaIdRow и rtaIdColumn в config. Требуется число.");
            }
        }

        public void ExtractImagesFromFiles(FileFormat fileFormat)
        {
            if (fileFormat != FileFormat.Xls)
            {
                logger.Log("ОШИБКА извлечения схем ДТП. Извлечение схем доступно для файлов формата xls!!!");
                return;
            }

            if (!Directory.Exists(directoryExportedCardsXls))
            {
                logger.Log($"ОШИБКА извлечения схем ДТП. Не удалось найти директорию с файлами карточек: " +
                    $"{directoryExportedCardsXls}.");
                return;
            }

            Reload();

            logger.Log($"Извлечение схем ДТП.");
            logger.Log($"Путь извлечения схем: {rtaDiagramDirectory}.");

            foreach (var file in Directory.GetFiles(directoryExportedCardsXls))
            {
                ExtractImagesFromXlsFile(file);
            }

            logger.Log($"Извлечение схем ДТП выполнено.");
            logger.Log($"Карточек ДТП с пустым ID: {emptyIdCounter}");
            logger.Log($"Карточек ДТП без схемы: {RtaIdWithoutDiagramList.Count}");
            logger.Log($"Карточек ДТП с несколькими изображениями (пропущены): " +
                $"{RtaIdDiagramConfuseList.Count}");
        }

        private void ExtractImagesFromXlsFile(string xlsPath)
        {
            var book = new Workbook();

            try
            {
                book.LoadFromFile(xlsPath);
            }
            catch (FileNotFoundException)
            {
                logger.Log($"ОШИБКА обработка файла карточек. Не удалось найти файл {xlsPath}.");
                return;
            }

            foreach (Worksheet sheet in book.Worksheets)
            {
                var rtaId = (string)sheet.GetCalculateValue(rtaIdXlsCell.Item1, rtaIdXlsCell.Item2);

                if (rtaId == null || rtaId == string.Empty)
                {
                    emptyIdCounter++;
                    continue;
                }

                if (sheet.Pictures.Count == 0)
                {
                    RtaIdWithoutDiagramList.Add(rtaId);
                    continue;
                }

                if (sheet.Pictures.Count > 1)
                {
                    RtaIdDiagramConfuseList.Add(rtaId);
                    continue;
                }

                sheet.Pictures[0].SaveToImage(@$"{rtaDiagramDirectory}/{rtaId}.png");
            }
        }

        private void Reload()
        {
            if (Directory.Exists(rtaDiagramDirectory))
            {
                Directory.Delete(rtaDiagramDirectory, true);
            }
            Directory.CreateDirectory(rtaDiagramDirectory);

            emptyIdCounter = 0;
            RtaIdWithoutDiagramList.Clear();
            RtaIdDiagramConfuseList.Clear();
        }
    }
}