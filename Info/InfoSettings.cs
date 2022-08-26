using BuzzGUI.Common.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WDE.Info
{
    public class InfoSettings : Settings
    {
        [BuzzSetting(true, Description = "Show status bar.")]
        public bool ShowStatusBar { get; set; }
    }
}
