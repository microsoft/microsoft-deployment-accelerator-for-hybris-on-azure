// Microsoft Deployment Accelerator for hybris on Azure - sample code
// Copyright (c) Microsoft Corporation
// see LICENSE.txt for license information

using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tangible.Azure.AdditionalConfiguration;
using tangible.Azure.Base;
using tangible.Azure.Downloader;
using tangible.Azure.StartupScripts;
using tangible.Azure.Tracing;

namespace MicrosoftDXGermany.hybrisOnAzure.FrontendWorkerRole
{
    public class FrontendWorkerServer : AzureComponentBase
    {
        static string TRACESOURCE = "FrontendWorkerServer";

        public override bool BeforeOnStart()
        {
            var onStartSuccessful = true;

            System.Net.ServicePointManager.DefaultConnectionLimit = 12;

            #region create plugins
            if (!this.ActivePlugins.Any(p => p.GetType() == typeof(Plugins.HybrisPlugin)))
                this.AddPlugin<Plugins.HybrisPlugin>(null);
            else
            {
                Trace.TraceAndLogWarning(TRACESOURCE, "Before on Start called with a running HybrisPlugin. Sending stop...");
                try
                {
                    this.ActivePlugins.First(p => p.GetType() == typeof(Plugins.HybrisPlugin)).SendStop();
                    Trace.TraceAndLogInformation(TRACESOURCE, "Successfully stopped HybrisPlugin in BeforeOnStart.");
                }
                catch (Exception ex)
                {
                    Trace.TraceAndLogError(TRACESOURCE, "Error stopping HybrisPlugin in BeforeOnStart. Error: " + ex.ToString());
                }
            }

            try
            {
                bool startSolr = false;
                if (!bool.TryParse(RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.StartSolrPlugin"), out startSolr))
                    Trace.TraceAndLogError(TRACESOURCE, "ERROR: Could not parse 'HybrisOnAzure.StartSolrPlugin' configuration.");
                if (startSolr && !this.ActivePlugins.Any(p => p.GetType() == typeof(Plugins.SolrPlugin)))
                    this.AddPlugin<Plugins.SolrPlugin>(null);
                else if (this.ActivePlugins.Any(p => p.GetType() == typeof(Plugins.SolrPlugin)))
                {
                    Trace.TraceAndLogWarning(TRACESOURCE, "Before on Start called with a running SolrPlugin. Sending stop...");
                    try
                    {
                        this.ActivePlugins.First(p => p.GetType() == typeof(Plugins.SolrPlugin)).SendStop();
                        Trace.TraceAndLogInformation(TRACESOURCE, "Successfully stopped SolrPlugin in BeforeOnStart.");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceAndLogError(TRACESOURCE, "Error stopping SolrPlugin in BeforeOnStart. Error: " + ex.ToString());
                    }

                    if (!startSolr)
                    {
                        Trace.TraceAndLogInformation(TRACESOURCE, "Removing SolrPlugin.");
                        this.RemovePlugin(this.ActivePlugins.First(p => p.GetType() == typeof(Plugins.SolrPlugin)));
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError(TRACESOURCE, "Error creating SolrPlugin in BeforeOnStart. Error: " + ex.ToString());
            }
            #endregion

            #region CLEANUP
            try
            {
                var rootDirectory = RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.BaseDirectory");
                Trace.TraceInformation(TRACESOURCE + " OnStart - cleanup old files from '{0}'.", rootDirectory);
                if (System.IO.Directory.Exists(rootDirectory))
                {
                    Trace.TraceInformation(TRACESOURCE + " OnStart - '{0}' exists > delete.", rootDirectory);
                    System.IO.Directory.Delete(rootDirectory, true);
                    if (System.IO.Directory.Exists(rootDirectory))
                        Trace.TraceError(TRACESOURCE + " OnStart -'{0}' successfully deleted but still exists!.", rootDirectory);
                    else
                        Trace.TraceInformation(TRACESOURCE + " OnStart - '{0}' successfully deleted.", rootDirectory);
                }
                else
                    Trace.TraceInformation(TRACESOURCE + " OnStart - '{0}' does not exist.", rootDirectory);
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError(TRACESOURCE, "OnStart - error at Cleanup old files: " + ex.ToString());
            }
            #endregion

            #region Download stuff from storage
            // download files from storage to this computer
            try
            {
                Trace.TraceInformation(TRACESOURCE + ": BeforeOnStart: Starting DownloadHelper.");
                DownloadHelper.Download();
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError(TRACESOURCE, "BeforeOnStart: ERROR while executing DownloadHelper. Message: " + ex.Message);
                onStartSuccessful = false;
            }
            #endregion

            #region PatchFiles
            // patch files
            try
            {
                Trace.TraceInformation(TRACESOURCE + ": BeforeOnStart: Starting PatchFiles.");
                PatchFileHelper.PatchAllFilesFromConfig();
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError(TRACESOURCE, "BeforeOnStart: ERROR while executing PatchFileHelper. Message: " + ex.Message, ex);
                onStartSuccessful = false;
            }
            #endregion

            #region Run Commandlets
            // execute setup via configurable commandlets.
            try
            {
                Trace.TraceInformation(TRACESOURCE + ": BeforeOnStart: Starting SetupHelper.");
                new StartupScriptHelper().ExecuteScripts();
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError(TRACESOURCE, "BeforeOnStart: ERROR while executing SetupHelper. Message: " + ex.Message, ex);
                onStartSuccessful = false;
            }
            #endregion

            #region handle role environment changes
            RoleEnvironment.Changed += RoleEnvironment_Changed;
            #endregion

            #region hadle additional changes
            AdditionalConfigurationManager.Instance.AdditionalConfigurationChanged += AdditionalConfiguration_Changed;
            try
            {
                AdditionalConfigurationManager.Instance.ProcessConfiguration();
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError(TRACESOURCE, "Error processing initial AdditionalConfiguration:" + ex.ToString());
            }
            #endregion

            return onStartSuccessful;
        }

        void AdditionalConfiguration_Changed(object sender, ConfigurationItemChangedEventArgs e)
        {
            Trace.TraceInformation(TRACESOURCE + ":AdditionalConfiguration: " + e.Name + " (" + e.ChangeType.ToString() + ")" + " New = " + e.NewValue ?? "NULL" + " | " + "Old = " + e.OldValue ?? "NULL");

            // on change of the "Reboot"-Value > Request a role recycle
            if (e.Name == "Reboot" && e.ChangeType == ChangeType.Modified && !string.IsNullOrEmpty(e.NewValue))
            {
                Trace.TraceAndLogWarning(TRACESOURCE, "Additional Change requested reboot.");
                //RoleEnvironment.RequestRecycle();
                this.OnStop();
                System.Diagnostics.Process.Start("shutdown", "/r /t 0");
            }
        }

        void RoleEnvironment_Changed(object sender, RoleEnvironmentChangedEventArgs e)
        {
        }
    }
}
