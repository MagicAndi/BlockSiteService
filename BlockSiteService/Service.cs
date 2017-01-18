using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using System.Configuration;
using System.Security.Cryptography;

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
                    logger.Warn("Unable to find the hosts backup file at '{0}'.", backupFilePath);
                    hostsFile.CopyTo(backupFilePath);
                    backupFile = new FileInfo(backupFilePath);
                    backupFile.IsReadOnly = true;
                    return;
                }

                if(! CheckFilesAreEqual(hostsFile, backupFile))
                {
                    // If modified, copy the backup file
                    var timeStampFilePath = string.Format("{0}_Deleted_{1}.bak", hostsFile.FullName, DateTime.Now.ToString("ddMMyyyy_HHmm"));
                    hostsFile.CopyTo(timeStampFilePath);
                    backupFile.CopyTo(hostsFilePath, true);

                    // Set hosts file as readonly
                    hostsFile = new FileInfo(hostsFilePath);
                    hostsFile.IsReadOnly = true;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Service failed when preventing changes to HOSTS file.");
            }            


            logger.Trace(LogHelper.BuildMethodExitTrace());
        }

        #endregion

        #region Private Methods
        
        private bool CheckFilesAreEqual(FileInfo fileInfo1, FileInfo fileInfo2)
        {
            if (fileInfo1.Length != fileInfo2.Length)
            {
                return false;
            }
            
            if (fileInfo1.LastWriteTime != fileInfo2.CreationTime)
            {
                return false;
            }

            byte[] file1 = File.ReadAllBytes(fileInfo1.FullName);
            byte[] file2 = File.ReadAllBytes(fileInfo2.FullName);

            if (file1.Length == file2.Length)
            {
                for (int i = 0; i < file1.Length; i++)
                {
                    if (file1[i] != file2[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
        
        #endregion
    }
}