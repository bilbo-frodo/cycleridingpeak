using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Globalization;

public partial class Analytics : System.Web.UI.Page
{
    protected string filePath;
    protected string strCurrentDate, nextLineDate=string.Empty;
    protected string searchHit=string.Empty;
    protected bool dateFound = false;
    protected DateTime dtDate, dtLastDate=DateTime.MinValue;
    protected string year;
    protected bool showConsecutiveDates = false;

    protected void Page_Load(object sender, EventArgs e)
    {
        // Specify the path to the text file
        filePath = "C:/BillShortcuts/Health/Health Data/Data" + DateTime.Now.Year.ToString() + ".txt";
        if (!IsPostBack)
        {
            uiDataFile.Text = filePath;
            uiDdlYear.Text = DateTime.Now.Year.ToString();
        }
    }
    protected void GetSearchResults(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(uiSearchText.Text))
            {
                return; // have no search text
            }

            if (!string.IsNullOrEmpty(uiDataFile.Text))
            {
                filePath = uiDataFile.Text;
            }

            showConsecutiveDates = uiChkShowDates.Checked;

            // Read all lines from the file
            string[] lines = File.ReadAllLines(filePath);

            uiLtlOutput.Text = string.Empty;

            foreach (string line in lines)
            {
                if (line.StartsWith("Date"))
                {
                    dateFound = true;
                    strCurrentDate = line.Replace("�", string.Empty);
                    dtDate = GetDateObject(strCurrentDate);
                }
                else
                {
                    if (line.ToLower().Contains(uiSearchText.Text.ToLower()))
                    {
                        uiLtlOutput.Text += "<tr>";
                        if (dateFound)
                        {
                            string style = "";
                            if (showConsecutiveDates)
                            {
                                if (dtDate.ToLongDateString() == dtLastDate.AddDays(1).ToLongDateString())
                                {
                                    style = "color:blue;";
                                }
                            }
                            uiLtlOutput.Text += "<td style='vertical-align:top;" + style + "'>" + GetDateFrom(dtDate) + "</td>";  //remove the 'Date: ' string
                            dateFound = false;
                        } else
                        {
                            uiLtlOutput.Text += "<td></td>";
                        }
                        
                        searchHit = line.Replace("�", "'");  // file.readalllines changes quotes to '?' so change it back

                        // highlight the search term
                        searchHit = searchHit.Replace(uiSearchText.Text, "<b>" + uiSearchText.Text + "</b>");
                        
                        // if we only want to see the value of the specific search term, not the whole search text
                        if (uiChkShowSpecificData.Checked)
                        {
                            uiLtlOutput.Text += "<td>" + GetSpecificData(searchHit, uiSearchText.Text) + " </td>";
                        } else
                        {
                            uiLtlOutput.Text += "<td>" + searchHit + " </td>";
                        }

                        uiLtlOutput.Text += "</tr>";

                        dtLastDate = dtDate;
                    }

                }
            }
        }
        catch (Exception ex)
        {
            Response.Write($"An error occurred while reading the file: {ex.Message}");
        }
    }

    /// <summary>
    /// gets a specific search term from the input line
    /// e.g. return 'HRV:40' from 'HRV:40, total power:297 , lf/hf:7.74'
    /// </summary>
    /// <param name="searchHit">is the full length search result</param>
    /// <param name="searchTerm"> is the term being searched for </param>
    /// <returns></returns>
    protected string GetSpecificData(string searchHit, string searchTerm)
    {
        string rc = "";
        string lowerSearchHit = searchHit.ToLower();
        string lowerSearchTerm = searchTerm.ToLower();

        String[] pairs = searchHit.Split((new char[] { ',' }));
        foreach (string pair in pairs)
        {
            if (pair.ToLower().Contains(lowerSearchTerm))
            {
                rc = pair;
            }
        }
       
        return rc;
    }

    /// <summary>
    /// remove 'Date: ' from the input line
    /// </summary>
    /// <param name="dateLine"></param>
    /// <returns></returns>
    protected string GetDateFrom(string dateLine)
    {
        int line = dateLine.IndexOf("Date:");
        return dateLine.Substring(line + 5).TrimStart(' ');
    }
    /// <summary>
    /// returns a DateTime object in a specific format according to checkbox parameter:
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    protected string GetDateFrom(DateTime dateTime)
    {
        string rc = string.Empty;
        if (uiChkChangeDateFormat.Checked)
        {
            rc = dtDate.ToString("dd/MM/yyyy");
        } else
        {
            rc = dtDate.ToString("dddd ") + dtDate.ToString("dd").TrimStart('0') + GetDaySuffix(int.Parse(dtDate.ToString("dd"))) + dtDate.ToString(" MMMM");
        }
        return rc;
    }

    string GetDaySuffix(int day)
    {
        switch (day)
        {
            case 1:
            case 21:
            case 31:
                return "st";
            case 2:
            case 22:
                return "nd";
            case 3:
            case 23:
                return "rd";
            default:
                return "th";
        }
    }

    protected DateTime GetDateObject(string strCurrentDate)
    {                
        DateTime dtObject;
        string replacedStr = GetDateFrom(strCurrentDate)
                                 .Replace("nd ", " ")
                                 .Replace("th ", " ")
                                 .Replace("rd ", " ")
                                 .Replace("st ", " ");

        //DateTime.TryParseExact("Monday 28 Feb 2023", "dddd dd MMMM yyyy", CultureInfo.CurrentCulture, DateTimeStyles.None, out dtObject)

        // need to add leading '0' to single digit days
        replacedStr = replacedStr.Replace(" 1 ", " 01 ");
        replacedStr = replacedStr.Replace(" 2 ", " 02 ");
        replacedStr = replacedStr.Replace(" 3 ", " 03 ");
        replacedStr = replacedStr.Replace(" 4 ", " 04 ");
        replacedStr = replacedStr.Replace(" 5 ", " 05 ");
        replacedStr = replacedStr.Replace(" 6 ", " 06 ");
        replacedStr = replacedStr.Replace(" 7 ", " 07 ");
        replacedStr = replacedStr.Replace(" 8 ", " 08 ");
        replacedStr = replacedStr.Replace(" 9 ", " 09 ");
        replacedStr = replacedStr.Trim() + " " + uiDdlYear.Text;
        if (DateTime.TryParseExact(replacedStr,
                                    "dddd dd MMMM yyyy",
                                    CultureInfo.InstalledUICulture,
                                    DateTimeStyles.None,
                                    out dtObject))
        {
            //valid date            
            return dtObject;
        } else
        {
            return DateTime.MinValue;
        }
    }
}