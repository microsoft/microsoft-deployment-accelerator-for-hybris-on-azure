@REM --------------------
@REM This script connects to a Windows Azure Files account
@REM --------------------
if not exist <#%HybrisOnAzure.BackOfficeShare.DesiredDrive%#> goto create

:remove 
echo <#%HybrisOnAzure.BackOfficeShare.DesiredDrive%#> exists, deleting...
net use <#%HybrisOnAzure.BackOfficeShare.DesiredDrive%#> /delete

:create
echo Using AzureFiles account="<#%HybrisOnAzure.BackOfficeShare.AccountName%#>" share="<#%HybrisOnAzure.BackOfficeShare.ShareName%#>"
net use <#%HybrisOnAzure.BackOfficeShare.DesiredDrive%#> \\<#%HybrisOnAzure.BackOfficeShare.AccountName%#>.file.core.windows.net\<#%HybrisOnAzure.BackOfficeShare.ShareName%#> /u:<#%HybrisOnAzure.BackOfficeShare.AccountName%#> <#%HybrisOnAzure.BackOfficeShare.AccountKey%#>