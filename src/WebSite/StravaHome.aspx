<%@ Page Language="C#" EnableViewState="false" AutoEventWireup="true" CodeFile="StravaHome.aspx.cs" Inherits="StravaHome" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>strava.net</title>
    <link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/4.4.1/css/bootstrap.min.css" />
    <link rel="stylesheet" href="//code.jquery.com/ui/1.11.2/themes/smoothness/jquery-ui.css">    
    <script src="https://code.jquery.com/jquery-1.12.4.js"></script>
    <script src="https://code.jquery.com/ui/1.12.1/jquery-ui.js"></script>
    <script src="https://maxcdn.bootstrapcdn.com/bootstrap/4.4.1/js/bootstrap.min.js"></script>

    <style>
        div, table {font-size:12px;}
        td.alignright {text-align:right;}
        td.highlighted {color:orange;}
        .w-200{ width: 200px !important; }
        .w-108 {width:108px; text-align:right;}
        .w-96 {width:96px; text-align:right;}
        .w-84 {width:84px; text-align:right;}
        .w-72 {width:72px; text-align:right;}
        .w-64 {width:64px; text-align:right;}
        .form-rounded {border-radius:4px; border-width:0px; padding: 2px;}
        .error {color:red;}
    </style>
</head>
<body>
    <form id="form1" runat="server" dir="auto" defaultbutton="GetActivities">
    <div class="container-fluid">
        <!-- A grey horizontal navbar that becomes vertical on small screens -->
        <nav class="navbar navbar-expand-sm bg-light" style="width:1200px !important;">
            <ul class="navbar-nav">
                <li class="nav-item">
                    <asp:Button ID="GetTokenButton" runat="server" Text="Authorise" OnClick="GetCodeButton_Click" CssClass="nav-link" />
                </li>    
                <li class="nav-item">
                    <input type="button" name="GetAthlete" value="Get Athlete" id="GetAthlete" class="nav-link" />
                </li>
                <li class="nav-item">
                    <asp:Button ID="GetActivities" runat="server" Text="Get Activities" OnClick="GetActivityDetailsClick" CssClass="nav-link" />
                </li>
                <li class="nav-item nav-link">
                    <label for="uiTxtStartDate">Between:</label>
                    <asp:TextBox ID="uiTxtStartDate" CssClass="form-rounded w-84" runat="server" />
                </li>
                <li class="nav-item nav-link">
                    <label for="uiTxtEndDate">and</label>
                    <asp:TextBox ID="uiTxtEndDate" CssClass="form-rounded w-84" runat="server" />
                </li>
                <li class="nav-item nav-link"><asp:Literal ID="uiLtlStatus" runat="server"></asp:Literal></li>
            </ul>
        </nav>
        <ul class="navbar navbar-expand-sm">
            <li class="nav-link">
            <asp:RadioButton ID="uiRbCycling" Checked="true" Text="Cycling" GroupName="ActivityType" runat="server" />
                </li>
            <li class="nav-link">
            <asp:RadioButton ID="uiRbSpinning" Text="Spinning" GroupName="ActivityType" runat="server" />
                </li>
            <li class="nav-link">
            <asp:RadioButton ID="uiRbParkrun" Text="Park Run" GroupName="ActivityType" runat="server" />
                </li>
        </ul>
        
        
        <div class="row">
            <div class="col">
             <asp:Label ID="athleteName" runat="server" Text=""></asp:Label>
            </div>
        </div>
        <div class="row">
            <div class="col">
            <asp:Literal ID="uiLtlOutput" runat="server"></asp:Literal>
            </div>
        </div>
        <div class="row">
            <div class="col">
            <asp:Literal ID="uiLtlSummary" runat="server"></asp:Literal>
            </div>
        </div>
    </div>
    </form>
</body>
    <script>
        var strava = strava || {};

        $(function () {
            strava.init();
            strava.hookupGetAthlete.init();
        });

        strava.init = function () {
            $("#uiTxtStartDate").datepicker({
                dateFormat: "dd/mm/yy"
            });
            $("#uiTxtEndDate").datepicker({
                dateFormat: "dd/mm/yy"
            });
        };

        strava.hookupGetAthlete = function () {
            'use strict';
            var
                logError = function (message, object) {

                    /// <summary>Safe error logging.</summary>
                    if (window && window.console && window.console.error) {
                        window.console.error("Error - " + message, object);
                    }
                },
                setAthlete = function (json) {
                    if ((json === null) || (json === undefined)) {
                        alert('no data returned');
                    }

                    if ((json.firstname !== null) && (json.lastname !== null)) {
                        $('#athleteName').text(json.firstname + ' ' + json.lastname);
                    }

                },
                getAthlete = function () {

                    $.ajax({
                        url: "/StravaApiHandler.ashx",
                        type: 'POST',
                        data: '',
                        dataType: 'json',
                        async: true,
                        global: false
                    })
                        .done(function (data, textStatus, jqXHR) {
                            setAthlete(data);
                        })
                        .fail(function (error) {
                            alert('ERROR' + error.responseText);
                        })
                },
                hookupEvents = function () {                   
                    $('#GetAthlete').click(function () {
                        getAthlete();
                        //alert('get athlete clicked');
                    });
                },
                init = function () {
                    hookupEvents();

                };

            return {
                init: init
            };
        }();

    </script>
</html>
