

namespace TgBotParserAli.Models
{
    internal class EpnApiResponse
    {
        public string IdentifiedAs { get; set; }
        public string Error { get; set; }
        public Dictionary<string, SearchResult> Results { get; set; }
    }
}
