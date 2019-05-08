﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Timers;

using NLog;

using BlockSiteService.Utilities;
using System.Collections.Generic;

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

                if (hostsFile.LastWriteTime < DateTime.Now.AddDays(-AppScope.Configuration.MaxAgeOfHostsFileInDays))
                {
                    RebuildHostsFile();
                    FlushDns();
                }
                else if (!hostsFile.IsReadOnly)
                {
                    CloseBrowser();
                    RebuildHostsFile();
                    FlushDns();
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
            logger.Trace(LogHelper.BuildMethodEntryTrace());

            string installFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var hostsTemplateFile = new FileInfo(Path.Combine(hostsFolderPath, "HostsTemplate.txt"));

            if (!hostsTemplateFile.Exists)
            {
                logger.Warn("Unable to find the hosts template file at '{0}'.", hostsTemplateFile);
                return;
            }

            var timestamp = DateTime.Now.ToString("ddMMyyyy_HHmmss");
            var temporaryFilePath = Path.Combine(installFolder, string.Format("HostsDownload-{0}.txt", timestamp));
            var updatedHostsFilePath = Path.Combine(installFolder, string.Format("UpdatedHosts-{0}.txt", timestamp));
            
            hostsTemplateFile.CopyTo(updatedHostsFilePath);

            WebClient webClient = new WebClient();
            webClient.DownloadFile(AppScope.Configuration.HostsFileSourceUrl, temporaryFilePath);

            using (Stream blockedDomains = File.OpenRead(temporaryFilePath))
            {
                using (Stream updatedHostsFile = new FileStream(updatedHostsFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    blockedDomains.CopyTo(updatedHostsFile);
                }
            }

            WhitelistDomains(updatedHostsFilePath);

            var currentHostsFile = new FileInfo(Path.Combine(hostsFolderPath, "hosts"));
            File.Copy(currentHostsFile.FullName, Path.Combine(hostsFolderPath, string.Format("hosts-{0}.bak", timestamp)));
            var hostsFilePath = currentHostsFile.FullName;
            currentHostsFile.IsReadOnly = false;
            currentHostsFile.Delete();

            File.Copy(updatedHostsFilePath, hostsFilePath, true);

            // Set file to read only
            currentHostsFile = new FileInfo(hostsFilePath);
            currentHostsFile.IsReadOnly = true;

            // Delete temporary files
            File.Delete(temporaryFilePath);
            File.Delete(updatedHostsFilePath);

            logger.Trace(LogHelper.BuildMethodExitTrace());
        }

        private void WhitelistDomains(string updatedHostsFilePath)
        {
            var whitelistFile = new FileInfo(Path.Combine(hostsFolderPath, "Whitelist.txt"));

            if (whitelistFile.Exists)
            {
                var domains = File.ReadLines(whitelistFile.FullName);
                domains = domains.Where(d => !d.StartsWith("#"));

                foreach (var domain in domains)
                {
                    var lines = File.ReadLines(updatedHostsFilePath);
                    var updatedContent = new List<string>();

                    foreach (var line in lines)
                    {
                        if (line.Contains(domain) && (!line.StartsWith("#")))
                        {
                            updatedContent.Add("# " + line);
                        }
                        else
                        {
                            updatedContent.Add(line);
                        }
                    }

                    File.WriteAllLines(updatedHostsFilePath, updatedContent);
                }
            }
        }


        private void FlushDns()
        {
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = "ipconfig.exe";
            process.StartInfo.Arguments = "/flushdns";
            process.Start();
            process.WaitForExit();
            //string output = process.StandardOutput.ReadToEnd();
            //return output;
        }

        #endregion
    }
}