==== ConfigureWebFarm.cmd =====
This commandlet is part of the HybrisForAll deployment and sets up the IIS Webfarm. Is is to be contained in a Zip file named ARR30.zip.

==== startup.cmd ====
This commandlet is part of the HybrisForAll deployment and installs bits necessary for the webfarm and application request routing. Is is to be contained in a Zip file named ARR30.zip.

The ARR30.zip file needs to contain the following files besides the two commandlets:

WebFarm Framework 1.1
http://download.microsoft.com/download/5/7/0/57065640-4665-4980-A2F1-4D5940B577B0/webfarm_v1.1_amd64_en_US.msi
   Renamed to webfarm_amd64_en-US.msi
 
Application Request Routing 3.0 for IIS
http://www.microsoft.com/en-us/download/details.aspx?id=39715 
   Renamed to requestRouter_amd64_en-US.msi

External Disk Cache V1
http://download.microsoft.com/download/3/4/1/3415F3F9-5698-44FE-A072-D4AF09728390/ExternalDiskCache_amd64_en-US.msi

External Disk Cache Patch:
Start-BitsTransfer -Source http://download.microsoft.com/download/D/E/9/DE90D9BD-B61C-43F5-8B80-90FDC0B06144/ExternalDiskCachePatch_amd64.msp


======================================
Please refer to chapter 2.9 "Application Request Routing" of the Hybris On Azure - For All PDF documentation.