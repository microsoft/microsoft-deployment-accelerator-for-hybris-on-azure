using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using tangible.Azure.StatusService;
using System.Net;
using System.Text;
using Microsoft.WindowsAzure.Storage;
using tangible.Azure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Blob;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using AdditionalConfiguration = tangible.Azure.AdditionalConfiguration.AdditionalConfiguration;

namespace MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole
{
    public partial class Admin : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            try
            {
                if (!RoleEnvironment.IsAvailable) { Response.Write("Role environment not available."); return; }

                // get currently active additional configuration
                var additionalConfig = GetAdditionalConfiguration();
                // display the current status of 
                DisplayArrStatus(additionalConfig);
                // if there are instances of the frontend worker > display their status
                if (RoleEnvironment.Roles.ContainsKey("MicrosoftDXGermany.hybrisOnAzure.FrontendWorkerRole"))
                {
                    DisplayRoleStatus("MicrosoftDXGermany.hybrisOnAzure.FrontendWorkerRole", "FrontendWorkerServer", tblFrontend, additionalConfig);
                    if (!Page.IsPostBack)
                        txtFrontendInstanceCount.Text = RoleEnvironment.Roles["MicrosoftDXGermany.hybrisOnAzure.FrontendWorkerRole"].Instances.Count.ToString();
                }

                // if there are instances of the backend workder > display their status
                if (RoleEnvironment.Roles.ContainsKey("MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole"))
                    DisplayRoleStatus("MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole", "BackOfficeWorkerServer", tblBackOffice, additionalConfig);

                // display currently used hybris and java packages
                if (!Page.IsPostBack)
                {
                    var defaultConfig = additionalConfig.Roles.First(r => r.Name == "MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole").Instances.First(i => i.Id == "default");
                    txtJavaPackage.Text = defaultConfig.Configurations.Any(c => c.Name == "JavaPackage") ? defaultConfig.Configurations.First(c => c.Name == "JavaPackage").Value : string.Empty;
                    txtHybrisPackage.Text = defaultConfig.Configurations.Any(c => c.Name == "HybrisPackage") ? defaultConfig.Configurations.First(c => c.Name == "HybrisPackage").Value : string.Empty;
                }

