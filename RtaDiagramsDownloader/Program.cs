using rtadatatool;
using RtaDiagramsDownloader;
using System.Collections;
using System.Configuration;
using CommandLine;
using RtaDiagramsDownloader.Models;
using Spire.Xls;

public class Program
{
    public class Options
    {
        [Option("dateStart", Required = true, HelpText = "Дата начала периода выгрузки карточек ДТП. Формат MM-yyyy.")]
        public string? dateStart { get; set; }

        [Option("dateEnd", Required = true, HelpText = "Дата окончания периода выгрузки карточек ДТП. Формат MM-yyyy.")]
        public string? dateEnd { get; set; }
    }

    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args).WithParsed(async o =>
        {
            DateOnly dateStart;
            DateOnly dateEnd;

            if (o.dateStart != null && o.dateEnd != null)
            {
                if (!DateOnly.TryParse(o.dateStart, out dateStart))
                {
                    Console.WriteLine($"Некорректный формат параметра dateStart.");
                    return;
                }

                if (!DateOnly.TryParse(o.dateEnd, out dateEnd))
                {
                    Console.WriteLine($"Некорректный формат параметра dateEnd.");
                    return;
                }

                await DownloadDiagrams(new DownloadSetting(dateStart, dateEnd));
            }
        });

        Console.ReadKey();
    }

    private static async Task DownloadDiagrams(DownloadSetting setting)
    {
        if (!CheckConfFile())
        {
            return;
        }

        var urlRtaCardsGenerateXls = ConfigurationManager.AppSettings["urlRtaCardsGenerateXls"]!.ToString();
        var urlRtaCardsGenerateXml = ConfigurationManager.AppSettings["urlRtaCardsGenerateXml"]!.ToString();
        var urlRtaCardsExport = ConfigurationManager.AppSettings["urlRtaCardsExport"]!.ToString();

        // Загрузка упрощенных карточек ДТП в формате xls.
        var downloaderXls = new RtaCardsDownloader(FileFormat.Xls, setting);
        await downloaderXls.DownloadCards();

        /*
        // Извлечение схем ДТП из xls-файлов загруженных карточек.
        var extractor = new RtaDiagramExtractor();
        foreach (var file in Directory.GetFiles(@$"./{setting.DirectoryExportedCardsXls}"))
        {
            extractor.ExtractImagesFromXls(file);
        }

        // Загрузка полных карточек ДТП в формате xml.
        var downloaderXml = new RtaCardsDownloader(urlRtaCardsGenerateXml, urlRtaCardsExport, "xml", setting);
        await downloaderXml.DownloadCards();
        */
        /*
        var diagramGrouper = new RtaDiagramGrouper();
        diagramGrouper.GroupDiagrams(@$"./RtaExportedCardsxml");

        while (true)
        {
            Console.WriteLine("Требуется корректировка? (да/нет)");
            var resp = Console.ReadLine();

            if (resp == "нет")
            {
                break;
            }

            if (resp != "да")
            {
                continue;
            }

            Console.WriteLine("Введите коды для корректировки (список через запятую):");
            var codes = Console.ReadLine().Split(",").ToList();
            diagramGrouper.Correct(codes);
        }
        */
    }

    private static bool CheckConfFile()
    {
        var confParamList = new List<string>() { 
            "urlRtaCardsGenerateXls",
            "directoryExportedCardsXml",
            "directoryDiagramsByCards",
            "directoryDiagramsByGroups"
        };

        foreach (var confParam in confParamList)
        {
            if (ConfigurationManager.AppSettings[confParam] == null)
            {
                PutErrorConfMsgToConsole("urlRtaCardsGenerateXml");
                return false;
            }
        }

        return true;
    }

    private static void PutErrorConfMsgToConsole(string confParam) 
        => Console.WriteLine($"Необходимо установить значение параметра {confParam} в App.config.");
}
