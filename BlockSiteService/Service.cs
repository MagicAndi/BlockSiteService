using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using System.Configuration;

using NLog;
using BlockSiteService.Utilities;

namespace BlockSiteService
{
    public class Service
    {
        #region Private Data

        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Timer timer;
        #endregion

        #region Constructor(s)

        public Service()
        {
            timer = new Timer(1000 * 60) { AutoReset = true };
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
        }

        #endregion

        #region Service Methods

        public void Start()
        {
            timer.Start();
            logger.Info(string.Format("{0} is starting at {1}.", AppScope.Configuration.ApplicationTitle, DateTime.Now));
        }

        public void Stop()
        {
            timer.Stop();
            logger.Info(string.Format("{0} is stopping at {1}.", AppScope.Configuration.ApplicationTitle, DateTime.Now));
        }

        public void OnTimer(object sender, ElapsedEventArgs e)
        {
            logger.Trace(LogHelper.BuildMethodEntryTrace());

            try
            {
                if (! Directory.Exists(AppScope.Configuration.HostsFolderPath))
                {
                    var message = string.Format("The configuration value 'HostsFolderPath' is invalid: '{0}'", 
                                                AppScope.Configuration.HostsFolderPath);
                    throw new ConfigurationErrorsException(message);
                }

                var hostsFilePath = Path.Combine(AppScope.Configuration.HostsFolderPath, "hosts");
                logger.Debug("Hosts file path: " + hostsFilePath);
                var hostsFile = new FileInfo(hostsFilePath);

                if (!hostsFile.Exists)
                {
                    logger.Error("Unable to find the hosts file at '{0}'.", hostsFilePath);
                    return;
                }

                var backupFilePath = Path.Combine(AppScope.Configuration.HostsFolderPath, "hosts.bak");
                logger.Debug("Hosts backup file path: " + backupFilePath);
                var backupFile = new FileInfo(backupFilePath);

                if (!backupFile.Exists)
                {
                    logger.Info("Unable to find the hosts backup file at '{0}'.", backupFilePath);

                    // Copy the Hosts file and make it read-only
                }



            }
            catch (Exception ex)
            {
                // Console.WriteLine("The process failed: {0}", e.ToString());
            }

            finally { }


            // Get the Hosts backup file
            // Set to read-only

            // Get HOSTS file
            // Check if modified
            // If modified, copy the backup file
            // Set hosts file as readonly

            logger.Trace(LogHelper.BuildMethodExitTrace());
        }

        #endregion
    }
}