                // display DeploymentHistory
                DisplayDeploymentHistory();
            }
            catch(Exception ex)
            {
                Response.Write("Error loading the Admin.aspx page: " + ex.ToString());
            }
        }

        private void DisplayArrStatus(AdditionalConfiguration additionalConfig)
        {
            var role = RoleEnvironment.Roles["MicrosoftDXGermany.hybrisOnAzure.ArrRole"];

            TableRow currentRow = null;
            for(int i = 0; i < role.Instances.Count; i++)
            {
                var currentInstance = role.Instances[i];

                // only display 6 instances per row
                // > add current row and create a new one
                if (i % 6 == 0)
                {
                    currentRow = new TableRow();
                    tblArr.Rows.Add(currentRow);
                }

                var cll = new TableCell();
                currentRow.Cells.Add(cll);
                var div = new Panel() { CssClass = "default" };
                cll.Controls.Add(div);
                
                // display IPAddress / Name of that instance
                div.Controls.Add(new Label() { Text = currentInstance.InstanceEndpoints.First().Value.IPEndpoint.Address.ToString() + "<br />" });

                // get status from that instance
                var statusClient = GetStatusServiceClientForInstance(currentInstance);
                if (statusClient == null)
                {
                    div.CssClass = "error";
                    div.Controls.Add(new Label() { Text = "No connection to status client." });
                    continue;
                }
                Dictionary<string, Dictionary<string, Tuple<string, string>>> clientResult = null;
                try
                {
                    clientResult = statusClient.GetPluginStatus();
                }
                catch(Exception ex)
                {
                    div.CssClass = "error";
                    div.Controls.Add(new Label() { Text = "Error retrieving client status. " + ex.Message });
                    continue;
                }

                // there is only one component (ArrServer) running hosting one plugin (ArrPlugin)
                // > we get this specific status information
                Tuple<string, string> arrPluginStatus;
                try
                {
                    arrPluginStatus = clientResult["ArrServer"]["ArrPlugin"];
                    var status = arrPluginStatus.Item1.ToLower();
                    div.CssClass = (status == "healthy" || status == "warning" || status == "error") ? status : "default";
                    div.Controls.Add(new Label() { Text = arrPluginStatus.Item2 });
                }
                catch(Exception ex)
                {
                    div.CssClass = "warning";
                    div.Controls.Add(new Label() { Text = "Error getting plugin status: " + ex.Message });
                    continue;
                }

                // display checkBox for MaintenanceMode
                var chk = new CheckBox() { ID = "chkMaintenance_" + currentInstance.Id, Text = "Set maintenance" };
                if (!Page.IsPostBack)
                    chk.Checked = arrPluginStatus.Item2.ToLower().Contains("maintenance");
                cll.Controls.Add(chk);
            }

            // display current instance count
            if (!Page.IsPostBack)
            {
                txtArrInstanceCount.Text = role.Instances.Count.ToString();
            }
        }
        private void DisplayRoleStatus(string roleName, string componentName, Table targetTable, AdditionalConfiguration additionalConfig)
        {
            var role = RoleEnvironment.Roles[roleName];
            foreach(var currentInstance in role.Instances)
            {
                var additionalInstanceConfig = additionalConfig.GetConfigurationForInstance(currentInstance.Role.Name, currentInstance.Id);
                var row = new TableRow();
                targetTable.Rows.Add(row);
                var cll1 = new TableCell();
                row.Cells.Add(cll1);
                var div = new Panel();
                cll1.Controls.Add(div);

                // display IPAddress / Name of that instance
                div.Controls.Add(new Label() { Text = currentInstance.InstanceEndpoints.First().Value.IPEndpoint.Address.ToString() + "<br />" });

                // get status from that instance
                var statusClient = GetStatusServiceClientForInstance(currentInstance);
                if (statusClient == null)
                {
                    div.CssClass = "error";
                    div.Controls.Add(new Label() { Text = "No connection to status client." });
                    continue;
                }
                Dictionary<string, Dictionary<string, Tuple<string, string>>> clientResult = null;
                try
                {
                    clientResult = statusClient.GetPluginStatus();
                }
                catch (Exception ex)
                {
                    div.CssClass = "error";
                    div.Controls.Add(new Label() { Text = "Error retrieving client status. " + ex.Message });
                    continue;
                }

                var cll2 = new TableCell();
                row.Cells.Add(cll2);
                var div2 = new Panel() { CssClass = "pluginState" };
                cll2.Controls.Add(div2);

                // there is only one component running: FrontendWorkerServer or BackOfficeWorkerServer
                var component = clientResult[componentName];
                foreach(var pluginName in component.Keys)
                {
                    div2.Controls.Add(new Label() { Text = pluginName + " (" });
                    div2.Controls.Add(new Label() { Text = component[pluginName].Item1, CssClass = component[pluginName].Item1.ToLower() });
                    div2.Controls.Add(new Label() { Text = "): " + component[pluginName].Item2 + "<br />" });
                }

                // display role status
                if (component.Values.Any(pluginStatus => pluginStatus.Item1.ToLower() == "error"))
                {
                    div.CssClass = "error";
                    div.Controls.Add(new Label() { Text = "Error" });
                }
                else if (component.Values.Any(pluginStatus => pluginStatus.Item1.ToLower() == "warning"))
                {
                    div.CssClass = "warning";
                    div.Controls.Add(new Label() { Text = "Error" });
                }
                else
                {
                    div.CssClass = "healthy";
                    div.Controls.Add(new Label() { Text = "Healthy" });
                }

                // display controls for rebooting and stop hybris
                var table = new Table() { CssClass = "instanceOptions" };
                cll2.Controls.Add(table);
                var r1 = new TableRow();
                table.Rows.Add(r1);
                var c1 = new TableCell();
                r1.Cells.Add(c1);
                var chk1 = new CheckBox() { ID = "chkStopHybris_" + currentInstance.Id, Text = "Stop Hybris" };
                if (!Page.IsPostBack)
                    chk1.Checked = additionalInstanceConfig.Any(c => c.Name == "StopHybris") ? bool.Parse(additionalInstanceConfig.First(c => c.Name == "StopHybris").Value) : false;
                c1.Controls.Add(chk1);
                var r2 = new TableRow();
                table.Rows.Add(r2);
                var c2 = new TableCell();
                r2.Cells.Add(c2);
                var chk2 = new CheckBox() { ID = "chkReboot_" + currentInstance.Id, Text = "Reboot" };
                if (!Page.IsPostBack)
                    chk2.Checked = false; // TODO: Init
                c2.Controls.Add(chk2);
            }
        }
        private void DisplayDeploymentHistory()
        {
            CloudStorageAccount storageAccount;
            if (!RoleEnvironment.IsAvailable)
                storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true;");
            else
                storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("config");
            container.CreateIfNotExists();
            foreach(var blob in container.ListBlobs().Where(b => b is CloudBlockBlob).Select(b => b as CloudBlockBlob).OrderByDescending(b => b.Name))
            {
                var row = new TableRow();
                tblDeploymentHistory.Rows.Add(row);
                var cell1 = new TableHeaderCell() { Text = blob.Name };
                cell1.Style.Add("width", "250px");
                row.Cells.Add(cell1);
                var cell2 = new TableCell() { Text = "<a href=\"GetDeploymentHistory.ashx?deploymentInfo=" + blob.Name + "\">Download</a>" };
                row.Cells.Add(cell2);
            }
        }

        private IStatusService GetStatusServiceClientForInstance(RoleInstance instance)
        {
            try
            {
                NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
                var factory = new ChannelFactory<IStatusService>(binding);

                var endpoint = instance.InstanceEndpoints["PluginStatus"];
                EndpointAddress address = new EndpointAddress(String.Format("net.tcp://{0}/PluginStatus", endpoint.IPEndpoint));
                return factory.CreateChannel(address);
            }
            catch(Exception ex)
            {
                tangible.Azure.Tracing.Trace.TraceAndLogError("StatusPage", "Error getting status service client for " + instance.Id + ": " + ex.ToString());
                return null;
            }
        }

        protected void cmdSubmit_Click(object sender, EventArgs e)
        {
            // check, if user provided a user name
            if (string.IsNullOrWhiteSpace(txtUserName.Text))
            {
                lblMessage.Text = "Please provide a user name to associate with change submission";
                lblMessage.CssClass = "error";
                return;
            }

            // get currently active additional configuration
            var oldAddConfig = GetAdditionalConfiguration();

            // create a new AdditionalConfiguration based on user input
            tangible.Azure.AdditionalConfiguration.AdditionalConfiguration newAddConfig = null;
            try
            {
                newAddConfig = GetNewAdditionalConfiguration();
                newAddConfig.Timestamp = DateTime.UtcNow;
                newAddConfig.Author = txtUserName.Text;
            }
            catch (Exception ex)
            {
                lblMessage.Text = "Error creating new additional configuration: " + ex.ToString();
                lblMessage.CssClass = "error";
                return;
            }

            // update java and hybris packages
            try
            {
                UpdateJavaAndHybrisPackages(oldAddConfig, newAddConfig);
            }
            catch(Exception ex)
            {
                lblMessage.Text = "Error updating Packages: " + ex.ToString();
                lblMessage.CssClass = "error";
                return;
            }

            // upload new configuration file to the blob store
            string containerAndFilePath = null;
            try
            {
                containerAndFilePath = UploadAdditionalConfiguration(newAddConfig);
            }
            catch(Exception ex)
            {
                lblMessage.Text = "Error uploading new additional configuration: " + ex.ToString();
                lblMessage.CssClass = "error";
                return;
            }
            
            // Adapt Deployment-Configuration for additional-config-file and instance counts
            try
            {
                UpdateCloudServiceConfigurationAndInstanceCount(containerAndFilePath);

                lblMessage.Text = "New configuration posted successfully.";
                lblMessage.CssClass = "healthy";
            }
            catch(Exception ex)
            {
                lblMessage.Text = "Error setting new service configuration: " + ex.ToString();
                lblMessage.CssClass = "error";
            }
        }

        private void UpdateCloudServiceConfigurationAndInstanceCount(string containerAndFilePath)
        {
            var deploymentSlot = RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.DeploymentSlot");
            var subscriptionId = RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.SubscriptionId");
            var managementThumb = RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.ManagementCertThumb");
            var hostedServiceName = RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.HostedServiceName");

            var deployment = GetDeployment(subscriptionId, hostedServiceName, deploymentSlot, managementThumb);
            var clearConfig = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(deployment.Configuration));
            var xConfig = XElement.Parse(clearConfig);
            int arrInstanceCount;
            if (!int.TryParse(txtArrInstanceCount.Text, out arrInstanceCount))
                arrInstanceCount = RoleEnvironment.Roles["MicrosoftDXGermany.hybrisOnAzure.ArrRole"].Instances.Count;
            SetInstanceCount(xConfig, "MicrosoftDXGermany.hybrisOnAzure.ArrRole", arrInstanceCount);

            if (RoleEnvironment.Roles.ContainsKey("MicrosoftDXGermany.hybrisOnAzure.FrontendWorkerRole"))
            {
                int frontendInstanceCount;
                if (!int.TryParse(txtFrontendInstanceCount.Text, out frontendInstanceCount))
                    frontendInstanceCount = RoleEnvironment.Roles["MicrosoftDXGermany.hybrisOnAzure.FrontendWorkerRole"].Instances.Count;
                SetInstanceCount(xConfig, "MicrosoftDXGermany.hybrisOnAzure.FrontendWorkerRole", frontendInstanceCount);
            }

            SetAdditionalConfigFile(xConfig, containerAndFilePath);
            SetConfiguration(deployment, xConfig);
        }

        private void UpdateJavaAndHybrisPackages(AdditionalConfiguration oldAddConfig, AdditionalConfiguration newAddConfig)
        {
            CloudStorageAccount storageAccount;
            if (!RoleEnvironment.IsAvailable)
                storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true;");
            else
                storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();

            var oldDefaultBackOfficeConfig = oldAddConfig.Roles.First(r => r.Name == "MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole").Instances.First(i => i.Id == "default");
            var currentJavaPackage = oldDefaultBackOfficeConfig.Configurations.Any(c => c.Name == "JavaPackage") ? oldDefaultBackOfficeConfig.Configurations.First(c => c.Name == "JavaPackage").Value : string.Empty;
            var currentHybrisPackage = oldDefaultBackOfficeConfig.Configurations.Any(c => c.Name == "HybrisPackage") ? oldDefaultBackOfficeConfig.Configurations.First(c => c.Name == "HybrisPackage").Value : string.Empty;

            var newJavaPackage = txtJavaPackage.Text;
            if (string.IsNullOrEmpty(newJavaPackage))
                throw new Exception("No Java Package defined.");
            if (!blobClient.BlobExists("deployment/JavaPackages/" + newJavaPackage))
                throw new Exception("Could not find java package at deployment/JavaPackages/" + newJavaPackage);

            var newHybrisPackage = txtHybrisPackage.Text;
            if (string.IsNullOrEmpty(newHybrisPackage))
                throw new Exception("No Hybris Package defined.");
            if (!blobClient.DirectoryExists("deployment/HybrisPackages/" + newHybrisPackage))
                throw new Exception("Could not find hybris package at deployment/HybrisPackages/" + newHybrisPackage);
            
            if (currentJavaPackage != newJavaPackage)
                blobClient.CopyBlob("deployment/JavaPackages/" + newJavaPackage, "deployment/java.zip");
            if (currentHybrisPackage != newHybrisPackage)
                blobClient.CopyDirectory("deployment/HybrisPackages/" + newHybrisPackage, "deployment");
            
            // store new package information
            foreach (var role in newAddConfig.Roles.Where(r => r.Name == "MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole" || r.Name == "MicrosoftDXGermany.hybrisOnAzure.FrontendWorkerRole"))
                foreach(var instance in role.Instances)
                {
                    // remove old package information
                    instance.Configurations.RemoveAll(c => c.Name == "JavaPackage");
                    instance.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "JavaPackage", Value = newJavaPackage });
                    instance.Configurations.RemoveAll(c => c.Name == "HybrisPackage");
                    instance.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "HybrisPackage", Value = newHybrisPackage });
                }
        }

        private AdditionalConfiguration GetNewAdditionalConfiguration()
        {
            var newConfiguration = new AdditionalConfiguration();

            // Arr Configuration
            int desiredArrInstanceCount;
            if (!int.TryParse(txtArrInstanceCount.Text, out desiredArrInstanceCount))
                throw new Exception("Cannot parse ARR instance count");
            var arrRole = new tangible.Azure.AdditionalConfiguration.Role() { Name = "MicrosoftDXGermany.hybrisOnAzure.ArrRole", DesiredInstanceCount = desiredArrInstanceCount };
            newConfiguration.Roles.Add(arrRole);

            // default configuration
            var arrDefaultConfig = new tangible.Azure.AdditionalConfiguration.Instance() { Id = "default" };
            arrRole.Instances.Add(arrDefaultConfig);
            arrDefaultConfig.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "IsInMaintenance", Value = "False" });
           
            // instance configuration
            if (RoleEnvironment.IsAvailable)
                foreach (var arrInstance in RoleEnvironment.Roles["MicrosoftDXGermany.hybrisOnAzure.ArrRole"].Instances)
                {
                    var arrInstanceConfig = new tangible.Azure.AdditionalConfiguration.Instance() { Id = arrInstance.Id };
                    arrRole.Instances.Add(arrInstanceConfig);
                    var setMaintenance = Page.FindControl("chkMaintenance_" + arrInstance.Id) as CheckBox;
                    if (setMaintenance != null)
                        arrInstanceConfig.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "IsInMaintenance", Value = setMaintenance.Checked.ToString() });
                }

            // Frontend Worker Instances
            int desiredFrontendInstanceCount;
            if (!int.TryParse(txtArrInstanceCount.Text, out desiredFrontendInstanceCount))
                throw new Exception("Cannot parse FrontendWorker instance count");
            var frontendRole = new tangible.Azure.AdditionalConfiguration.Role() { Name = "MicrosoftDXGermany.hybrisOnAzure.FrontendWorkerRole", DesiredInstanceCount = desiredFrontendInstanceCount };
            newConfiguration.Roles.Add(frontendRole);

            // defaultConfiguration
            var frontendDefaultConfig = new tangible.Azure.AdditionalConfiguration.Instance() { Id = "default" };
            frontendRole.Instances.Add(frontendDefaultConfig);
            frontendDefaultConfig.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "StopHybris", Value = "False" });
            frontendDefaultConfig.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "Reboot", Value = String.Empty });

            // instance configuration
            if (RoleEnvironment.IsAvailable && RoleEnvironment.Roles.ContainsKey("MicrosoftDXGermany.hybrisOnAzure.FrontendWorkerRole"))
                foreach (var frontendInstance in RoleEnvironment.Roles["MicrosoftDXGermany.hybrisOnAzure.FrontendWorkerRole"].Instances)
                {
                    var frontendInstanceConfig = new tangible.Azure.AdditionalConfiguration.Instance() { Id = frontendInstance.Id };
                    frontendRole.Instances.Add(frontendInstanceConfig);
                    var stopHybris = Page.FindControl("chkStopHybris_" + frontendInstance.Id) as CheckBox;
                    if (stopHybris != null)
                        frontendInstanceConfig.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "StopHybris", Value = stopHybris.Checked.ToString() });
                    var reboot = Page.FindControl("chkReboot_" + frontendInstance.Id) as CheckBox;
                    if (reboot != null)
                        frontendInstanceConfig.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "Reboot", Value = (reboot.Checked) ? newConfiguration.Timestamp.ToString() : string.Empty });
                }

            // BackOffice Worker Instances
            var backOfficeRole = new tangible.Azure.AdditionalConfiguration.Role() { Name = "MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole" };
            newConfiguration.Roles.Add(backOfficeRole);

            // defaultConfiguration
            var backOfficeDefaultConfig = new tangible.Azure.AdditionalConfiguration.Instance() { Id = "default" };
            backOfficeRole.Instances.Add(frontendDefaultConfig);
            backOfficeDefaultConfig.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "StopHybris", Value = "False" });
            backOfficeDefaultConfig.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "Reboot", Value = String.Empty });

            // instance configuration
            if (RoleEnvironment.IsAvailable && RoleEnvironment.Roles.ContainsKey("MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole"))
                foreach (var backOfficeInstance in RoleEnvironment.Roles["MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole"].Instances)
                {
                    var backOfficeInstanceConfig = new tangible.Azure.AdditionalConfiguration.Instance() { Id = backOfficeInstance.Id };
                    backOfficeRole.Instances.Add(backOfficeInstanceConfig);
                    var stopHybris = Page.FindControl("chkStopHybris_" + backOfficeInstance.Id) as CheckBox;
                    if (stopHybris != null)
                        backOfficeInstanceConfig.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "StopHybris", Value = stopHybris.Checked.ToString() });
                    var reboot = Page.FindControl("chkReboot_" + backOfficeInstance.Id) as CheckBox;
                    if (reboot != null)
                        backOfficeInstanceConfig.Configurations.Add(new tangible.Azure.AdditionalConfiguration.ConfigurationItem() { Name = "Reboot", Value = (reboot.Checked) ? newConfiguration.Timestamp.ToString() : string.Empty });
                }

            return newConfiguration;
        }
        private tangible.Azure.AdditionalConfiguration.AdditionalConfiguration GetAdditionalConfiguration()
        {
            var deploymentSlot = RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.DeploymentSlot");
            var subscriptionId = RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.SubscriptionId");
            var managementThumb = RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.ManagementCertThumb");
            var hostedServiceName = RoleEnvironment.GetConfigurationSettingValue("HybrisOnAzure.HostedServiceName");

            var deployment = GetDeployment(subscriptionId, hostedServiceName, deploymentSlot, managementThumb);
            var clearConfig = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(deployment.Configuration));
            var xConfig = XElement.Parse(clearConfig);
            var additionalConfigFileAndContainer = xConfig.Descendants(nsConfig + "Setting").FirstOrDefault(elem => elem.Attribute("name") != null && elem.Attribute("name").Value == "tangible.Azure.AdditionalConfiguration").Attribute("value").Value;

            CloudStorageAccount storageAccount;
            if (!RoleEnvironment.IsAvailable)
                storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true;");
            else
                storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blob = blobClient.GetBlobReference(additionalConfigFileAndContainer);
            using(var ms = new MemoryStream())
            {
                blob.DownloadToStream(ms);
                ms.Position = 0;

                var serializer = new XmlSerializer(typeof(tangible.Azure.AdditionalConfiguration.AdditionalConfiguration));
                return serializer.Deserialize(ms) as tangible.Azure.AdditionalConfiguration.AdditionalConfiguration;
            }
        }
        private string UploadAdditionalConfiguration(tangible.Azure.AdditionalConfiguration.AdditionalConfiguration configuration)
        {
            var fileName = "config/" + configuration.Timestamp.ToString("yyyy-MM-dd_HH-mm-ss") + ".xml";

            CloudStorageAccount storageAccount;
            if (!RoleEnvironment.IsAvailable)
                storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true;");
            else
                storageAccount = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            var blob = blobClient.GetBlockBlobReference(fileName, true);

            using (var ms = new MemoryStream())
            {
                // create a serializer
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(tangible.Azure.AdditionalConfiguration.AdditionalConfiguration));
                serializer.Serialize(ms, configuration);
                ms.Position = 0;

                blob.UploadFromStream(ms);
            }

            return fileName;
        }

        XNamespace nsConfig = "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration";
        private tangible.Azure.Management.Model.Deployment GetDeployment(string subscriptionId, string hostedServiceName, string deploymentSlot, string managementThumb)
        {
            var subscription = new tangible.Azure.Management.Model.Subscription() { Id = subscriptionId, ManagementCertificateThumbprint = managementThumb };
            var cloudService = subscription.HostedServices.FirstOrDefault(hs => hs.ServiceName == hostedServiceName);

            return (deploymentSlot.ToLower() == "production") ? cloudService.Production : cloudService.Staging;
        }
        private void SetInstanceCount(XElement xConfig, string roleName, int instanceCount)
        {
            var xRole = xConfig.Descendants(nsConfig + "Role").FirstOrDefault(elem => elem.Attribute("name") != null && elem.Attribute("name").Value == roleName);
            var xInstances = xRole.Descendants(nsConfig + "Instances").FirstOrDefault();
            xInstances.Attribute("count").Value = instanceCount.ToString();
        }
        private void SetAdditionalConfigFile(XElement xConfig, string p)
        {
            foreach(var xRole in xConfig.Descendants(nsConfig + "Role"))
            {
                var xSetting = xRole.Descendants(nsConfig + "Setting").FirstOrDefault(elem => elem.Attribute("name") != null && elem.Attribute("name").Value == "tangible.Azure.AdditionalConfiguration");
                xSetting.Attribute("value").Value = p;
            }
        }
        private void SetConfiguration(tangible.Azure.Management.Model.Deployment deployment, XElement xConfig)
        {
            new tangible.Azure.Management.Api.HostedServicesApi().SetConfiguation(xConfig, deployment.DeploymentSlot, deployment.HostedService.ServiceName, deployment.HostedService.Subscription.Id, deployment.HostedService.Subscription.ManagementCertificateThumbprint);
        }
    }
}
