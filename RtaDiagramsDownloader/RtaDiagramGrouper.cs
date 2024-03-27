using System.Xml;

namespace RtaDiagramsDownloader
{
    public class RtaDiagramGrouper
    {
        private const string rtaDiagramDirectory = @"./RtaDiagramByGroups";

        private Dictionary<string, List<string>> dic = new Dictionary<string, List<string>>(); 

        public RtaDiagramGrouper() { }

        public void GroupDiagrams(string dirPath)
        {
            var files = Directory.GetFiles(dirPath);

            foreach (var xmlFilePath in files)
            {
                List<Tuple<string, string>> dtpDataList = new List<Tuple<string, string>>();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xmlFilePath);

                XmlNodeList tabNodes = xmlDoc.SelectNodes("//tab");

                foreach (XmlNode tabNode in tabNodes)
                {
                    // Считываем значения EMTP_NUMBER и s_dtp
                    string emtpNumber = tabNode.SelectSingleNode("EMTP_NUMBER")?.InnerText;
                    string sDtp = tabNode.SelectSingleNode("infoDtp/s_dtp")?.InnerText;

                    if (string.IsNullOrEmpty(emtpNumber) || string.IsNullOrEmpty(sDtp))
                    {
                        continue;
                    }

                    if (dic.ContainsKey(sDtp))
                    {
                        dic[sDtp].Add(emtpNumber);
                        continue;
                    }

                    if (!File.Exists($"./RtaDiagramByCards/{emtpNumber}.png"))
                    {
                        continue;
                    }

                    dic.Add(sDtp, new List<string>() { emtpNumber });
                }
            }

            Replace();
        }

        private void Replace()
        {
            ClearDiagramByGroupsDirectory();

            Console.WriteLine($"Записей: {dic.Count}");
            foreach (var row in dic)
            {
                File.Copy($"./RtaDiagramByCards/{row.Value[0]}.png", $"{rtaDiagramDirectory}/{row.Key}.png");
            }
        }

        public void Correct(List<string> codes)
        {
            var listUpdated = new List<string>();
            var listSearchError = new List<string>();
            var listEmpty = new List<string>();

            foreach (var code in codes)
            {
                if (!dic.ContainsKey((string)code))
                {
                    listSearchError.Add(code);
                    continue;
                }

                dic[code].RemoveAt(0);

                if (dic[code].Count == 0)
                {
                    listEmpty.Add(code);
                    dic.Remove(code);
                    continue;
                }

                listUpdated.Add(code);
            }

            Replace();

            Console.WriteLine($"Следующие коды пропущены (не были найдены): {string.Join(",", listSearchError)}");
            Console.WriteLine($"Следующие коды удалены (нет корректных схем): {string.Join(",", listEmpty)}");
            Console.WriteLine($"Для следующих кодов обновлены схемы: {string.Join(",", listUpdated)}");
        }

        private void ClearDiagramByGroupsDirectory()
        {
            if (Directory.Exists(rtaDiagramDirectory))
            {
                Directory.Delete(rtaDiagramDirectory, true);
            }

            Directory.CreateDirectory(rtaDiagramDirectory);
        }
    }
}
