=== Restrictions ===
- the Java Application must terminate
- the Java Application must be compatible with the Java Version deployed

=== In Order to run a JAR Java Application on Server startup follow these steps: ===
1. Edit RunJar.cmd: Place proper JAR filename

2. Zip JAR and RunJar.cmd into a single Zip file
   e.g.    RunJar.zip
           /- RunJar.cmd
           /- MyJavaProgram.jar

3. Upload RunJar.zip into /deployment/ container in BlobStorage configured in CloudConfiguration "StorageConnectionString" 

4. Download RunJar.zip at Server startup by adding the following part to CloudConfiguration "tangible.Azure.Startup.Download.Downloads"
   	   deployment/RunJar.zip|C:\RunJar|true;
   
   That CloudConfiguration contains a semicolon separated list of downloads to be made at startup. 
   A download is specified by three parameters separated by the pipe character '|'.
   1st parameter is the blob to be downloaded.
   2nd parameter is a directory the blob is to be downloaded to
   3rd parameter 'true' if the downloaded blob is a zip file and needs to be unzipped, 'false' otherwise

5. Execute RunJar.bat at Server startup after the download by adding the following part to CloudConfiguration "tangible.Azure.Startup.StartupCommands.Commands"
           C:\RunJar\RunJar.cmd|30000|true;

   That CloudConfiguration contains a semicolon separated list of jobs to be run at startup.
   A job is specified by three parameters separated by the pipe character '|'.
   1st parameter is the file to be executed (.bat, .cmd, .exe, ...)
   2nd parameter is the timeout in ms after which the execution is cancelled
   3rd parameter 'true' if the job needs to be executed in the director where it is stored (as there is made a copy before executing), 'false' otherwise

=== Access Azure RoleEnvironment ===
Because the JAR is not the RoleEntryPoint of the Cloud service, the RoleEnvironment will not be available using the Azure JAVA SDK.
But you can access ConfigurationSetting values from the cloud configuration and RoleEnvironment information by adding placeholders
in the RunJar.cmd file which will be replaced before executing the RunJar.cmd. (Examples are contained in the .cmd)

Configuration Setting value: Place the name of the configuration settinig in <#% %#> tags.
	Example: <#%HybrisOnAzure.JavaHomeDirectory%#> will be replaced by its value from the .cscfg

RoleEnvironment information: Place the expression to the desired information in <#% %#> tags.
	Example: <#%RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["TomcatHttp"].IPEndpoint.Port%#> will be replaced by the port number defined by the RoleEnvironment.
	Reference: https://msdn.microsoft.com/en-us/library/microsoft.windowsazure.serviceruntime.roleenvironment_members.aspx
