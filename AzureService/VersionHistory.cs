using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

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
        public VersionStatus Status { get; set; }
        public string[] Changes { get; set; }
    }
}
