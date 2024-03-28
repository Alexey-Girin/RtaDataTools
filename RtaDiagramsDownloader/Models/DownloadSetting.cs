namespace RtaDiagramsDownloader
{
    public class DownloadSetting
    {
        public string DirectoryExportedCardsXml { get; private set; } = "RtaExportedCardsXml";

        public string DirectoryExportedCardsXls { get; private set; } = "RtaExportedCardsXls";

        /// <summary>
        /// Директория для схем по карточкам
        /// </summary>
        public string DirectoryDiagramsByCards { get; private set; } = "RtaDiagramByCards";

        /// <summary>
        /// Директория для схем по кодам (по всем схемам на каждый код)
        /// </summary>
        public string DirectoryDiagramsByGroups { get; private set; } = "RtaDiagramByGroups";


        private readonly DateOnly rtaMinDate = new DateOnly(2015, 1, 1);

        private readonly DateOnly rtaMaxDate = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, 1);

        public DateOnly dateStart { get; private set; }

        public DateOnly dateEnd { get; private set; }

        public DownloadSetting(DateOnly dateStart, DateOnly dateEnd)
        {
            this.dateStart = dateStart < rtaMinDate ? rtaMinDate : dateStart;
            this.dateEnd = dateEnd > rtaMaxDate ? rtaMaxDate : dateEnd;
        }
    }
}
