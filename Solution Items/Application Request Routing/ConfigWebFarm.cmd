@echo off
rem echo off -> cmdlet freezes with echo on

@REM -----------------------------------
@REM Configure ApplicationRequestRouting
@REM -----------------------------------

rem Enable Reverse Proxy feature in ARR – will modify D:\Windows\System32\inetsrv\config\applicationHost.config
%systemroot%\system32\inetsrv\APPCMD set config -section:proxy /enabled:true /COMMIT:MACHINE/WEBROOT/APPHOST > appcmd-arr.log

rem Delete previous existing web farm to avoid errors in this script
%systemroot%\system32\inetsrv\appcmd.exe clear config -section:webFarms /commit:apphost

rem Create empty web farm
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /+"[name='<#%tangible.Azure.ARR.WebFarmName%#>']" /commit:apphost
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.affinity.useCookie:"True" /commit:apphost
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.loadBalancing.algorithm:"WeightedRoundRobin" /commit:apphost

rem Disable timeouts and set keep alive
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.protocol.httpVersion:"PassThrough" /commit:apphost
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.protocol.keepAlive:"true" /commit:apphost
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.protocol.timeout:"01:00:00" /commit:apphost

rem Enable Health monitoring
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.healthCheck.url:"<#%tangible.Azure.ARR.HealthCheckUrl%#>"
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.healthCheck.responseMatch:"<#%tangible.Azure.ARR.HealthCheckResponseMatch%#>"
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.healthCheck.interval:"<#%tangible.Azure.ARR.HealthCheckInterval%#>"
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.healthCheck.timeout:"<#%tangible.Azure.ARR.HealthCheckTimeout%#>" /commit:apphost

rem Disable Disk Cache
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.protocol.cache.enabled:"false" /commit:apphost
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.protocol.cache.validationInterval:"00:00:00" /commit:apphost

rem Set no buffer for constant data flow
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.protocol.minResponseBuffer:"0" /commit:apphost
%systemroot%\system32\inetsrv\appcmd.exe set config -section:webFarms /[name='<#%tangible.Azure.ARR.WebFarmName%#>'].applicationRequestRouting.protocol.responseBufferLimit:"0" /commit:apphost