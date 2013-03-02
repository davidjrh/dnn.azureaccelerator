<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="DNNAzure._default" %>
<!DOCTYPE html>

<html>
<head runat="server">
    <meta http-equiv="X-UA-Compatible" content="IE=Edge" >    
    <title>Deployment in progress... - DotNetNuke Azure Accelerator</title>
    <link href="css/styles.css" rel="stylesheet" type="text/css" />
    <meta http-equiv="Refresh" content="30;URL=" />
</head>
<body>
    <form id="form1" runat="server">
    <div id="#Deploying">
    <h1>Deployment in progress...</h1>
    <p>This site is currently being deployed on Windows Azure. Please wait until the site has been completely deployed.</p>
    </div>
    <asp:Label runat="server" ID="lblError" EnableViewState="False" CssClass="Error"></asp:Label>
    <asp:GridView ID="grdLog" runat="server" AutoGenerateColumns="False">
        <Columns>
            <asp:BoundField DataField="Timestamp" HeaderText="Datetime">
            <ItemStyle CssClass="Info" />
            </asp:BoundField>
            <asp:BoundField DataField="Role" HeaderText="Role" />
            <asp:BoundField DataField="RoleInstance" HeaderText="Instance" />
            <asp:BoundField DataField="Message" HeaderText="Message" />
        </Columns>
    </asp:GridView>
    </form>
</body>
</html>
