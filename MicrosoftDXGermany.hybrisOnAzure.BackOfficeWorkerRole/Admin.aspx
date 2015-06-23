<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="MicrosoftDXGermany.hybrisOnAzure.BackOfficeWorkerRole.Admin"  %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Hybris on Azure : Admin Page</title>
    <style type="text/css">
        body {
            font-family:Verdana;
            height:100%;
            width:100%;
            font-size:10pt;
        }
        h1 {
            font-size:16pt;
        }
        h2 {
            font-size:13pt;
        }
        div.centerFrame {
            width:1000px;
            display:block;
            margin-left:auto;
            margin-right:auto;
            border:solid 1px black;
            padding:5px;
        }
        div.default {
            width:150px;
            height:100px;
            padding:5px;
            background-color:lightblue;
        }
        div.healthy {
            width:150px;
            height:100px;
            padding:5px;
            background-color:green;
        }
        div.warning {
            width:150px;
            height:100px;
            padding:5px;
            background-color:orange;
        }
        div.error {
            width:150px;
            height:100px;
            padding:5px;
            background-color:red;
        }
        span.healthy {
            color:green;
        }
        span.warning {
            color:orange;
        }
        span.error {
            color:red;
        }
        table.centerTable {
            width:960px;
            display:block;
            margin-left:auto;
            margin-right:auto;
        }
        table.instanceOptions {
            margin-top:5px;
        }
        th {
            font-weight:normal;
            text-align:left;
            width:150px;
            vertical-align:top;
        }
        td {
            text-align:left;
            vertical-align:top;
        }
        div.pluginState {
            vertical-align:top;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <table border="0">
            <tr>
                <td>
                    <h1>Administration Page</h1>
                </td>
                <td>
                    <a href="Admin.aspx">Refresh</a>
                </td>
            </tr>
        </table>
        
        <h2>Application Request Routing Tier</h2>
        <div class="centerFrame">
            <asp:Table ID="tblArr" runat="server" CssClass="centerTable" BorderColor="0" CellSpacing="5" />
            <table class="centerTable" style="margin-top:10px;">
                <tr>
                    <th>Instances:</th>
                    <td>
                        <asp:TextBox ID="txtArrInstanceCount" runat="server" Width="50px" MaxLength="2" TextMode="SingleLine" />
                    </td>
                </tr>
            </table>
        </div>

        <h2>Frontend Worker</h2>
        <div class="centerFrame" runat="server">
            <asp:Table ID="tblFrontend" runat="server" CssClass="centerTable" BorderColor="0" CellSpacing="5" />
            <table class="centerTable" style="margin-top:10px;">
                <tr>
                    <th>Instances:</th>
                    <td>
                        <asp:TextBox ID="txtFrontendInstanceCount" runat="server" Width="50px" MaxLength="2" TextMode="SingleLine" />
                    </td>
                </tr>
            </table>
        </div>

        <h2>BackOffice Worker</h2>
        <div class="centerFrame">
            <asp:Table ID="tblBackOffice" runat="server" CssClass="centerTable" BorderColor="0" CellSpacing="5" />
        </div>

        <h2>Deployment</h2>
        <div class="centerFrame">
            <table border="0" class="centerTable">
                <tr>
                    <th>Java:</th>
                    <td>
                        <asp:TextBox ID="txtJavaPackage" runat="server" width="300" />
                    </td>
                </tr>
                <tr>
                    <th>Hybris:</th>
                    <td>
                        <asp:TextBox ID="txtHybrisPackage" runat="server" Width="300" />
                    </td>
                </tr>
            </table>
        </div>
        <hr />
        <table border="0">
            <tr>
                <td colspan="2">
                    Benutzername: <asp:TextBox ID="txtUserName" runat="server" Width="250" />
                </td>
            </tr>
            <tr>
                <td>
                    <asp:Button Text="Submit changes" ID="cmdSubmit" runat="server" OnClick="cmdSubmit_Click" />
                </td>
                <td>
                    <asp:Label ID="lblMessage" runat="server" />
                </td>
            </tr>
        </table>
        
        <hr />
        <h1>Deployment History</h1>
        <asp:Table ID="tblDeploymentHistory" runat="server" />
    </form>
</body>
</html>
