namespace RtaDiagramsDownloader
{
    public class ConsoleLogger : ILogger
    {
        public ConsoleLogger() { }

        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
