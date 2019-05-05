using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Timers;

using NLog;

using BlockSiteService.Utilities;


namespace BlockSiteService
{
    public class Service
    {
        #region Private Data

        private Logger logger = LogManager.GetCurrentClassLogger();
        private System.Timers.Timer timer;
        private string hostsFolderPath;

        #endregion

        #region Constructor(s)

        public Service()
        {
            timer = new System.Timers.Timer(1000 * AppScope.Configuration.PollIntervalInSeconds) { AutoReset = true };
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            
            var systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
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
            CloseBrowser();
            timer.Stop();
            logger.Info(string.Format("{0} is stopping at {1}.", AppScope.Configuration.ApplicationTitle, DateTime.Now));
        }

        public void OnTimer(object sender, ElapsedEventArgs e)
        {
            logger.Trace(LogHelper.BuildMethodEntryTrace());

            try
            {               
                var hostsFilePath = Path.Combine(hostsFolderPath, "hosts");
                var hostsFile = new FileInfo(hostsFilePath);

                if (!hostsFile.Exists)
                {
                    logger.Error("Unable to find the hosts file at '{0}'.", hostsFilePath);
                    return;
                }

                if(hostsFile.LastWriteTime < DateTime.Now.AddDays(-AppScope.Configuration.MaxAgeOfHostsFileInDays))
                {
                    RebuildHostsFile();
                }
                else if (!hostsFile.IsReadOnly)
                {
                    CloseBrowser();
                    RebuildHostsFile();
                }

                CleanupLogFiles();
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Service failed when preventing changes to HOSTS file.");
            }

            logger.Trace(LogHelper.BuildMethodExitTrace());
        }
        
        #endregion

        #region Private Methods

        private void CleanupLogFiles()
        {
            if (!AppScope.Configuration.CleanLogFiles)
            {
                return;
            }

            var maxAgeOfLogFilesInDays = AppScope.Configuration.MaxAgeOfLogFilesInDays;

            if (maxAgeOfLogFilesInDays > 0)
            {
                var currentFolderPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                var currentFolder = new DirectoryInfo(currentFolderPath);
                var maxLogDate = DateTime.Now - new TimeSpan(maxAgeOfLogFilesInDays, 0, 0, 0);

                foreach (FileInfo file in currentFolder.GetFiles())
                {
                    if (file.Name.StartsWith("BlockSiteService_") && 
                        file.Name.EndsWith(".txt") && 
                        file.CreationTime < maxLogDate)
                    {
                        logger.Trace("Deleting log file '" + file.FullName + "'.");
                        file.Delete();
                    }
                }
            }
        }
        
        private void CloseBrowser()
        {
            if(!AppScope.Configuration.KillBrowser)
            {
                return;
            }

            var browserType = AppScope.Configuration.BrowserType;

            if (!string.IsNullOrEmpty(browserType))
            {
                Process[] processes = Process.GetProcessesByName(browserType);

                foreach (Process process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, string.Format("Unable to kill browser process {0} with PID {1}.", process.ToString(), process.Id));
                    }
                }
            }
        }

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

        private void RebuildHostsFile()
        {
            // See https://github.com/EddyErkel/Poweshell_updateHostsFile/blob/master/updateHostsFile.ps1
            // and https://github.com/robledosm/update-mvpsHosts/blob/master/update-mvpsHosts.ps1

            //var timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            //var temporaryFileName = string.Format("HostsDownload-{0}.txt", timestamp);

            //WebClient webClient = new WebClient();
            //webClient.DownloadFile(AppScope.Configuration.HostsFileSourceUrl, @"c:\temp\" + temporaryFileName);

        }

        #endregion
    }
}