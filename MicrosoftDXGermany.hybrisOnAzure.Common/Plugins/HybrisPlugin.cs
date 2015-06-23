// Microsoft Deployment Accelerator for hybris on Azure - sample code
// Copyright (c) Microsoft Corporation
// see LICENSE.txt for license information

using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using tangible.Azure.Auxiliary.VolumeManagement;
using tangible.Azure.Base;
using tangible.Azure.Tracing;
using tangible.Azure.Auxiliary;
using tangible.Azure.AdditionalConfiguration;

namespace MicrosoftDXGermany.hybrisOnAzure.Plugins
{
    public class HybrisPlugin : IAzurePlugin
    {
        // Path where Hybris has been downloaded to
        public static string BaseDirectory = Environment.ExpandEnvironmentVariables(RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.BaseDirectory"));
        public static string JavaHomeDirectory = Environment.ExpandEnvironmentVariables(RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.JavaHomeDirectory"));
        public static int JavaProcessShutdownWaitMinutes = int.Parse(RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.JavaProcessShutdownWaitMinutes"));
        public static bool IsBackOfficeWorker = RoleEnvironment.CurrentRoleInstance.Role.Name.ToLower().Contains("backoffice");

        public bool Initialize()
        {
            Trace.TraceInformation("HybrisPlugin: Initializing.");
            this.Status = AzurePluginStatus.Initializing;
            this.StatusMessage = string.Empty;

            // 1. create necessary directories for usage by hybris
            #region Directories
            string workBaseDirectory = null;
            try
            {
                workBaseDirectory = RoleEnvironment.GetLocalResource("HybrisOnAzure.WorkBase").RootPath;
            }
            catch(Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error initializing: Could not retrieve local resource for workBaseDirectory. " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error initializing: Could not retrieve local resource for workBaseDirectory. ";
                return false;
            }

            //    a directory for "working" purposes mapped to a drive letter
            string workDrive = null;
            try
            {
                Trace.TraceInformation("HybrisPlugin: Initializing work-Drive.");
                if (!Directory.Exists(Path.Combine(workBaseDirectory, "work")))
                    Directory.CreateDirectory(Path.Combine(workBaseDirectory, "work"));
                workDrive = DrivePathManager.Map(Path.Combine(workBaseDirectory, "work"), "Work");
                if (!workDrive.EndsWith("\\")) workDrive = workDrive + "\\";
                Trace.TraceInformation("HybrisPlugin: mapped work directory to " + workDrive);
            }
            catch(Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error initializing: Could not map work drive. " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error initializing: Could not map work drive.";
                return false;
            }

            //    a directory for "temp" purposes mapped to a drive letter
            string tempDrive = null;
            try
            {
                Trace.TraceInformation("HybrisPlugin: Initializing temp-Drive.");
                if (!Directory.Exists(Path.Combine(workBaseDirectory, "temp")))
                    Directory.CreateDirectory(Path.Combine(workBaseDirectory, "temp"));
                tempDrive = DrivePathManager.Map(Path.Combine(workBaseDirectory, "temp"), "Temp");
                if (!tempDrive.EndsWith("\\")) tempDrive = tempDrive + "\\";
                Trace.TraceInformation("HybrisPlugin: mapped temp directory to " + tempDrive);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error initializing: Could not map temp drive. " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error initializing: Could not map temp drive.";
                return false;
            }

            //    a directory for "data" purposes mapped to a drive letter
            string dataDrive = null;
            try
            {
                Trace.TraceInformation("HybrisPlugin: Initializing data-Drive.");
                if (!Directory.Exists(Path.Combine(workBaseDirectory, "data")))
                    Directory.CreateDirectory(Path.Combine(workBaseDirectory, "data"));
                dataDrive = DrivePathManager.Map(Path.Combine(workBaseDirectory, "data"), "Data");
                if (!dataDrive.EndsWith("\\")) dataDrive = dataDrive + "\\";
                Trace.TraceInformation("HybrisPlugin: mapped data directory to " + dataDrive);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error initializing: Could not map data drive. " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error initializing: Could not map data drive.";
                return false;
            }
            #endregion
            #region Subdirectories for direct use by hybris
            Trace.TraceInformation("HybrisPlugin: Initializing subdirectories.");
            string hybrisWorkDirectory = null;
            string hybrisTempDirectory = null;
            string sharedTempDirectory = null;
            string hybrisDataDirectory = null;
            string hybrisLogsDirectory = null;
            try
            {
                // Work Directory = Z:\hybris
                hybrisWorkDirectory = Path.Combine(workDrive, "hybris");
                if (!Directory.Exists(hybrisWorkDirectory))
                    Directory.CreateDirectory(hybrisWorkDirectory);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Could not create directory " + hybrisWorkDirectory + ": " + ex.ToString());
            }
            try
            {
                // Temp Directory = Y:\hybris
                hybrisTempDirectory = Path.Combine(tempDrive, "hybris");
                if (!Directory.Exists(hybrisTempDirectory))
                    Directory.CreateDirectory(hybrisTempDirectory);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Could not create directory " + hybrisTempDirectory + ": " + ex.ToString());
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
                Trace.TraceAndLogError("HybrisPlugin", "Could not create directory " + sharedTempDirectory + ": " + ex.ToString());
            }
            try
            {
                // Data Directory = S:\hybris on BackOfficeWorker, X:\hybris otherwise
                if (IsBackOfficeWorker)
                {
                    var driveLetter = hybrisDataDirectory = RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.BackOfficeShare.DesiredDrive");
                    if (!driveLetter.EndsWith("\\")) driveLetter = driveLetter + "\\";
                    hybrisDataDirectory = Path.Combine(driveLetter, "hybris");
                }

                else
                    hybrisDataDirectory = Path.Combine(dataDrive, "hybris");
                if (!Directory.Exists(hybrisDataDirectory))
                    Directory.CreateDirectory(hybrisDataDirectory);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Could not create directory " + hybrisDataDirectory + ": " + ex.ToString());
            }
            try
            {
                // Logs Directory = X:\Logs\hybris
                hybrisLogsDirectory = Path.Combine(dataDrive, "logs", "hybris");
                if (!Directory.Exists(hybrisLogsDirectory))
                    Directory.CreateDirectory(hybrisLogsDirectory);
            }
            catch(Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Could not create directory " + hybrisLogsDirectory + ": " + ex.ToString());
            }
            #endregion


            // 2. Build hybris configuration files
            #region Calculates instance numbers
            string role = "";
            string rolenumber = "";
            Trace.TraceInformation("HybrisPlugin: Calculate instance numbers.");
            try
            {
                // This will have values like "BackOfficeWorker_IN_0" or "FrontendWorker_IN_0"
                string instanceName = RoleEnvironment.CurrentRoleInstance.Id;
                if (!string.IsNullOrEmpty(instanceName) && instanceName.Contains('_'))
                {
                    role = instanceName.Split('_').FirstOrDefault();
                    if (!string.IsNullOrEmpty(role))
                    {
                        role = role.ToLower();
                    }
                    try
                    {
                        int _rolenumber = Convert.ToInt32(instanceName.Split('_').LastOrDefault());
                        rolenumber = _rolenumber.ToString(CultureInfo.InvariantCulture);
                    }
                    catch (FormatException)
                    {
                        // do nothing
                    }
                    catch (OverflowException)
                    {
                        // do nothing
                    }
                }
                if (string.IsNullOrEmpty(role))
                {
                    role = "unknown";
                }
                if (string.IsNullOrEmpty(rolenumber))
                {
                    rolenumber = "0";
                }
            }
            catch(Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error calculating role numbers: " + ex.Message);
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error calculating role numbers";
                return false;
            }
            #endregion

            #region local.properties file
            // Build hybris configuration user.properties file
            Trace.TraceInformation("HybrisPlugin: Updating local.properties file.");
            string hybrisRoot = Path.Combine(BaseDirectory, "hybris");
            string hybrisWorkingDirectory = Path.Combine(hybrisRoot, "bin", "platform");
            try
            {
                string userPropertiesFile = Path.Combine(hybrisRoot, "config", "local.properties");
                string userPropertiesFileContent = System.IO.File.Exists(userPropertiesFile) ? System.IO.File.ReadAllText(userPropertiesFile) : string.Empty;

                // Set the tomcat TCP port and binding information
                IPEndPoint tomcatHttpIPEndpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["TomcatHttp"].IPEndpoint;
                userPropertiesFileContent = PatchProperty(userPropertiesFileContent, "tomcat.http.listenaddress", tomcatHttpIPEndpoint.Address.ToString());
                userPropertiesFileContent = PatchProperty(userPropertiesFileContent, "tomcat.http.port", tomcatHttpIPEndpoint.Port.ToString(CultureInfo.InvariantCulture));
                IPEndPoint tomcatHttpsIPEndpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["TomcatHttps"].IPEndpoint;
                userPropertiesFileContent = PatchProperty(userPropertiesFileContent, "tomcat.https.listenaddress", tomcatHttpsIPEndpoint.Address.ToString());
                userPropertiesFileContent = PatchProperty(userPropertiesFileContent, "tomcat.https.port", tomcatHttpsIPEndpoint.Port.ToString(CultureInfo.InvariantCulture));

                // Set the JGroups TCP port and binding information
                IPEndPoint invalidationIPEndpoint = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["InvalidateJGroup"].IPEndpoint;
                userPropertiesFileContent = PatchProperty(userPropertiesFileContent, "cluster.broadcast.method.jgroups.tcp.bind_addr", invalidationIPEndpoint.Address.ToString());
                userPropertiesFileContent = PatchProperty(userPropertiesFileContent, "cluster.broadcast.method.jgroups.tcp.bind_port", invalidationIPEndpoint.Port.ToString(CultureInfo.InvariantCulture));

                // Set the cluster id and node id
                // For a FrontendWorker, the cluster ID is calculated based on its IP Address (whilst the BackOfficeWorker has the ClusterId 0)
                //       See FrontendHybrisPlugin.cs Line 111
                IPAddress managementEndpointAddress = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["Management"].IPEndpoint.Address;
                byte[] addressBytes = managementEndpointAddress.GetAddressBytes();
                int clusterId = IsBackOfficeWorker ? 0 : addressBytes[addressBytes.Length - 1];

                userPropertiesFileContent = PatchProperty(userPropertiesFileContent, "cluster.maxid", "256");
                userPropertiesFileContent = PatchProperty(userPropertiesFileContent, "cluster.id", clusterId.ToString(CultureInfo.InvariantCulture));
                userPropertiesFileContent = PatchProperty(userPropertiesFileContent, "node.id", clusterId.ToString(CultureInfo.InvariantCulture));


                // Change media.default.url.strategy when being on backoffice worker
                if (RoleEnvironment.CurrentRoleInstance.Role.Name.ToLower().Contains("backoffice"))
                {
                    //userPropertiesFileContent = PatchProperty(userPropertiesFileContent, "media.default.url.strategy", "windowsAzureBlobURLStrategy");
                }

                // Update work path settings
                userPropertiesFileContent = userPropertiesFileContent.Replace(@"{instanceworkdir}", hybrisWorkDirectory.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)); // INFO: Was instanceWorkDirectory
                userPropertiesFileContent = userPropertiesFileContent.Replace(@"{workdir}", hybrisWorkDirectory.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                userPropertiesFileContent = userPropertiesFileContent.Replace(@"{logsdir}", hybrisLogsDirectory.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                userPropertiesFileContent = userPropertiesFileContent.Replace(@"{datadir}", hybrisDataDirectory.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                userPropertiesFileContent = userPropertiesFileContent.Replace(@"{tempdir}", hybrisTempDirectory.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                // Update the user.properties file
                File.WriteAllText(userPropertiesFile, userPropertiesFileContent);
            }
            catch(Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error creating user.properties file: " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error creating user.properties file";
                return false;
            }
            #endregion

            // 3. Create hybris commands
            #region build command
            // Read the standard PATH environment variable
            string searchPath = Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.Machine);

            string hybrisBuildCommandFileName = Path.Combine(hybrisWorkingDirectory, "hybris-build.cmd");
            try
            {
                Trace.TraceInformation("HybrisPlugin: Create hybris build command.");

                // Build hybris build command
                var hybrisBuildCommandBuilder = new StringBuilder();
                hybrisBuildCommandBuilder.AppendLine(@"@echo off");
                hybrisBuildCommandBuilder.AppendLine(@"set path=" + Path.Combine(JavaHomeDirectory, @"bin") + ";" + searchPath);
                hybrisBuildCommandBuilder.AppendLine(@"set java_home=" + JavaHomeDirectory);
                hybrisBuildCommandBuilder.AppendLine(@"set temp=" + sharedTempDirectory);
                hybrisBuildCommandBuilder.AppendLine(@"set tmp=" + sharedTempDirectory);
                hybrisBuildCommandBuilder.AppendLine();
                hybrisBuildCommandBuilder.AppendLine(@"set HYBRIS_DATA_DIR=" + hybrisDataDirectory);
                hybrisBuildCommandBuilder.AppendLine(@"set HYBRIS_TEMP_DIR=" + hybrisTempDirectory);
                hybrisBuildCommandBuilder.AppendLine(@"set HYBRIS_WORK_DIR=" + hybrisWorkDirectory);
                hybrisBuildCommandBuilder.AppendLine(@"set HYBRIS_LOG_DIR=" + hybrisLogsDirectory);
                hybrisBuildCommandBuilder.AppendLine(@"set HYBRIS_LOGS_DIR=" + hybrisLogsDirectory);
                hybrisBuildCommandBuilder.AppendLine();
                hybrisBuildCommandBuilder.AppendLine(@"cd " + hybrisWorkingDirectory);
                hybrisBuildCommandBuilder.AppendLine(@"call setantenv.bat");
                hybrisBuildCommandBuilder.AppendLine();
                //hybrisBuildCommandBuilder.AppendLine(@"cd " + Path.Combine(hybrisWorkingDirectory, @"..\..\build"));
                //hybrisBuildCommandBuilder.AppendLine(@"call ant config -Denv=" + DeploymentName + " -Drole=" + RoleName + " -Drolenumber=" + rolenumber); 
                //hybrisBuildCommandBuilder.AppendLine();
                //hybrisBuildCommandBuilder.AppendLine(@"cd " + hybrisWorkingDirectory);
                hybrisBuildCommandBuilder.AppendLine(@"call ant");
                File.WriteAllText(hybrisBuildCommandFileName, hybrisBuildCommandBuilder.ToString());
            }
            catch(Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error creating hybris build command: " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error creating hybris build command";
                return false;
            }
            #endregion

            #region start command
            // Build hybris start command
            string hybrisStartCommandFileName = Path.Combine(hybrisWorkingDirectory, "hybris-start.cmd");
            try
            { 
                Trace.TraceInformation("HybrisPlugin: Generate hybris start command");
                var hybrisStartCommandBuilder = new StringBuilder();
                hybrisStartCommandBuilder.AppendLine(@"@echo off");
                hybrisStartCommandBuilder.AppendLine(@"set path=" + Path.Combine(JavaHomeDirectory, @"bin") + ";" + searchPath);
                hybrisStartCommandBuilder.AppendLine(@"set java_home=" + JavaHomeDirectory);
                hybrisStartCommandBuilder.AppendLine(@"set temp=" + sharedTempDirectory);
                hybrisStartCommandBuilder.AppendLine(@"set tmp=" + sharedTempDirectory);
                hybrisStartCommandBuilder.AppendLine();
                hybrisStartCommandBuilder.AppendLine(@"set HYBRIS_DATA_DIR=" + hybrisDataDirectory);
                hybrisStartCommandBuilder.AppendLine(@"set HYBRIS_TEMP_DIR=" + hybrisTempDirectory);
                hybrisStartCommandBuilder.AppendLine(@"set HYBRIS_WORK_DIR=" + hybrisWorkDirectory);
                hybrisStartCommandBuilder.AppendLine(@"set HYBRIS_LOG_DIR=" + hybrisLogsDirectory);
                hybrisStartCommandBuilder.AppendLine(@"set HYBRIS_LOGS_DIR=" + hybrisLogsDirectory);
                hybrisStartCommandBuilder.AppendLine();
                hybrisStartCommandBuilder.AppendLine(@"cd " + hybrisWorkingDirectory);
                hybrisStartCommandBuilder.AppendLine(@"call setantenv.bat");
                hybrisStartCommandBuilder.AppendLine(@"call hybrisserver.bat");
                File.WriteAllText(hybrisStartCommandFileName, hybrisStartCommandBuilder.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error creating hybris start command: " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error creating hybris start command";
                return false;
            }
            #endregion

            #region stop command
            // Build hybris stop command
            string hybrisStopCommandFileName = Path.Combine(hybrisWorkingDirectory, "hybris-stop.cmd");

            try
            {
                Trace.TraceInformation("HybrisPlugin: Generating hybris stop command");
            
                var hybrisStopCommandBuilder = new StringBuilder();
                hybrisStopCommandBuilder.AppendLine(@"@echo off");
                hybrisStopCommandBuilder.AppendLine(@"set path=" + Path.Combine(JavaHomeDirectory, @"bin") + ";" + searchPath);
                hybrisStopCommandBuilder.AppendLine(@"set java_home=" + JavaHomeDirectory);
                hybrisStopCommandBuilder.AppendLine(@"set temp=" + sharedTempDirectory);
                hybrisStopCommandBuilder.AppendLine(@"set tmp=" + sharedTempDirectory);
                hybrisStopCommandBuilder.AppendLine();
                hybrisStopCommandBuilder.AppendLine(@"set HYBRIS_DATA_DIR=" + hybrisDataDirectory);
                hybrisStopCommandBuilder.AppendLine(@"set HYBRIS_TEMP_DIR=" + hybrisTempDirectory);
                hybrisStopCommandBuilder.AppendLine(@"set HYBRIS_WORK_DIR=" + hybrisWorkDirectory);
                hybrisStopCommandBuilder.AppendLine(@"set HYBRIS_LOG_DIR=" + hybrisLogsDirectory);
                hybrisStopCommandBuilder.AppendLine(@"set HYBRIS_LOGS_DIR=" + hybrisLogsDirectory);
                hybrisStopCommandBuilder.AppendLine();
                hybrisStopCommandBuilder.AppendLine(@"cd " + hybrisWorkingDirectory);
                hybrisStopCommandBuilder.AppendLine(@"java -jar StopTanukiWrapper.jar");
                File.WriteAllText(hybrisStopCommandFileName, hybrisStopCommandBuilder.ToString());
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error creating hybris stop command: " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error creating hybris stop command";
                return false;
            }
            #endregion

            // 4. TanukiWrapper
            #region Tanuki wrapper
            //Trace.TraceInformation("HybrisPlugin: Installing StopTanukiWrapper.jar");
            //try
            //{
            //    // Save the required StopTanukiWrapper.jar file in the platform directory
            //    string stopTanukiWrapperFileName = Path.Combine(hybrisWorkingDirectory, "StopTanukiWrapper.jar");
            //    File.WriteAllBytes(stopTanukiWrapperFileName, hybrisOnAzure.Common.Properties.Resources.StopTanukiWrapper);
            //}
            //catch(Exception ex)
            //{
            //    Trace.TraceAndLogError("HybrisPlugin", "Error installing StopTanukiWrapper.jar: " + ex.ToString());
            //    this.Status = AzurePluginStatus.ErrorInitializing;
            //    this.StatusMessage = "Error installing StopTanukiWrapper.jar";
            //    return false;
            //}
            #endregion

            // 5. actually build the hybris platform
            #region Build hybris
            // Build hybris platform
            Trace.TraceInformation("HybrisPlugin: Building hybris platform.");
            try
            {
                var buildOutput = new StringBuilder();
                var buildError = new StringBuilder();
                using (var buildProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        WorkingDirectory = hybrisWorkingDirectory,
                        FileName = hybrisBuildCommandFileName,
                        Arguments = string.Empty,
                        UseShellExecute = false,
                        LoadUserProfile = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true,
                        CreateNoWindow = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    },
                    EnableRaisingEvents = true,
                })
                {
                    buildProcess.OutputDataReceived += (s, a) => Trace.TraceVerbose("HybrisPlugin: Building hybris ouput line" + a.Data);
                    buildProcess.ErrorDataReceived += (s, a) => Trace.TraceAndLogError("HybrisPlugin", "Building hybris error line" + a.Data);

                    if (buildProcess.Start())
                    {
                        buildProcess.BeginOutputReadLine();
                        buildProcess.BeginErrorReadLine();
                        buildProcess.WaitForExit();
                    }

                    if (buildProcess.ExitCode == 0)
                        Trace.TraceAndLogInformation("HybrisPlugin", "Successfully built hybris platform.");
                    else
                        Trace.TraceAndLogError("HybrisPlugin", "Error executing build hybris platform command.");
                }
            }
            catch(Exception ex)
            {
                Trace.TraceAndLogError("Hybris", "Error building hybris platform. " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error building hybris platform.";
                return false;
            }
            #endregion

            // 6. Get from additional settings if we may start the hybris process
            #region Additional configuration
            AdditionalConfigurationManager.Instance.AdditionalConfigurationChanged += Instance_AdditionalConfigurationChanged;
            try
            {
                Trace.TraceInformation("HybrisPlugin: Processing initial additional configuration.");
                AdditionalConfigurationManager.Instance.ProcessConfiguration();
                Trace.TraceInformation("HybrisPlugin: Successfully processed initial additional configuration.");
            }
            catch(Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error processing initial additional configuration: " + ex.ToString());
            }
            try
            {
                Trace.TraceInformation("HybrisPlugin", "Determining if Hybris was stopped before a reboot.");
                var stopHybris = AdditionalConfigurationManager.Instance.GetCurrentConfigurationValue("StopHybris");
                if (stopHybris == null)
                    Trace.TraceAndLogWarning("HybrisPlugin", "Determining if Hybris was stopped before a reboot resulted in a NULL value. Hybris will be started.");
                else
                {
                    Trace.TraceInformation("HybrisPlugin: Determining if Hybris was stopped before a reboot resulted in: " + stopHybris);
                    this.ConfigStopHybris = bool.Parse(stopHybris);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error getting from Additional configuration if processes are to be started: " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error getting Additional configuration";
                return false;
            }
            #endregion

            Trace.TraceInformation("HybrisPlugin: Initialization done.");
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
                    Trace.TraceAndLogInformation("HybrisPlugin", "Additional configuration changed value StopHybris to " + newValue.ToString());
                    // enable or disable the Routing rule to the maintenance page
                    this.ConfigStopHybris = newValue;

                    // if hybris has to be stopped > stop hybris
                    if (newValue && _HybrisJavaProcess != null && !_HybrisJavaProcess.HasExited)
                        SendStop();
                }
            }
        }

        private bool ConfigStopHybris { get; set; }

        private System.Diagnostics.Process _HybrisJavaProcess;

        public bool IsAlive
        {
            get 
            {
                // this Plugin is alive as long as the Hybris process is running
                // or if the additional configuration says the process should not be running
                try
                {
                    return this.ConfigStopHybris || (_HybrisJavaProcess != null && !_HybrisJavaProcess.HasExited);
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool Start()
        {
            // if hybris is still running we don't need to start again
            if (this.IsAlive) return true;
            
            this.Status = AzurePluginStatus.Starting;
            this.StatusMessage = string.Empty;
            try
            {
                // Prepare hybris start
				string hybrisRoot = Path.Combine(BaseDirectory, "hybris");
				string hybrisWorkingDirectory = Path.Combine(hybrisRoot, "bin", "platform");
				string hybrisStartCommandFileName = Path.Combine(hybrisWorkingDirectory, "hybris-start.cmd");

                // Start hybris process
                _HybrisJavaProcess = new System.Diagnostics.Process
                            {
                                StartInfo = new System.Diagnostics.ProcessStartInfo
                                    {
                                        WorkingDirectory = hybrisWorkingDirectory,
                                        FileName = hybrisStartCommandFileName,
                                        Arguments = string.Empty,
                                        UseShellExecute = false,
                                        LoadUserProfile = false,
                                        RedirectStandardOutput = true,
                                        RedirectStandardError = true,
                                        RedirectStandardInput = true,
                                        CreateNoWindow = true,
                                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                                    },
                                EnableRaisingEvents = true,
                            };
                _HybrisJavaProcess.OutputDataReceived += (s, a) => { if (!string.IsNullOrWhiteSpace(a.Data)) Trace.TraceVerbose("HybrisPlugin: Hybris process output: " + a.Data); };
                _HybrisJavaProcess.ErrorDataReceived += (s, a) => { if (!string.IsNullOrWhiteSpace(a.Data)) Trace.TraceAndLogError("HybrisPlugin", "Hybris process error: " + a.Data); };
                _HybrisJavaProcess.Exited += delegate(object sender, EventArgs e)
                    {
                        if (!this.ConfigStopHybris)
                        {
                            Trace.TraceAndLogError("HybrisPlugin", "Hybris process exited! ExitCode " + (sender as System.Diagnostics.Process).ExitCode.ToString());
                            this.Status = AzurePluginStatus.Error;
                            this.StatusMessage = "Hybris process exited!";
                        }
                        else
                        {
                            Trace.TraceAndLogWarning("HybrisPlugin", "Hybris process exited due to Additional Configuration change! ExitCode " + (sender as System.Diagnostics.Process).ExitCode.ToString());
                            this.Status = AzurePluginStatus.Warning;
                            this.StatusMessage = "Hybris process exited!";
                        }
                    };
                if (_HybrisJavaProcess.Start())
                {
                    _HybrisJavaProcess.BeginOutputReadLine();
                    _HybrisJavaProcess.BeginErrorReadLine();
                }
                else
                {
                    Trace.TraceAndLogError("HybrisPlugin", "Hybris process could not be started!");
                    this.Status = AzurePluginStatus.ErrorStarting;
                    this.StatusMessage = "Hybris process could not be started!";
                    return false;
                }
                Trace.TraceAndLogInformation("HybrisPlugin", "Hybris process started.");
            }
            catch(Exception ex)
            {
                Trace.TraceAndLogError("HybrisPlugin", "Error while creating HybrisJavaProcess: " + ex.ToString());
                this.Status = AzurePluginStatus.ErrorStarting;
                this.StatusMessage = "Error while creating HybrisJavaProcess.";
                return false;
            }

            this.Status = AzurePluginStatus.Healthy;
            this.StatusMessage = "Process ID: " + _HybrisJavaProcess.Id.ToString();
            return true;
        }

        private int StopRetries = 6;
        private int StopRetryInterval = 1000 * 30; // 30 seconds
        public void SendStop()
        {
            this.Status = AzurePluginStatus.Stopping;

            Trace.TraceAndLogWarning("HybrisPlugin", "SendStop: Kill hybris child processes.");
            foreach (var childProcess in _HybrisJavaProcess.GetChildProcesses(true).Reverse())
            {
                try
                {
                    childProcess.Kill();
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("HybrisPlugin: SendStop: Could not kill process with ID=" + childProcess.Id.ToString() + ": " + ex.ToString());
                }
            }
            try
            {
                _HybrisJavaProcess.Kill();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("HybrisPlugin: SendStop: Could not kill HybrisJavaProcess: " + ex.ToString());
            }

            if (!_HybrisJavaProcess.HasExited)
                this.Status = AzurePluginStatus.Unknown;
            else
                this.Status = AzurePluginStatus.Stopped;
            this.StatusMessage = "Stopped.";

            if (this.ConfigStopHybris)
                this.StatusMessage = "Stopped due to additional configuration change.";
            else
                this.StatusMessage = "Stopped.";
        }

        public AzurePluginStatus Status { get; private set; }

        public string StatusMessage { get; private set; }

        #region Private Methods
        /// <summary>
        /// Patches the property in the given property file content string.
        /// </summary>
        private static string PatchProperty(string data, string key, string value)
        {
            const RegexOptions options = RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.Compiled;

            string result;
            var pattern = "^" + key.Replace(".", @"\.") + "=(.*)$";
            if (Regex.IsMatch(data, pattern, options))
            {
                var newValue = key + "=" + value;
                result = Regex.Replace(data, pattern, newValue, options);
            }
            else
            {
                // append the value when it is  not there
                result = data + "\r\n" + key + "=" + value + "\r\n";
            }
            return result;
        }
        #endregion
    }
}
