using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Westwind.Utilities.Configuration;

namespace BlockSiteService
{
    public class ApplicationConfiguration : AppConfiguration
    {
        #region Public Properties

        public string ApplicationTitle { get; set; }
        public int PollIntervalInSeconds { get; set; }
        public bool KillBrowser { get; set; }
        public bool ForceShutdown { get; set; }
        public string BrowserType { get; set; }
        public bool CleanLogFiles { get; set; }
        public int MaxAgeOfLogFilesInDays { get; set; }

        #endregion

        #region Constructor

        public ApplicationConfiguration()
        {
        }

        #endregion        
    }
}