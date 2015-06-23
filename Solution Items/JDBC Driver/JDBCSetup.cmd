@REM --------------------
@REM JDBC driver settings
@REM --------------------
if exist %SystemDrive%\registry-edited.txt goto skip

REM Workaround for JDBC to SQL Azure
REG ADD HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters /f /v KeepAliveTime /t REG_DWORD /d 30000 >> %SystemDrive%\registry-edited.txt
REG ADD HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters /f /v KeepAliveInterval /t REG_DWORD /d 1000 >> %SystemDrive%\registry-edited.txt
REG ADD HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters /f /v TcpMaxDataRetransmission /t REG_DWORD /d 10 >> %SystemDrive%\registry-edited.txt

time /t >> %SystemDrive%\registry-edited.txt
shutdown /r /t 1

:skip