using CommandLine.Text;
using CommandLine;
using Newtonsoft.Json;
using System.Drawing;
using System.Xml;
using RtaDiagramsDownloader.Models;

namespace RtaDiagramsDownloader
{
    public class RtaDiagramGrouper
    {
        private string rtaDiagramDirectoryByGroups;

        private string rtaFullCardsDirectory;

        private DownloadSetting setting;

        private ILogger logger;

        private Dictionary<string, List<string>> rtaDiagramCollectionByCode = new Dictionary<string, List<string>>();

        public RtaDiagramGrouper(DownloadSetting setting, ILogger logger)
        {
            this.setting = setting;
            this.logger = logger;

            rtaDiagramDirectoryByGroups = @$"./{setting.DirectoryDiagramsByGroups}";
            rtaFullCardsDirectory = @$"./{setting.DirectoryExportedCardsXml}";
        }

        public void GroupDiagrams(FileFormat fileFormat)
        {
            if (fileFormat != FileFormat.Xml)
            {
                logger.Log("ОШИБКА. Группировка схем по кодам доступна только с использованием xml-карточек.");
            }

            logger.Log("Группировка схем ДТП.");
            logger.Log($"Путь для сгруппированных схем ДТП: {rtaDiagramDirectoryByGroups}");

            GroupDiagramsViaXml();

            logger.Log($"Схемы ДТП по кодам собраны. К-во: {rtaDiagramCollectionByCode.Count}");
        }

        private string FindGroupRepresentative(string groupPath)
        {
            var representatives = new Dictionary<string, RtaGroupRepresentative>();
            var files = Directory.GetFiles(groupPath);
            foreach (var file in files)
            {
                if (file == null)
                {
                    continue;
                }

                var hash = GetImageHash(new Bitmap(file));
                if (representatives.ContainsKey(hash))
                {
                    representatives[hash].Counter += 1;
                    continue;
                }
                
                representatives.Add(hash, new RtaGroupRepresentative(file));
            }
    
            if (representatives.Count == 0)
            {
                return string.Empty;
            }

            return representatives.MaxBy(e => e.Value.Counter).Value.RtaNumSamplePath;
        }

        private void GroupDiagramsViaXml()
        {
            if (!Directory.Exists(rtaFullCardsDirectory))
            {
                logger.Log($"ОШИБКА. Не удалось найти директорию с полными карточками: {rtaFullCardsDirectory}.");
                return;
            }

            var files = Directory.GetFiles(rtaFullCardsDirectory);
            foreach (var xmlFilePath in files)
            {
                if (!File.Exists(xmlFilePath))
                {
                    logger.Log($"ОШИБКА. Не удалось найти файл с полными карточками: {xmlFilePath}.");
                    continue;
                }

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFilePath);
                if (xmlDoc == null)
                {
                    logger.Log($"ОШИБКА. Не удалось открыть файл с полными карточками: {xmlFilePath}.");
                    continue;
                }

                var tabNodes = xmlDoc.SelectNodes("//tab");
                if (tabNodes == null)
                {
                    logger.Log($"ОШИБКА формата. Не удалось распарсить файл с полными карточками: {xmlFilePath}.");
                    continue;
                }

                foreach (XmlNode tabNode in tabNodes)
                {
                    var emtpNumber = tabNode.SelectSingleNode("EMTP_NUMBER")?.InnerText;
                    var diagramCode = tabNode.SelectSingleNode("infoDtp/s_dtp")?.InnerText;

                    if (string.IsNullOrEmpty(emtpNumber) || string.IsNullOrEmpty(diagramCode))
                    {
                        continue;
                    }

                    if (rtaDiagramCollectionByCode.ContainsKey(diagramCode))
                    {
                        if (rtaDiagramCollectionByCode[diagramCode].Find(e => e == emtpNumber) != null)
                        {
                            continue;
                        }

                        rtaDiagramCollectionByCode[diagramCode].Add(emtpNumber);
                        continue;
                    }

                    rtaDiagramCollectionByCode.Add(diagramCode, new List<string>() { emtpNumber });
                }
            }
            
            CollectRtaDiagramsByGroup();
        }

        private void CollectRtaDiagramsByGroup()
        {
            RecreateDir(rtaDiagramDirectoryByGroups);
            var resultPath = @$"{rtaDiagramDirectoryByGroups}/000_GROUP_RESULT";
            Directory.CreateDirectory(resultPath);

            foreach (var diagrams in rtaDiagramCollectionByCode)
            {
                var groupPath = @$"{rtaDiagramDirectoryByGroups}/{diagrams.Key}";
                Directory.CreateDirectory(groupPath);

                foreach (var diagram in diagrams.Value)
                {
                    var initDgFile = getInitDgPath(diagram);
                    if (!File.Exists(initDgFile))
                    {
                        continue;
                    }

                    File.Copy(initDgFile, $"{rtaDiagramDirectoryByGroups}/{diagrams.Key}/{diagram}.png");
                }

                var represPath = FindGroupRepresentative(groupPath);
                if (represPath == null)
                {
                    continue;
                }

                File.Copy(represPath, @$"{resultPath}/{diagrams.Key}.png");
            }
        }

        private string getInitDgPath(string fileName) 
            => $"./{setting.DirectoryDiagramsByCards}/{fileName}.png";

        private void RecreateDir(string dir)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
            Directory.CreateDirectory(dir);
        }

        private string GetImageHash(Bitmap bmpSource)
        {
            var lResult = string.Empty;

            var bmpMin = new Bitmap(bmpSource, new Size(16, 16));
            for (int j = 0; j < bmpMin.Height; j++)
            {
                for (int i = 0; i < bmpMin.Width; i++)
                {
                    var pixelBl = bmpMin.GetPixel(i, j).GetBrightness() < 0.5f;
                    lResult += pixelBl ? '1' : '0';
                }
            }

            return lResult;
        }
    }
}