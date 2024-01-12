<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Analytics.aspx.cs" Inherits="Analytics" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <p>
                Data file:
                <asp:TextBox ID="uiDataFile"  CssClass="form-rounded w-72" runat="server" />
            </p>
        </div>
        <div>
            Enter search text:
            <asp:TextBox ID="uiSearchText" CssClass="form-rounded w-72" runat="server" />
            <asp:Button ID="btnSearch" runat="server" Text="Search" OnClick="GetSearchResults" CssClass="nav-link" />
        </div>
        <div>
            <p>
                <asp:Literal ID="uiLtlOutput" runat="server" />
            </p>
        </div>
    </form>
</body>
</html>
