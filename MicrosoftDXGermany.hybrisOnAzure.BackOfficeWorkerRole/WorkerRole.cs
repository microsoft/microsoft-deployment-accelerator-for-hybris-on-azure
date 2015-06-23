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
using System.Threading;
using System.Web;
using tangible.Azure.Base;
using tangible.Azure.Tracing;

namespace MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole
{
    public class BackOfficeWorkerRole : AzurePluginHost
    {
        public BackOfficeWorkerRole()
            : base()
        {
            this.UnhandledException += new UnhandledExceptionEventHandler(BackOfficeWorkerRole_UnhandledException);
        }

        void BackOfficeWorkerRole_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Trace.TraceAndLogError("BackOfficeWorkerRole", "Unhandled exception " + ((e.ExceptionObject != null) ? e.ExceptionObject.ToString() : "No information"));
        }

        public override bool BeforeOnStart()
        {
            // Note: (2) We attach also to a local file on disk
            if (System.IO.File.Exists("C:\\AzureWorkerRoleLog.txt")) System.IO.File.Delete("C:\\AzureWorkerRoleLog.txt");
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener("C:\\AzureWorkerRoleLog.txt"));

            this.AddComponent(new BackOfficeWorkerServer());
            return true;
        }
    }
}
