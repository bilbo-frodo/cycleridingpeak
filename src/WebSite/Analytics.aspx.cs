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
    protected string currentDate;

    protected void Page_Load(object sender, EventArgs e)
    {
        // Specify the path to the text file
        filePath = "c:/Temp/testdata.txt";
    }
    protected void GetSearchResults(object sender, EventArgs e)
    {
        try
        {
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
                    currentDate = line;
                }
                else
                {
                    if (line.ToLower().Contains(uiSearchText.Text.ToLower()))
                    {
                        uiLtlOutput.Text += "<b>" + currentDate + "</b>";
                        uiLtlOutput.Text += line + "</br>";
                    }

                }
            }
        }
        catch (Exception ex)
        {
            Response.Write($"An error occurred while reading the file: {ex.Message}");
        }
    }
}