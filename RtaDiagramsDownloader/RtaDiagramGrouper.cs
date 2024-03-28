using System.Xml;

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
            logger.Log($"Пусть для сгруппированных схем ДТП: {rtaDiagramDirectoryByGroups}");

            GroupDiagramsViaXml();

            logger.Log($"Схемы ДТП по кодам собраны. К-во: {rtaDiagramCollectionByCode.Count}");
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

            foreach (var diagrams in rtaDiagramCollectionByCode)
            {
                Directory.CreateDirectory(@$"{rtaDiagramDirectoryByGroups}/{diagrams.Key}");

                foreach (var diagram in diagrams.Value)
                {
                    var initDgFile = getInitDgPath(diagram);
                    if (!File.Exists(initDgFile))
                    {
                        continue;
                    }

                    File.Copy(initDgFile, $"{rtaDiagramDirectoryByGroups}/{diagrams.Key}/{diagram}.png");
                }
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
    }
}