using System.Drawing.Imaging;
using Spire.Xls;
using System.Configuration;

namespace rtadatatool
{
    public class RtaDiagramExtractor
    {
        private const string rtaDiagramDirectory = @".\RtaDiagrams";

        private Tuple<int, int> rtaIdXlsCell;

        public List<string> RtaIdWithoutDiagramList { get; set; } = new List<string>();

        public List<string> RtaIdDiagramConfuseList { get; set; } = new List<string>();

        public RtaDiagramExtractor()
        {
            var rtaIdRow = ConfigurationManager.AppSettings["rtaIdRow"];
            var rtaIdCol = ConfigurationManager.AppSettings["rtaIdColumn"];

            if (rtaIdRow == null || rtaIdCol == null)
            {
                throw new Exception("Ошибка! Необходимо заполнить поля rtaIdRow и rtaIdColumn в config.");
            }

            try
            {
                rtaIdXlsCell = new Tuple<int, int>(int.Parse(rtaIdRow), int.Parse(rtaIdCol));
            }
            catch (Exception)
            {
                throw new Exception("Ошибка заполнения rtaIdRow и rtaIdColumn в config. Требуется число.");
            }
        }

        public void ExtractImagesFromXls(string xlsPath)
        {
            var book = new Workbook();

            try
            {
                book.LoadFromFile(xlsPath);
            }
            catch (FileNotFoundException)
            {
                throw new Exception($"Ошибка! Не удалось найти xls-файл: {xlsPath}.");
            }

            if (!Directory.Exists(rtaDiagramDirectory)) 
            {
                Directory.CreateDirectory(rtaDiagramDirectory);
            }

            foreach (Worksheet sheet in book.Worksheets)
            {
                var rtaId = (string)sheet.GetCalculateValue(rtaIdXlsCell.Item1, rtaIdXlsCell.Item2);

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

                sheet.Pictures[0].Picture.Save(@$"{rtaDiagramDirectory}\{rtaId}.png", ImageFormat.Png);
            }
        }
    }
}