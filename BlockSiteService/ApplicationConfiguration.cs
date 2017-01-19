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

        #endregion

        #region Constructor

        public ApplicationConfiguration()
        {
        }

        #endregion        
    }
}