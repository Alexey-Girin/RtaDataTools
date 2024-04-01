namespace RtaDiagramsDownloader.Models
{
    public class RtaGroupRepresentative
    {
        public string RtaNumSamplePath { get; set; } = string.Empty;

        public int Counter { get; set; } = 0;

        public RtaGroupRepresentative(string rtaNumSamplePath) 
        {
            RtaNumSamplePath = rtaNumSamplePath;
        }
    }
}
