rem Install Application Request Routing and its dependencies and patches

if exist %systemdrive%\arr-installed.txt goto skip

cd /d "%~dp0"
msiexec /i webfarm_amd64_en-US.msi /qn /log installWebfarm.log
msiexec /i ExternalDiskCache_amd64_en-US.msi /qn /log installExtCache.log
msiexec /i ExternalDiskCachePatch_amd64.msp /qn /log installExtCachePatch.log
msiexec /i requestRouter_amd64_en-US.msi /qn /log installARR.log

time /t >> %systemdrive%\arr-installed.txt

:skip