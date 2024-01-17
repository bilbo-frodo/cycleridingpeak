using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Analytics : System.Web.UI.Page
{
    protected string filePath;
    protected string strCurrentDate, nextLineDate=string.Empty;
    protected string searchHit=string.Empty;
    protected bool dateFound = false;

    protected void Page_Load(object sender, EventArgs e)
    {
        // Specify the path to the text file
        filePath = "C:/BillShortcuts/Health/Health Data/Data2023.txt";
        if (!IsPostBack)
        {
            uiDataFile.Text = filePath;
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

            // Read all lines from the file
            string[] lines = File.ReadAllLines(filePath);

            uiLtlOutput.Text = string.Empty;

            foreach (string line in lines)
            {
                if (line.StartsWith("Date"))
                {
                    strCurrentDate = line.Replace("�", string.Empty);
                    dateFound = true;
                }
                else
                {
                    if (line.ToLower().Contains(uiSearchText.Text.ToLower()))
                    {
                        uiLtlOutput.Text += "<tr>";
                        if (dateFound)
                        {
                            uiLtlOutput.Text += "<td style='vertical-align:top'>" + GetDateFrom(strCurrentDate) + "</td>";  //remove the 'Date: ' string
                            dateFound = false;
                        } else
                        {
                            uiLtlOutput.Text += "<td></td>";
                        }
                        
                        searchHit = line.Replace("�", "'");  // file.readalllines changes quotes to '?' so change it back

                        // highlight the search term
                        searchHit = searchHit.Replace(uiSearchText.Text, "<b>" + uiSearchText.Text + "</b>");

                        uiLtlOutput.Text += "<td>" + searchHit + " </td>";    
                        uiLtlOutput.Text += "</tr>";
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
    /// remove 'Date: ' from the input line
    /// </summary>
    /// <param name="dateLine"></param>
    /// <returns></returns>
    protected string GetDateFrom(string dateLine)
    {
        int line = dateLine.IndexOf("Date:");
        return dateLine.Substring(line + 5).TrimStart(' ');
    }
}