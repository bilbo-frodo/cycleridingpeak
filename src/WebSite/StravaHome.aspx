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
        a.linkBlack {color:black;}
        .w-200{ width: 200px !important; }
        .w-108 {width:108px; text-align:right;}
        .w-96 {width:96px; text-align:right;}
        .w-84 {width:84px; text-align:right;}
        .w-72 {width:72px; text-align:right;}
        .w-64 {width:64px; text-align:right;}
        .form-rounded {border-radius:4px; border-width:0px; padding: 2px;}
        .error {color:red;}
        .ui-datepicker table {
            width: 100%;
            font-size: .7em;
            border-collapse: collapse;
            font-family:verdana;
            margin: 0 0 .4em;
        }
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
        var tableSort = tableSort || {};

        $(function () {
            strava.init();
            strava.hookupGetAthlete.init();

            tableSort.init();
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

        tableSort = function () {
            var
                getNumberFromString = function (text) {
                    var regex = "/(^\d+\.\d{1,2}|\d+)(.+$)/i";
                    /*
                     * /
                     *    (           # start of optional group
                     *        ^\d+    # any no of digits
                     *        \.      # followed by a .
                     *        \d{1,2} # followed by 1 or 2 digits
                     *      |         # OR
                     *        \d+     # any no of digits
                     *    )
                     *    (.+$)  # followed by any number of chars
                     * 
                     * /i  #case insensitive
                     */
                    var rc = Number(text.replace(/(^\d+\.\d{1,2}|\d+)(.+$)/i, '$1'));
                    return rc;
                },

                /*
                 * from https://www.w3schools.com/howto/howto_js_sort_table.asp
                 */
                sortTable = function (n) {
                    var table, rows, switching, i, x, y, shouldSwitch, dir, switchcount = 0;
                    table = document.getElementById("resultsTable");
                    switching = true;
                    //Set the sorting direction to ascending:
                    dir = "asc";
                    //Make a loop that will continue until no switching has been done:
                    while (switching) {
                        //start by saying: no switching is done:
                        switching = false;
                        rows = table.rows;
                        //Loop through all table rows (except the first, which contains table headers, and the last/footer):
                        for (i = 1; i < (rows.length - 2); i++) {
                            //start by saying there should be no switching:
                            shouldSwitch = false;
                            //get the two elements you want to compare, one from current row and one from the next:
                            x = rows[i].getElementsByTagName("TD")[n];
                            y = rows[i + 1].getElementsByTagName("TD")[n];
                            //check if the two rows should switch place, based on the direction, asc or desc:
                            //the regex replace extracts the first number in the string e.g. returns '24' from '24(15)'                    
                            if (dir == "asc") {
                                if (getNumberFromString(x.innerHTML) > getNumberFromString(y.innerHTML)) {
                                    //if so, mark as a switch and break the loop:
                                    shouldSwitch = true;
                                    break;
                                }
                            } else if (dir == "desc") {
                                if (getNumberFromString(x.innerHTML) < getNumberFromString(y.innerHTML)) {
                                    //if so, mark as a switch and break the loop:
                                    shouldSwitch = true;
                                    break;
                                }
                            }
                        }
                        if (shouldSwitch) {
                            //If a switch has been marked, make the switch and mark that a switch has been done:
                            rows[i].parentNode.insertBefore(rows[i + 1], rows[i]);
                            switching = true;
                            //Each time a switch is done, increase this count by 1:
                            switchcount++;
                        } else {
                            //If no switching has been done AND the direction is "asc", set the direction to "desc" and run the while loop again.
                            if (switchcount == 0 && dir == "asc") {
                                dir = "desc";
                                switching = true;
                            }
                        }
                    }
                },
                init = function () {
                    // wire up table column headers - clicking a table header sorts by that column
                    $("#column2").click(function () { sortTable(2); });
                    $("#column3").click(function () { sortTable(3); });
                    $("#column4").click(function () { sortTable(4); });
                    $("#column5").click(function () { sortTable(5); });
                    $("#column6").click(function () { sortTable(6); });
                    $("#column7").click(function () { sortTable(7); });
                };
            return {
                init: init,
                sortTable: sortTable
            };
        }();
     
    </script>
</html>
