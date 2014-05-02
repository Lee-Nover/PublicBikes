using System;

namespace Bicikelj.AzureService
{
    public enum VersionStatus
    {
        Development,
        Beta,
        Published
    }

    public class VersionHistory
    {
        public string Version { get; set; }
        public DateTime? DatePublished { get; set; }
        public VersionStatus Status { get; set; }
        public string[] Changes { get; set; }
    }
}
