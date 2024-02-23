<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Analytics.aspx.cs" Inherits="Analytics" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style>
        .w-200{ width: 200px;}
        .w-300{ width: 300px; }
        .w-108 {width:108px; text-align:right;}
        .w-96 {width:96px; text-align:right;}
        .w-84 {width:84px; text-align:right;}
        .w-72 {width:72px; text-align:right;}
        .w-64 {width:64px; text-align:right;}
        .w-56 {width:56px; text-align:right;}
        .w-48 {width:48px; text-align:right;}
        .w-40 {width:40px; text-align:right;}
        .w-0 {width:0; display:none;}
        .form-rounded {border-radius:4px; border-width:1px; padding: 2px;}
        .position-right {position:absolute;right:100px;border:solid 1px; padding:10px;}
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <p>
                Data file:
                <asp:TextBox ID="uiDataFile"  CssClass="form-rounded w-300" runat="server" />
                Data for Year:
                <asp:DropDownList ID="uiDdlYear" CssClass="form-rounded w-56" runat="server">
                    <asp:ListItem Text="2023"></asp:ListItem>
                    <asp:ListItem Text="2024"></asp:ListItem>
                    <asp:ListItem Text="2025"></asp:ListItem>
                </asp:DropDownList>
            </p>
            <div class="position-right">
                <h3>Parameters</h3>
                <p>Show Consecutive Dates
                <asp:CheckBox ID="uiChkShowDates" CssClass="form-rounded w-40" runat="server" />
                </p>
                <p>
                Show Specific Hits
                <asp:CheckBox ID="uiChkShowSpecificData" CssClass="form-rounded w-40" runat="server" />
                </p>
                <p>
                    Change Date Format
                    <asp:CheckBox ID="uiChkChangeDateFormat" CssClass="form-rounded w-40" runat="server" />
                </p>
            </div>
            
        </div>
        <div>
            Enter search text:
            <asp:TextBox ID="uiSearchText" CssClass="form-rounded w-200" runat="server" />
            <asp:Button ID="btnSearch" runat="server" Text="Search" OnClick="GetSearchResults" CssClass="nav-link" />
        </div>
        <div>
            <br />
            <table>
                <tr><td class="w-200">Date</td><td></td></tr>                
                <asp:Literal ID="uiLtlOutput" runat="server" />
            </table>
        </div>
    </form>
</body>
</html>
