using System;

namespace EQLogParser.Contracts
{
    public class DismissBuffRequest
    {
        public string PlayerName { get; set; } = string.Empty;
        public string BuffName { get; set; } = string.Empty;
        public DateTime Landed { get; set; }
    }
}
