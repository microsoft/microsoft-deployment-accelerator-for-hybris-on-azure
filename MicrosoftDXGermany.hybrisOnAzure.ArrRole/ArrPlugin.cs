// Microsoft Deployment Accelerator for hybris on Azure - sample code
// Copyright (c) Microsoft Corporation
// see LICENSE.txt for license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using tangible.Azure.AdditionalConfiguration;
using tangible.Azure.Base;
using tangible.Azure.IIS;
using tangible.Azure.Tracing;

namespace MicrosoftDXGermany.hybrisOnAzure.ArrRole
{
    /// <summary>
    /// This plugin runs on a separate thread and monitors changes made to the environment
    /// that e.g. put this instance into maintenance mode.
    /// </summary>
    public class ArrPlugin : IAzurePlugin
    {
        public static string TRACESOURCE = "ArrPlugin";

        public bool Initialize()
        {
            this.Status = AzurePluginStatus.Initializing;
            this.StatusMessage = "Initializing";

            #region handle additional changes
            // Hook up to the AdditionalConfigurationChanged event
            AdditionalConfigurationManager.Instance.AdditionalConfigurationChanged += AdditionalConfiguration_Changed;
            try
            {
                // set up the last known configuration
                AdditionalConfigurationManager.Instance.ProcessConfiguration();
                this.Status = AzurePluginStatus.NotStarted;
                this.StatusMessage = "Not started.";
            }
            catch (Exception ex)
            {
                Trace.TraceAndLogError(TRACESOURCE, "Error processing initial AdditionalConfiguration:" + ex.ToString());
                this.Status = AzurePluginStatus.ErrorInitializing;
                this.StatusMessage = "Error processing initial AdditionalConfiguration:" + ex.ToString();
            }
            #endregion

            this.IsAlive = false;

            return true;
        }

        /// <summary>
        /// Handles changes made to the additional configuration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AdditionalConfiguration_Changed(object sender, ConfigurationItemChangedEventArgs e)
        {
            Trace.TraceInformation(TRACESOURCE + ":AdditionalConfiguration: " + e.Name + " (" + e.ChangeType.ToString() + ")" + " New = " + e.NewValue ?? "NULL" + " | " + "Old = " + e.OldValue ?? "NULL");

            if (e.Name == "IsInMaintenance")
            {
                // this instance is told to change it's maintenance mode
                var stringValue = (e.ChangeType == ChangeType.Added | e.ChangeType == ChangeType.Modified) ? e.NewValue : e.OldValue;
                bool newValue;
                if (bool.TryParse(stringValue, out newValue))
                {
                    // enable or disable the Routing rule to the maintenance page
                    CommonIISHelper.ChangeArrRuleEnabled("IsInMaintenance", newValue);
                }
            }

            SetStatus();
        }

        public bool IsAlive
        {
            get;
            private set;
        }

        public bool Start()
        {
            this.Status = AzurePluginStatus.Starting;
            this.StatusMessage = "Starting";

            SetStatus();
            this.IsAlive = true;

            return true;
        }

        public void SendStop()
        {
            this.Status = AzurePluginStatus.Stopped;
            this.StatusMessage = String.Empty;
        }

        private void SetStatus()
        {
            try
            {
                var inMaintenance = CommonIISHelper.GetArrRuleEnabled("IsInMaintenance");
                if (inMaintenance == null || !inMaintenance.HasValue)
                {
                    throw new Exception("Error retrieving 'IsInMaintenance' ArrRule.");
                }
                else if (inMaintenance.Value)
                {
                    // this instance is in maintenance mode
                    this.Status = AzurePluginStatus.Warning;
                    this.StatusMessage = "Maintenance";
                }
                else
                {
                    this.Status = AzurePluginStatus.Healthy;
                    this.StatusMessage = "Running";
                }
            }
            catch (Exception ex)
            {
                this.Status = AzurePluginStatus.Error;
                this.StatusMessage = ex.Message;
            }
        }

        private AzurePluginStatus _Status;
        public AzurePluginStatus Status
        {
            get { return _Status; }
            set { _Status = value; }
        }

        private string _StatusMessage;
        public string StatusMessage
        {
            get { return _StatusMessage; }
            set { _StatusMessage = value; }
        }
    }
}