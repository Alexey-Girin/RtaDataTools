using RtaDiagramsDownloader.Models;
using System.Configuration;

public static class Extensions
{
    public static async Task DownloadFile(this HttpClient client, string address, string fileName)
    {
        using (var response = await client.GetAsync(address))
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var file = File.OpenWrite(fileName))
        {
            stream.CopyTo(file);
        }
    } 

    public static string? GetExportedCardsDirectoryByFormat(FileFormat fileFormat, DownloadSetting setting)
    {
        string? directory = null;

        switch (fileFormat)
        {
            case FileFormat.Xml:
                {
                    directory = setting.DirectoryExportedCardsXml;
                    break;
                }
            case FileFormat.Xls:
                {
                    directory = setting.DirectoryExportedCardsXls;
                    break;
                }
            default:
                {
                    break;
                }
        }

        return directory;
    }

    public static string? GetFileExtByFormat(FileFormat fileFormat)
    {
        string? formatExt = null;

        switch (fileFormat)
        {
            case FileFormat.Xml:
                {
                    formatExt = "xml";
                    break;
                }
            case FileFormat.Xls:
                {
                    formatExt = "xls";
                    break;
                }
            default:
                {
                    break;
                }
        }

        return formatExt;
    }

    public static string? GetCardsGenerateUrlByFormat(FileFormat fileFormat)
    {
        string? url = null;

        switch (fileFormat)
        {
            case FileFormat.Xml:
                {
                    url = ConfigurationManager.AppSettings["urlRtaCardsGenerateXml"]!.ToString();
                    break;
                }
            case FileFormat.Xls:
                {
                    url = ConfigurationManager.AppSettings["urlRtaCardsGenerateXls"]!.ToString();
                    break;
                }
            default:
                {
                    break;
                }
        }

        return url;
    }
}
