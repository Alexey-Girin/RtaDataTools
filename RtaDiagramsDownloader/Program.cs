// See https://aka.ms/new-console-template for more information

using rtadatatool;

internal class Program
{
    static async Task Main()
    {
        var a = new RtaCardsDownloader();
        await a.DownloadCards();
        
    }
}

public static class Extensions
{
    public static async Task DownloadFile(this HttpClient client, string address, string fileName)
    {
        using (var response = await client.GetAsync(address))
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var file = System.IO.File.OpenWrite(fileName))
        {
            stream.CopyTo(file);
        }
    }
}