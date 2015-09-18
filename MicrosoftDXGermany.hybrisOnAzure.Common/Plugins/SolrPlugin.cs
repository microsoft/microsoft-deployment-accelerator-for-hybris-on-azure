// Microsoft Deployment Accelerator for hybris on Azure - sample code
// Copyright (c) Microsoft Corporation
// see LICENSE.txt for license information

using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.AccessControl;
using System.Text;
using tangible.Azure.AdditionalConfiguration;
using tangible.Azure.Auxiliary;
using tangible.Azure.Auxiliary.VolumeManagement;
using tangible.Azure.Base;
using tangible.Azure.Tracing;

namespace MicrosoftDXGermany.hybrisOnAzure.Plugins
{
    public class SolrPlugin : IAzurePlugin
    {
        // Path where Hybris has been downloaded to
        public static string BaseDirectory = Environment.ExpandEnvironmentVariables(RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.BaseDirectory"));
        public static string JavaHomeDirectory = Environment.ExpandEnvironmentVariables(RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.JavaHomeDirectory"));
        public static int JavaProcessShutdownWaitMinutes = int.Parse(RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.JavaProcessShutdownWaitMinutes"));
        public static bool IsBackOfficeWorker = RoleEnvironment.CurrentRoleInstance.Role.Name.ToLower().Contains("backoffice");

        public bool Initialize()
        {
            Trace.TraceInformation("SolrPlugin: Initializing.");
            this.Status = AzurePluginStatus.Initializing;
            this.StatusMessage = string.Empty;

            // 1. Create necessary directories for usage by solr
            ///// SolrJavaProcessHostRolePlugin.cs Line 61
            #region Directories
            string solrServerDirectory = Path.Combine(BaseDirectory, "solr");

            string workBaseDirectory = null;
            try
            {
                workBaseDirectory = RoleEnvironment.GetLocalResource("HybrisOnAzure.WorkBase").RootPath;
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Error initializing: Could not retrieve local resource for workBaseDirectory. " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error initializing: Could not retrieve local resource for workBaseDirectory. ";
                return false;
            }
            //    a directory for "working" purposes mapped to a drive letter
            string workDrive = null;
            try
            {
                Trace.TraceInformation("SolrPlugin: Initializing work-Drive.");
                if (!Directory.Exists(Path.Combine(workBaseDirectory, "work")))
                    Directory.CreateDirectory(Path.Combine(workBaseDirectory, "work"));
                workDrive = DrivePathManager.Map(Path.Combine(workBaseDirectory, "work"), "Work");
                if (!workDrive.EndsWith("\\")) workDrive = workDrive + "\\";
                Trace.TraceInformation("SolrPlugin: mapped work directory to " + workDrive);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Error initializing: Could not map work drive. " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error initializing: Could not map work drive.";
                return false;
            }

            //    a directory for "temp" purposes mapped to a drive letter
            string tempDrive = null;
            try
            {
                Trace.TraceInformation("SolrPlugin: Initializing temp-Drive.");
                if (!Directory.Exists(Path.Combine(workBaseDirectory, "temp")))
                    Directory.CreateDirectory(Path.Combine(workBaseDirectory, "temp"));
                tempDrive = DrivePathManager.Map(Path.Combine(workBaseDirectory, "temp"), "Temp");
                if (!tempDrive.EndsWith("\\")) tempDrive = tempDrive + "\\";
                Trace.TraceInformation("SolrPlugin: mapped temp directory to " + tempDrive);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Error initializing: Could not map temp drive. " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error initializing: Could not map temp drive.";
                return false;
            }

            //    a directory for "data" purposes mapped to a drive letter
            string dataDrive = null;
            try
            {
                Trace.TraceInformation("SolrPlugin: Initializing data-Drive.");
                if (!Directory.Exists(Path.Combine(workBaseDirectory, "data")))
                    Directory.CreateDirectory(Path.Combine(workBaseDirectory, "data"));
                dataDrive = DrivePathManager.Map(Path.Combine(workBaseDirectory, "data"), "Data");
                if (!dataDrive.EndsWith("\\")) dataDrive = dataDrive + "\\";
                Trace.TraceInformation("SolrPlugin: mapped data directory to " + dataDrive);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Error initializing: Could not map data drive. " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error initializing: Could not map data drive.";
                return false;
            }
            #endregion
            #region Subdirectories for direct use by solr
            Trace.TraceInformation("SolrPlugin: Initializing subdirectories.");
            string solrWorkDirectory = null;
            string solrTempDirectory = null;
            string sharedTempDirectory = null;
            string solrDataDirectory = null;
            string solrLogsDirectory = null;

            try
            {
                // Work Directory = Z:\solr
                solrWorkDirectory = Path.Combine(workDrive, "solr");
                if (!Directory.Exists(solrWorkDirectory))
                    Directory.CreateDirectory(solrWorkDirectory);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Could not create directory " + solrWorkDirectory + ": " + ex.ToString());
            }
            try
            {
                // Temp Directory = Y:\solr
                solrTempDirectory = Path.Combine(tempDrive, "solr");
                if (!Directory.Exists(solrTempDirectory))
                    Directory.CreateDirectory(solrTempDirectory);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Could not create directory " + solrTempDirectory + ": " + ex.ToString());
            }
            try
            {
                // Shared Temp Directory = Y:\shared
                sharedTempDirectory = Path.Combine(tempDrive, "shared");
                if (!Directory.Exists(sharedTempDirectory))
                    Directory.CreateDirectory(sharedTempDirectory);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Could not create directory " + sharedTempDirectory + ": " + ex.ToString());
            }
            try
            {
                // Data Directory = S:\solr on BackOfficeWorker, X:\hybris otherwise
                if (IsBackOfficeWorker)
                {
                    var driveLetter = solrDataDirectory = RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.BackOfficeShare.DesiredDrive");
                    if (!driveLetter.EndsWith("\\")) driveLetter = driveLetter + "\\";
                    solrDataDirectory = Path.Combine(driveLetter, "solr");
                }
                else
                    solrDataDirectory = Path.Combine(dataDrive, "solr");
                if (!Directory.Exists(solrDataDirectory))
                    Directory.CreateDirectory(solrDataDirectory);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Could not create directory " + solrDataDirectory + ": " + ex.ToString());
            }
            try
            {
                // Logs Directory = X:\logs\solr
                solrLogsDirectory = Path.Combine(dataDrive, "logs", "solr");
                if (!Directory.Exists(solrLogsDirectory))
                    Directory.CreateDirectory(solrLogsDirectory);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Could not create directory " + solrDataDirectory + ": " + ex.ToString());
            }
            #endregion

            // 2. Patch SOLR commands
            #region start/stop command script
            // Read the standard PATH environment variable
            string searchPath = Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Machine);

            // Patch SOLR start/stop command script
            Trace.TraceInformation("SolrPlugin: Creating start/stop command script.");
            string solrCommandFileName = "solrserver.bat";
            try
            {
                string solrCommandFileContent = File.ReadAllText(Path.Combine(solrServerDirectory, solrCommandFileName));
                var backOfficeInstance = RoleEnvironment.Roles["MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole"].Instances.FirstOrDefault();
                if (backOfficeInstance == null)
                    throw new NullReferenceException("No BackOfficeWorker instance found.");
                IPEndPoint masterIPEndpoint = backOfficeInstance.InstanceEndpoints["Solr"].IPEndpoint; // CloudEnvironment.Roles[RoleNames.BackOfficeWorker].Instances.First().InstanceEndpoints[EndpointNames.BackOfficeWorker.Solr].IPEndpoint;
                IPEndPoint solrIPEndpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["Solr"].IPEndpoint;
                IPEndPoint solrStopIPEndpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["SolrStop"].IPEndpoint;
                solrCommandFileContent = solrCommandFileContent.Replace(@"{solrdir}", solrServerDirectory);
                solrCommandFileContent = solrCommandFileContent.Replace(@"{solrserverdir}", solrServerDirectory);
                solrCommandFileContent = solrCommandFileContent.Replace(@"{solrworkdir}", solrWorkDirectory);
                solrCommandFileContent = solrCommandFileContent.Replace(@"{solrtempdir}", solrTempDirectory);
                solrCommandFileContent = solrCommandFileContent.Replace(@"{solrdatadir}", solrDataDirectory);
                solrCommandFileContent = solrCommandFileContent.Replace(@"{solrlogsdir}", solrLogsDirectory);
                solrCommandFileContent = solrCommandFileContent.Replace(@"{solrport}", solrIPEndpoint.Port.ToString(CultureInfo.InvariantCulture));
                solrCommandFileContent = solrCommandFileContent.Replace(@"{solrmasterip}", masterIPEndpoint.Address.ToString());
                solrCommandFileContent = solrCommandFileContent.Replace(@"{solrmasterport}", masterIPEndpoint.Port.ToString(CultureInfo.InvariantCulture));
                solrCommandFileContent = solrCommandFileContent.Replace(@"{solrstopkey}", Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("=", ""));
                solrCommandFileContent = solrCommandFileContent.Replace(@"{solrstopport}", solrStopIPEndpoint.Port.ToString(CultureInfo.InvariantCulture));
                File.WriteAllText(Path.Combine(solrServerDirectory, solrCommandFileName), solrCommandFileContent);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Error creating solr start/stop command script: " + ex.Message);
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error creating solr start/stop command script.";
                return false;
            }
            #endregion

            #region start command
            // Build SOLR start command
            Trace.TraceInformation("SolrPlugin: Creating start command.");
            try
            {
                string solrStartCommandFileName = Path.Combine(solrServerDirectory, "solr-start.cmd");
                var solrStartCommandBuilder = new StringBuilder();
                solrStartCommandBuilder.AppendLine(@"@echo off");
                solrStartCommandBuilder.AppendLine(@"set path=" + Path.Combine(JavaHomeDirectory, @"bin") + ";" + searchPath);
                solrStartCommandBuilder.AppendLine(@"set java_home=" + JavaHomeDirectory);
                solrStartCommandBuilder.AppendLine(@"set temp=" + sharedTempDirectory);
                solrStartCommandBuilder.AppendLine(@"set tmp=" + sharedTempDirectory);
                solrStartCommandBuilder.AppendLine();
                solrStartCommandBuilder.AppendLine(@"set SOLR_DIR=" + solrServerDirectory);
                solrStartCommandBuilder.AppendLine(@"set SOLR_WORK_DIR=" + solrWorkDirectory);
                solrStartCommandBuilder.AppendLine(@"set SOLR_TEMP_DIR=" + solrTempDirectory);
                solrStartCommandBuilder.AppendLine(@"set SOLR_DATA_DIR=" + solrDataDirectory);
                solrStartCommandBuilder.AppendLine(@"set SOLR_LOGS_DIR=" + solrLogsDirectory);
                solrStartCommandBuilder.AppendLine();
                solrStartCommandBuilder.AppendLine(@"call " + Path.GetFileNameWithoutExtension(solrCommandFileName) + " start");
                File.WriteAllText(solrStartCommandFileName, solrStartCommandBuilder.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Error creating solr start command: " + ex.Message);
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error creating solr start command.";
                return false;
            }
            #endregion

            #region stop command
            // Build SOLR stop command
            Trace.TraceInformation("SolrPlugin: Creating stop command.");
            try
            {
                string solrStopCommandFileName = Path.Combine(solrServerDirectory, "solr-stop.cmd");
                var solrStopCommandBuilder = new StringBuilder();
                solrStopCommandBuilder.AppendLine(@"@echo off");
                solrStopCommandBuilder.AppendLine(@"set path=" + Path.Combine(JavaHomeDirectory, @"bin") + ";" + searchPath);
                solrStopCommandBuilder.AppendLine(@"set java_home=" + JavaHomeDirectory);
                solrStopCommandBuilder.AppendLine(@"set temp=" + sharedTempDirectory);
                solrStopCommandBuilder.AppendLine(@"set tmp=" + sharedTempDirectory);
                solrStopCommandBuilder.AppendLine();
                solrStopCommandBuilder.AppendLine(@"set SOLR_DIR=" + solrServerDirectory);
                solrStopCommandBuilder.AppendLine(@"set SOLR_WORK_DIR=" + solrWorkDirectory);
                solrStopCommandBuilder.AppendLine(@"set SOLR_TEMP_DIR=" + solrTempDirectory);
                solrStopCommandBuilder.AppendLine(@"set SOLR_DATA_DIR=" + solrDataDirectory);
                solrStopCommandBuilder.AppendLine(@"set SOLR_LOGS_DIR=" + solrLogsDirectory);
                solrStopCommandBuilder.AppendLine();
                solrStopCommandBuilder.AppendLine(@"call " + Path.GetFileNameWithoutExtension(solrCommandFileName) + " stop");
                File.WriteAllText(solrStopCommandFileName, solrStopCommandBuilder.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Error creating solr stop command: " + ex.Message);
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error creating solr stop command.";
                return false;
            }
            #endregion

            // 3. set full control for applications directory (otherwise java process cannot write to C:\Applications\solr\logs (whyever)
            #region directory permissions
            Trace.TraceInformation("SolrPlugin: Creating access rule for everyone to " + BaseDirectory);
            try
            {
                var security = Directory.GetAccessControl(BaseDirectory);
                var rule = new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.InheritOnly, AccessControlType.Allow);
                security.AddAccessRule(rule);
                Directory.SetAccessControl(BaseDirectory, security);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Error creating access rule for everyone: " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error creating access rule for everyone.";
                return false;
            }
            #endregion
            // 4. create C:\Applications\solr\server\logs directory (otherwise java process dies)
            #region logs dir
            Trace.TraceInformation("SolrPlugin: Creating logs dir because java ignors log-configuration.");
            try
            {
                var logsDir = Path.Combine(BaseDirectory, "solr", "server", "logs");
                if (!Directory.Exists(logsDir))
                    Directory.CreateDirectory(logsDir);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Error creating special log directory: " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error creating special log directory.";
                return false;

            }
            #endregion

            // 5. Get from additional settings if we may start the hybris process
            #region Additional configuration
            AdditionalConfigurationManager.Instance.AdditionalConfigurationChanged += Instance_AdditionalConfigurationChanged;
            try
            {
                Trace.TraceInformation("SolrPlugin: Processing initial additional configuration.");
                AdditionalConfigurationManager.Instance.ProcessConfiguration();
                Trace.TraceInformation("SolrPlugin: Successfully processed initial additional configuration.");
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Error processing initial additional configuration: " + ex.ToString());
            }
            try
            {
                Trace.TraceInformation("SolrPlugin", "Determining if Hybris was stopped before a reboot");
                var stopHybris = AdditionalConfigurationManager.Instance.GetCurrentConfigurationValue("StopHybris");
                if (stopHybris == null)
                    Trace.TraceAndLogWarning("SolrPlugin", "Determining if Hybris was stopped before a reboot resulted in a NULL value. Solr will be started.");
                else
                {
                    Trace.TraceInformation("SolrPlugin: Determining if Solr was stopped before a reboot resulted in the value: " + stopHybris);
                    this.ConfigStopSolr = bool.Parse(stopHybris);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("Solr", "Error getting from Additional configuration if processes are to be started: " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error getting Additional configuration";
                return false;
            }
            #endregion

            Trace.TraceInformation("SolrPlugin: Initialization done.");
            this.Status = AzurePluginStatus.NotStarted;
            this.StatusMessage = string.Empty;
            return true;
        }

        private void Instance_AdditionalConfigurationChanged(object sender, ConfigurationItemChangedEventArgs e)
        {
            if (e.Name == "StopHybris")
            {
                // this instance is told to change it's maintenance mode
                var stringValue = (e.ChangeType == ChangeType.Added | e.ChangeType == ChangeType.Modified) ? e.NewValue : e.OldValue;
                bool newValue;
                if (bool.TryParse(stringValue, out newValue))
                {
                    Trace.TraceAndLogInformation("SolrPlugin", "Additional configuration changed value StopHybris to " + newValue.ToString());

                    // enable or disable the Routing rule to the maintenance page
                    this.ConfigStopSolr = newValue;

                    // if hybris has to be stopped > stop solr
                    if (newValue && _SolrJavaProcess != null && !_SolrJavaProcess.HasExited)
                        SendStop();
                }
            }
        }

        private bool ConfigStopSolr { get; set; }

        private System.Diagnostics.Process _SolrJavaProcess;

        public bool IsAlive
        {
            get
            {
                // this Plugin is alive as long as the Solr process is running
                // or if the additional configuration says the process should not be running
                try
                {
                    return this.ConfigStopSolr || (_SolrJavaProcess != null && !_SolrJavaProcess.HasExited);
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Start()
        {
            // if SOLR is running we don't need to start again
            if (this.IsAlive) return true;

            this.Status = AzurePluginStatus.Starting;
            this.StatusMessage = string.Empty;
            try
            {
                // Prepare SOLR start
                string solrWorkingDirectory = Path.Combine(BaseDirectory, "solr");
                string solrStartCommandFileName = Path.Combine(solrWorkingDirectory, "solr-start.cmd");

                // Start SOLR process
                _SolrJavaProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        WorkingDirectory = solrWorkingDirectory,
                        FileName = solrStartCommandFileName,
                        Arguments = string.Empty,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    }
                };
                _SolrJavaProcess.OutputDataReceived += (s, a) => { if (!string.IsNullOrWhiteSpace(a.Data)) Trace.TraceVerbose("SolrPlugin: Solr process output: " + a.Data); };
                _SolrJavaProcess.ErrorDataReceived += (s, a) => { if (!string.IsNullOrWhiteSpace(a.Data)) Trace.TraceAndLogError("SolrPlugin", "Solr process error: " + a.Data); };
                _SolrJavaProcess.Exited += delegate(object sender, EventArgs e)
                {
                    if (!this.ConfigStopSolr)
                    {
                        Trace.TraceAndLogError("SolrPlugin", "Solr process exited! ExitCode " + (sender as System.Diagnostics.Process).ExitCode.ToString());
                        this.Status = AzurePluginStatus.Error;
                        this.StatusMessage = "Solr process exited!";
                    }
                    else
                    {
                        Trace.TraceAndLogWarning("SolrPlugin", "Solr process exited due to Additional Configuration change! ExitCode " + (sender as System.Diagnostics.Process).ExitCode.ToString());
                        this.Status = AzurePluginStatus.Warning;
                        this.StatusMessage = "Solr process exited!";
                    }
                };
                if (_SolrJavaProcess.Start())
                {
                    _SolrJavaProcess.BeginOutputReadLine();
                    _SolrJavaProcess.BeginErrorReadLine();
                }
                else
                {
                    Trace.TraceAndLogError("SolrPlugin", "Solr process could not be started!");
                    this.Status = AzurePluginStatus.ErrorStarting;
                    this.StatusMessage = "Solr process could not be started!";
                    return false;
                }
                Trace.TraceAndLogInformation("SolrPlugin", "Solr process started.");
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "Error while creating SolrJavaProcess: " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorStarting;
                this.StatusMessage = "Error while creating SolrJavaProcess.";
                return false;
            }

            this.Status = AzurePluginStatus.Healthy;
            this.StatusMessage = "Process ID: " + _SolrJavaProcess.Id.ToString();
            return true;
        }

        private int StopRetries = 6;
        private int StopRetryInterval = 1000 * 30; // 30 seconds
        public void SendStop()
        {
            this.Status = AzurePluginStatus.Stopping;

            // ADAPTING: Kill the Solr process
            Trace.TraceAndLogWarning("SolrPlugin", "SendStop: Kill solr child processes.");
            foreach (var childProcess in _SolrJavaProcess.GetChildProcesses(true).Reverse())
            {
                try
                {
                    childProcess.Kill();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("SolrPlugin: SendStop: Could not kill process with ID=" + childProcess.Id.ToString() + ": " + ex.ToString());
                }
            }
            try
            {
                Trace.TraceInformation("SolrPlugin: SendStop: Stopping SolrJavaProcess.");
                _SolrJavaProcess.Kill();
                Trace.TraceAndLogInformation("SolrPlugin", "SendStop: SolrJavaProcess killed.");
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("SolrPlugin", "SendStop: Could not kill SolrJavaProcess. " + ex.ToString());
            }

            if (!_SolrJavaProcess.HasExited)
                this.Status = AzurePluginStatus.Unknown;
            else
                this.Status = AzurePluginStatus.Stopped;


            if (this.ConfigStopSolr)
                this.StatusMessage = "Stopped due to additional configuration change.";
            else
                this.StatusMessage = "Stopped.";

            return;

            #region skipped
            //for (int i = 1; i <= this.StopRetries; i++)
            //{
            //    if (this.IsAlive)
            //    {
            //        // Prepare SOLR stop
            //        string solrWorkingDirectory = Path.Combine(BaseDirectory, "solr", "server");
            //        string solrStopCommandFileName = Path.Combine(solrWorkingDirectory, "solr-stop.cmd");

            //        System.Diagnostics.Process stopProcess = null;

            //        try
            //        {
            //            stopProcess = new System.Diagnostics.Process
            //            {
            //                StartInfo = new System.Diagnostics.ProcessStartInfo
            //                {
            //                    WorkingDirectory = solrWorkingDirectory,
            //                    FileName = solrStopCommandFileName,
            //                    Arguments = string.Empty,
            //                    UseShellExecute = false,
            //                    RedirectStandardOutput = true,
            //                    RedirectStandardError = true,
            //                    CreateNoWindow = true,
            //                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            //                }
            //            };
            //            stopProcess.OutputDataReceived += (s, a) => { if (!string.IsNullOrWhiteSpace(a.Data)) Trace.TraceVerbose("SolrPlugin: SendStop: Solr stop process output: " + a.Data); };
            //            stopProcess.ErrorDataReceived += (s, a) => { if (!string.IsNullOrWhiteSpace(a.Data)) Trace.TraceAndLogError("SolrPlugin", "SendStop: Solr stop process error: " + a.Data); };
            //            stopProcess.Exited += (s, a) => Trace.TraceInformation("SolrPlugin: SendStop: Solr stop process exited.");
            //            if (stopProcess.Start())
            //            {
            //                stopProcess.BeginOutputReadLine();
            //                stopProcess.BeginErrorReadLine();
            //            }

            //            // Wait up to XX minutes for solr process to exit
            //            Trace.TraceInformation(string.Format("SolrPlugin: SendStop: Waiting up to {0} minutes for solr process to exit.", JavaProcessShutdownWaitMinutes));
            //            if (!stopProcess.WaitForExit((int)TimeSpan.FromMinutes(JavaProcessShutdownWaitMinutes).TotalMilliseconds))
            //            {
            //                // the process did not exit after XX minutes
            //                // > force the process to exit
            //                Trace.TraceAndLogWarning("SolrPlugin", "SendStop: Kill solr platform process.");
            //                foreach (var childProcess in _SolrJavaProcess.GetChildProcesses(true).Reverse())
            //                {
            //                    try
            //                    {
            //                        childProcess.Kill();
            //                    }
            //                    catch (Exception ex)
            //                    {
            //                        Trace.TraceWarning("SolrPlugin: SendStop: Could not kill process with ID=" + childProcess.Id.ToString() + ": " + ex.ToString());
            //                    }
            //                }
            //                try
            //                {
            //                    _SolrJavaProcess.Kill();
            //                }
            //                catch (Exception ex)
            //                {
            //                    Trace.TraceWarning("SolrPlugin: SendStop: Could not kill SolrJavaProcess: " + ex.ToString());
            //                }
            //                try
            //                {
            //                    stopProcess.Kill();
            //                }
            //                catch (Exception ex)
            //                {
            //                    Trace.TraceWarning("SolrPlugin: SendStop: Could not kill stopProcess: " + ex.ToString());
            //                }
            //            }

            //            if (!stopProcess.HasExited)
            //            {
            //                Trace.TraceWarning("SolrPlugin: SendStop: StopProcess has not exited > kill it.");
            //                try
            //                {
            //                    stopProcess.Kill();
            //                }
            //                catch(Exception ex)
            //                {
            //                    Trace.TraceAndLogError("SolrPlugin", "SendStop: In retry " + i.ToString() + "Stop process did not exit and could not be killed. Exception: " + ex.ToString());
            //                }
            //            }
            //            if (_SolrJavaProcess.HasExited)
            //            {
            //                Trace.TraceInformation("SolrPlugin: SendStop: SolrJavaProcess exited.");
            //                if (!this.ConfigStopSolr)
            //                {
            //                    // we have not been shut down not due to an additional configuration change
            //                    this.Status = AzurePluginStatus.Stopped;
            //                    this.StatusMessage = string.Empty;
            //                }

            //                break;
            //            }
            //            else if (i == this.StopRetries)
            //            {
            //                Trace.TraceAndLogError("SolrPlugin", "SendStop: Final retry " + i.ToString() + ": SolrJavaProcess did not exit. Killing it.");
            //                try
            //                {
            //                    _SolrJavaProcess.Kill();
            //                    if (!this.ConfigStopSolr)
            //                    {
            //                        // we have not been shut down not due to an additional configuration change
            //                        this.Status = AzurePluginStatus.Stopped;
            //                        this.StatusMessage = string.Empty;
            //                    }
            //                }
            //                catch(Exception ex)
            //                {
            //                    Trace.TraceAndLogError("SolrPlugin", "SendStop: Final retry " + i.ToString() + ": Could not kill SolrJavaProcess.  Exception: " + ex.ToString());
            //                    this.Status = AzurePluginStatus.Error;
            //                    this.StatusMessage = "Could not kill Solr Process after final stop retry.";
            //                }
            //            }
            //        }
            //        catch (Exception ex)
            //        {
            //            Trace.TraceAndLogError("SolrPlugin", "Error stopping Solr Plugin: " + ex.ToString());
            //            this.Status = AzurePluginStatus.Unknown;
            //            this.StatusMessage = "Error stopping SolrPlugin";
            //        }
            //        System.Threading.Thread.Sleep(this.StopRetryInterval);
            //    }
            //}
            #endregion
        }

        public AzurePluginStatus Status { get; private set; }
        public string StatusMessage { get; private set; }
    }
}
