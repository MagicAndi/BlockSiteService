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

        private Logger logger = LogManager.GetCurrentClassLogger();
        private Timer timer;
        private string hostsFolderPath;
        #endregion

        #region Constructor(s)

        public Service()
        {
            timer = new Timer(1000 * AppScope.Configuration.PollIntervalInSeconds) { AutoReset = true };
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            
            var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            logger.Debug("System Path: '{0}'.", systemPath);
            hostsFolderPath = Path.Combine(systemPath, @"drivers\etc");
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
                // Debug
                CheckOpenedBrowserTabs();

                var hostsFilePath = Path.Combine(hostsFolderPath, "hosts");
                var hostsFile = new FileInfo(hostsFilePath);

                if (!hostsFile.Exists)
                {
                    logger.Error("Unable to find the hosts file at '{0}'.", hostsFilePath);
                    return;
                }

                var backupFilePath = Path.Combine(hostsFolderPath, "hosts.bak");
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
                    // Add a visible prompt telling yourself to stay focused!!

                    // Kill any open tabs taht use any blocked domains
                    // CheckOpenedBrowserTabs();

                    hostsFile.IsReadOnly = false;
                    File.WriteAllText(hostsFilePath, File.ReadAllText(backupFilePath));
                    logger.Info("Successfully reverted the changes to the HOSTS file.");
                }
                
                hostsFile.IsReadOnly = true;
                backupFile.IsReadOnly = true;
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

        private void CheckOpenedBrowserTabs()
        {
            // Tried
            //  - Selenium
            //  - DDE (no longer works in Firefox)

            // Other approaches:

            //  - UIAutomation 
            //      - http://hintdesk.com/c-list-all-opened-tabs-of-firefox-with-uiautomation/
            //      - http://stackoverflow.com/questions/15447518/c-sharp-get-url-from-firefox-but-dont-use-dde
            //      - https://www.codeproject.com/Articles/141842/Automate-your-UI-using-Microsoft-Automation-Framew
            //      - https://msdn.microsoft.com/en-us/library/ms747327.aspx

            //  - AutoIT
            //      - http://stackoverflow.com/questions/6810692/how-to-use-autoitx-in-net-c-without-registering
            //      - https://www.autoitscript.com/forum/topic/177167-using-autoitx-from-c-net/
            //      - https://github.com/OpenSharp/NAutoIt
            //      - https://github.com/search?l=C%23&q=autoit&type=Repositories&utf8=%E2%9C%93
            
            // - Miscellaneous
            //      -- https://github.com/TestStack/White
        }

        #endregion
    }
}