// Microsoft Deployment Accelerator for hybris on Azure - sample code
// Copyright (c) Microsoft Corporation
// see LICENSE.txt for license information

using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using tangible.Azure.Base;
using tangible.Azure.Tracing;

namespace MicrosoftDXGermany.hybrisOnAzure.ArrRole
{
    /// <summary>
    /// This class represents the entry point for Windows Azure.
    /// It may hold multiple Components (a component is usually run on one role, but they may be consolidated to run on one machine).
    /// In this case it hosts only the ArrServer component.
    /// </summary>
    public class ArrWebRole : AzurePluginHost
    {
        public ArrWebRole()
            : base()
        {
            this.UnhandledException += new UnhandledExceptionEventHandler(WebRole_UnhandledException);
        }

        void WebRole_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.TraceAndLogError("ArrWebRole", "Unhandled exception " + ((e.ExceptionObject != null) ? e.ExceptionObject.ToString() : "No information"));
        }

        public override bool BeforeOnStart()
        {
            // Note: (2) We attach also to a local file on disk
            if (System.IO.File.Exists("C:\\AzureWebRoleLog.txt")) System.IO.File.Delete("C:\\AzureWebRoleLog.txt");
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener("C:\\AzureWebRoleLog.txt"));

            this.AddComponent(new ArrServer());
            return true;
        }
    }
}