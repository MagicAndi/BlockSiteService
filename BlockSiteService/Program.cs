using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NLog;

namespace BlockSiteService
{
    class Program
    {
        #region Private Data

        private static Logger logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region Private Properties

        private static string ApplicationTitle
        {
            get { return AppScope.Configuration.ApplicationTitle; }
        }

        #endregion

        static void Main(string[] args)
        {
            ServiceConfiguration.Configure();

            // Global exception handler
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
        }
        
        /// <summary>
        /// Method to trap unhandled exceptions.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="UnhandledExceptionEventArgs"/> instance containing the event data.</param>
        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            logger.Error((Exception)e.ExceptionObject, 
                        string.Format("An unhandled exception has occurred in the {0}.", ApplicationTitle));
            Environment.Exit(1);
        }
    }
}