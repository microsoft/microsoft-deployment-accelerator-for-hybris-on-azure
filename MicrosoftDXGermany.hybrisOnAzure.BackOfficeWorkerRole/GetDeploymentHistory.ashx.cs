// Microsoft Deployment Accelerator for hybris on Azure - sample code
// Copyright (c) Microsoft Corporation
// see LICENSE.txt for license information

using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using tangible.Azure.Storage.Blob;

namespace MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole
{
    /// <summary>
    /// Summary description for GetDeploymentHistory
    /// </summary>
    public class GetDeploymentHistory : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            if (!context.Request.Params.AllKeys.Contains("deploymentInfo"))
            {
                context.Response.Write("Parameter missing.");
                return;
            }

            var fileName = "config/" + context.Request.Params["deploymentInfo"];

            context.Response.ContentType = "text/xml";
            CloudStorageAccount storageAccount;
            if (!RoleEnvironment.IsAvailable)
                storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true;");
            else
                storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();

            if (!blobClient.BlobExists(fileName))
            {
                context.Response.Write("File does not exist.");
                return;
            }

            try
            {
                var blob = blobClient.GetBlobReference(fileName);
                blob.DownloadToStream(context.Response.OutputStream);
            }
            catch (Exception ex)
            {
                context.Response.Write("Error getting file: " + ex.ToString());
            }
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}