@REM --------------------------------------------------------------------------
@REM This command file executes a given JAR file
@REM --------------------------------------------------------------------------

@REM If you want the JAR to be executed only once, enable the next line
@REM if exist %SystemDrive%\jar-executed.txt goto skip

@REM Read a value from the .cscfg file and store it in an environment variable
@REM that can be read from within Java
set CSCFG_HybrisOnAzure.JavaHomeDirectory="<#%HybrisOnAzure.JavaHomeDirectory%#>"

@REM Read a public endpoint from the RoleEnvironment
set RE_TomcatIP=<#%RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["TomcatHttp"].IPEndpoint.Address%#>
set RE_TomcatPort=<#%RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["TomcatHttp"].IPEndpoint.Port%#>
set RE_TomcatEndpoint=<#%RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["TomcatHttp"].IPEndpoint%#>

@REM start the java program
call "<#%HybrisOnAzure.JavaHomeDirectory%#>"\bin\java.exe -jar %~dp0MyJavaProgram.jar


time /t >> %SystemDrive%\jar-executed.txt
:skip