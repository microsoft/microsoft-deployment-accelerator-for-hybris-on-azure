// Microsoft Deployment Accelerator for hybris on Azure - sample code
// Copyright (c) Microsoft Corporation
// see LICENSE.txt for license information

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using tangible.Azure.Base;
using tangible.Azure.Downloader;
using tangible.Azure.Tracing;
using tangible.Azure.StartupScripts;
using tangible.Azure.IIS;
using tangible.Azure.AdditionalConfiguration;

namespace MicrosoftDXGermany.hybrisOnAzure.ArrRole
{
    /// <summary>
    /// This class represents the ARR Role.
    /// On Starting it downloads neccessary components and executes scripts as defined in the .cscfg file
    /// There may be run multiple Plugins on this role. There is only the ArrPlugin running which handles Setting an instance in Maintenance mode.
    /// </summary>
    public class ArrServer : AzureComponentBase
    {
        static string TRACESOURCE = "ArrServer";

        /// <summary>
        /// Is executed before the "OnStart" method is called.
        /// Here all configuration takes place.
        /// </summary>
        /// <returns></returns>
        public override bool BeforeOnStart()
        {
            var onStartSuccessful = true;

            System.Net.ServicePointManager.DefaultConnectionLimit = 64;

            #region create plugins
            this.AddPlugin<ArrPlugin>(null);
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

            #region Configure ARR
            Trace.TraceInformation("WebServer - OnStart: Configuring ARR HealthMonitoring.");
            onStartSuccessful = (!IISHelper.ConfigureHealthMonitoring()) ? false : onStartSuccessful;
            Trace.TraceInformation("WebServer - OnStart: Configure WebFarm");
            onStartSuccessful = (!IISHelper.ConfigureWebFarm()) ? false : onStartSuccessful;
            #endregion

            #region handle role environment changes
            RoleEnvironment.Changed += RoleEnvironment_Changed;
            #endregion

            return onStartSuccessful;
        }

        void RoleEnvironment_Changed(object sender, RoleEnvironmentChangedEventArgs e)
        {
            // On Topology Changes to the FrontendWorkerRole --> Configure AzureWebFarm
            if (e.Changes.Any(c => c is RoleEnvironmentTopologyChange))
            {
                Trace.TraceAndLogInformation(TRACESOURCE + ":TopologyChange", "The topology has changed. Reconfiguring webfarm.");
                ClixOnAzure.WebFarmHostManager.ConfigureAzureWebFarm();
                Trace.TraceInformation(TRACESOURCE + ":TopologyChange: Azure Web Farm has been reconfigured.");
            }
        }
    }
}
