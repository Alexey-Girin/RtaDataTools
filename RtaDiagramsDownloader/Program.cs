using System.Configuration;
using System.Drawing;
using System.Security.Policy;
using CommandLine;

namespace RtaDiagramsDownloader;

public class Program
{
    public class Options
    {
        [Option("dateStart", Required = true, HelpText = "Дата начала периода выгрузки карточек ДТП. Формат MM-yyyy.")]
        public string? dateStart { get; set; }

        [Option("dateEnd", Required = true, HelpText = "Дата окончания периода выгрузки карточек ДТП. Формат MM-yyyy.")]
        public string? dateEnd { get; set; }
    }


    public static async Task Main(string[] args)
    {
        await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(async o =>
        {
            DateOnly dateStart;
            DateOnly dateEnd;

            var logger = new ConsoleLogger();

            if (o.dateStart != null && o.dateEnd != null)
            {
                if (!DateOnly.TryParse(o.dateStart, out dateStart))
                {
                    logger.Log($"Некорректный формат параметра dateStart.");
                    return;
                }

                if (!DateOnly.TryParse(o.dateEnd, out dateEnd))
                {
                    logger.Log($"Некорректный формат параметра dateEnd.");
                    return;
                }

                await DownloadDiagrams(new DownloadSetting(dateStart, dateEnd), logger);
            }
        });
    }

    private static async Task DownloadDiagrams(DownloadSetting setting, ILogger logger)
    {
        if (!CheckConfFile(logger))
        {
            return;
        }

        // Загрузка упрощенных карточек ДТП со схемами в формате xls.
        var downloader = new RtaCardsDownloader(FileFormat.Xls, setting, logger);
        await downloader.DownloadCards();

        // Извлечение схем ДТП из xls-файлов загруженных карточек.
        var extractor = new RtaDiagramExtractor(setting, logger);
        extractor.ExtractImagesFromFiles(FileFormat.Xls);

        // Загрузка полных карточек ДТП в формате xml.
        downloader.DownloadFormat = FileFormat.Xml;
        await downloader.DownloadCards();

        // Группировка извлеченных схем ДТП по их кодам.
        var diagramGrouper = new RtaDiagramGrouper(setting, logger);
        diagramGrouper.GroupDiagrams(FileFormat.Xml);
    }

    private static bool CheckConfFile(ILogger logger)
    {
        var confParamList = new List<string>() { 
            "urlRtaCardsGenerateXls",
            "urlRtaCardsGenerateXml",
            "urlRtaCardsExport"
        };

        var confIntParamList = new List<string>() {
            "rtaIdRow",
            "rtaIdColumn"
        };

        foreach (var confParam in confParamList.Union(confIntParamList))
        {
            if (ConfigurationManager.AppSettings[confParam] == null)
            {
                logger.Log($"Необходимо установить значение параметра {confParam} в App.config.");
                return false;
            }
        }

        foreach (var confParam in confIntParamList)
        {
            if (!int.TryParse(ConfigurationManager.AppSettings[confParam], out _))
            {
                logger.Log($"Необходимо установить числовое значение параметра {confParam} в App.config.");
                return false;
            }
        }

        return true;
    }
}
