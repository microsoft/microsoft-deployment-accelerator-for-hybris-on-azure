@echo off

:: This script start, stops and restarts standalone solr for hybris on azure
:: (c) tanigble engineering GmbH, 2015

:: the value of this variable will be replaced on startup by the hybris on azure platform
set ISMASTER="<#%HybrisOnAzure.IndexMaster%#>"
call :UpCase ISMASTER

:: determine if this server needs master/slave configuration
if %ISMASTER%=="TRUE" (
  :: set options for solr master
  set JAVA_OPTIONS=-server -Xms2048m -Xmx2048m -jar -Dcom.sun.management.jmxremote.port=9883 -Dcom.sun.management.jmxremote.ssl=false -Dcom.sun.management.jmxremote.authenticate=false -Dcom.sun.management.jmxremote -DSTOP.PORT={solrstopport} -DSTOP.KEY={solrstopkey} -Dsolr.solr.home=. -Denable.master=true -Djetty.port={solrport} -Dsolr.data.dir={solrdatadir} start.jar
  )
if %ISMASTER%=="FALSE" (
  :: set options for solr slave
  set JAVA_OPTIONS=-server -Xms2048m -Xmx2048m -jar -Dcom.sun.management.jmxremote.port=9883 -Dcom.sun.management.jmxremote.ssl=false -Dcom.sun.management.jmxremote.authenticate=false -Dcom.sun.management.jmxremote -DSTOP.PORT={solrstopport} -DSTOP.KEY={solrstopkey} -Dsolr.solr.home=. -Denable.slave=true -Djetty.port={solrport} -Dmaster.host={solrmasterip}  -Dsolr.data.dir={solrdatadir} start.jar
  )

set LOG_FILE=%SOLR_DIR%\logs\solr.log


if ""%1"" == ""restart"" goto doRestart
if ""%1"" == ""start"" goto doStart
if ""%1"" == ""stop"" goto doStop
if ""%1"" == """" goto doUsage


goto EOF

:doStart

echo "Starting Solr"
cd %SOLR_DIR%
cmd /c java %JAVA_OPTIONS%

goto EOF 

:doStop

echo "Stoping Solr"
cd %SOLR_DIR%
cmd /c java %JAVA_OPTIONS% --stop

goto EOF 
:doRestart
echo "Starting Solr"
cd %SOLR_DIR%
cmd /c java %JAVA_OPTIONS% --stop
timeout 2
cmd /c java %JAVA_OPTIONS%

goto EOF 

:doUsage
@echo off
echo Usage: "%0 {start|stop|restart}"

goto EOF


:UpCase
:: Subroutine to convert a variable VALUE to all UPPER CASE.
:: The argument for this subroutine is the variable NAME.
FOR %%i IN ("a=A" "b=B" "c=C" "d=D" "e=E" "f=F" "g=G" "h=H" "i=I" "j=J" "k=K" "l=L" "m=M" "n=N" "o=O" "p=P" "q=Q" "r=R" "s=S" "t=T" "u=U" "v=V" "w=W" "x=X" "y=Y" "z=Z") DO CALL SET "%1=%%%1:%%~i%%"
GOTO:EOF

:EOF