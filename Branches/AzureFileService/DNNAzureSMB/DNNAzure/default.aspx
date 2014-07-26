<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="DNNAzure._default" %>
<!DOCTYPE html>
<html>
<head runat="server">
    <meta http-equiv="X-UA-Compatible" content="IE=Edge" >    
    <title>Site under maintenance</title>
    <link href="css/styles.css" rel="stylesheet" type="text/css" />
    <meta http-equiv="Refresh" content="30;URL=" />
    <style type="text/css">
        BODY { font-family: 'Segoe UI',Arial; font-weight: lighter; overflow:hidden;}
        H1 { font-family: 'Segoe UI', Arial; font-weight: lighter; color: #0094ff}
        #message { text-align: center; vertical-align: middle; width: 100%;}    
        #contents { margin-top:  25%;}
    </style>
</head>
<body>
<div id="message">
    <div id="contents">
        <h1>Site Under Maintenance</h1>
        <p>This site is currently under maintenance. We should be back shortly. Thank you for your patience.</p>
    </div>
</div>    
    <!-- Site is being deployed -->
    <form id="form1" runat="server">
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
