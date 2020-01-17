using System;
using Strava.NET.Api;
using Strava.NET.Client;
using Strava.NET.Model;
using ClassLibrary1.Strava.NET.Api;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;

public partial class StravaHome : System.Web.UI.Page
{
    // get these from the strava api page : https://www.strava.com/settings/api
    private static string ClientId = "39829";
    private static string ClientSecret = "2ef0a765044364334978c15bffd3c44991f08ec8";

    private static string Message { get; set; } = "";
    private static string Scope { get; set; }
    private static string Code { get; set; }
    private static string Token { get; set; }
    private static string TokenType { get; set; }
    private static string RefreshToken { get; set; }
    private static string ExpiresIn { get; set; }
    private static string ExpiresAt { get; set; }
    private static int ErrorLoopCount { get; set; } = 0;

    private static ApiClient ApiClient = new ApiClient();
    private static ActivitiesApi ActivitiesApiInstance = new ActivitiesApi(ApiClient);
    private static Dictionary<string, DetailedActivity> DetailedActivitiesDict = new Dictionary<string, DetailedActivity>();

    const bool DEBUG = false;

    private static readonly string State = new Random().Next(0, 100000).ToString("000000"); // CryptoRandom.CreateUniqueId();

    /// <summary>
    /// getting an access token from the strava api is a two step process
    /// 1) GetCodeButton_Click() gets a code for the required scope e.g. activity:read which then redirects back to this page with 'code' in the querystring
    /// 2) Page_Load then requests an access token using the code
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void Page_Load(object sender, EventArgs e)
    {
        CheckStateIsUnchanged();

        // set default values for the textboxes
        if (!IsPostBack)
        {
            var month = DateTime.Today.Month != 1 ? DateTime.Today.Month - 1 : 12;  //if today is in Jan then last month is Dec
            var year = month != 12 ? DateTime.Today.Year : DateTime.Today.Year - 1;
            uiTxtStartDate.Text = string.Format("01/{0:00}/{1}", month, year);
            uiTxtEndDate.Text = DateTime.Today.ToString("dd/MM/yyyy");
        }

        // if 'code' is in the querystring then this has been returned by the strava authorize endpoint
        if (!string.IsNullOrWhiteSpace(Request.QueryString["code"]) && !IsPostBack)
        {
            Code = Request.QueryString["code"];
            if (Code == null)
            {
                Message += "\n\nNot ready! Authorize first.";
                return;
            }

            if (false) Response.Write($"code:{Code}<br />");

            // got a code so now request a token using the code
            GetTokenFromApi(Code);
        }

        // viewstate is turned off, so grab values from Session
        if (Session["Token"] != null)
        {
            Token = Session["Token"].ToString();
        }
        if (Session["TokenExpires"] != null)
        {
            uiLtlStatus.Text = Session["TokenExpires"].ToString();
        }
    }

        /// <summary>
        /// now done via StravaApiHandler.ashx
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void GetAthleteDetailsClick(object sender, EventArgs e)
    {
        try
        {
            ApiClient = new ApiClient();
            ApiClient.AccessToken = Token;

            var apiInstance = new AthletesApi(ApiClient);
            var result = apiInstance.GetLoggedInAthlete();

            //uiLtlAthleteName.Text = string.Empty;
            //uiLtlAthleteName.Text+=(string.Format("<p>Athlete name:{0} {1}</p>", result.Firstname, result.Lastname));

            ErrorLoopCount = 0;
        }
        catch (Exception ex)
        {
            Response.Write("Exception when calling AthletesApi.GetLoggedInAthlete: " + ex.Message);
        }
    }

    protected void GetActivityDetailsClick(object sender, EventArgs e)
    {
        try
        {
            uiLtlOutput.Text = string.Empty;

            ApiClient.AccessToken = Token;

            DateTime fromDate, endDate;

            if (DateTime.TryParseExact(uiTxtStartDate.Text, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fromDate))
            {
                if (DateTime.TryParseExact(uiTxtEndDate.Text, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
                {
                    endDate = endDate.AddDays(1);  //make sure we include today's activities 

                    int? epochFromDate = (int)((DateTimeOffset)fromDate).ToUnixTimeSeconds();
                    int? epochToDate = (int)((DateTimeOffset)endDate).ToUnixTimeSeconds();

                    var result = ActivitiesApiInstance.GetLoggedInAthleteActivities(epochToDate, epochFromDate, page: 1, perPage: 150);

                    CalculateActivitiesByMonth(result);
                    CalculateTopAverageSpeeds(result);
                    CalculateTopDistances(result);
                    CalculateTopAverageHR(result);
                    CalculateTop3MaximumHR(result);

                    RenderActivities(result, fromDate, endDate);
                }
            }  else
            {
                // need some error handling for invalid dates
                Response.Write("<span class='error'>Invalid date format - must be dd/MM/yyyy</span>");
            }

            ErrorLoopCount = 0;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Authorization Error") && (ErrorLoopCount < 1))
            {
                ErrorLoopCount += 1;

                // try to renew token
                GetCode();

                //reset the dictionary to pick up any new data
                DetailedActivitiesDict = new Dictionary<string, DetailedActivity>();
            }
            else
            {
                Response.Write("Exception when calling GetActivityDetailsClick: " + ex.Message);
            }
        }
    }

    /// <summary>
    /// activity details are obtained from the api and cached in a dictionary 
    /// the dictionary is reset each time the token is renewed
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    private DetailedActivity GetActivityDetails(SummaryActivity item)
    {                
        string id = item.Id.ToString();

        DetailedActivity activityDetails = null;
        if (DetailedActivitiesDict.ContainsKey(id))
        {
            activityDetails = DetailedActivitiesDict[id];
        }
        else
        { 
            activityDetails = ActivitiesApiInstance.GetActivityById(item.Id, true);
            DetailedActivitiesDict.Add(id, activityDetails);
        }

        return activityDetails;
    }

    /// <summary>
    /// gets a code for the given scope e.g. activity:read (also returns 'read' permissions too on the athlete)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected void GetCodeButton_Click(object sender, EventArgs e)
    {
        try
        {
            GetCode();

        }
        catch (Exception ex)
        {
            Response.Write("Exception when calling GetCode: " + ex.Message);
        }
    }

    private static void GetCode()
    {
        Scope = "activity:read";

        // read_all - Allows access to view private routes, private segments, and private events. This scope matches the old view_private scope, except that it no longer includes access to private activities
        // activity:read_all - NEW! Allows the same access as activity:read, plus access to read the athlete’s activities that are visible to “Only You.”
        Scope = "activity:read_all";

        string redirectUri = "http://stravanet.local/stravahome";  //after the api call strava redirects back to this page

        // first get a code
        var apiClient = new ApiClient();
        var authorizeApi = new AuthorizeApi(apiClient);
        //ClientId = "10961181"; //wendy
        authorizeApi.GetCode(ClientId, ClientSecret, Scope, redirectUri, State);
    }

    private void RenderActivities(System.Collections.Generic.List<SummaryActivity> result, DateTime fromDate, DateTime endDate)
    {
        float? totalDistance = 0.0F;
        float? totalTimeCycled = 0.0F;
        float? totalTime = 0.0F;
        float? elevationGain = 0.0F;
        int noRides = 0;
        float? speedConversion = 3.6F; //don't know what the units are for 'avge speed' returned by strava

        // render table header
        uiLtlOutput.Text += "<p>";
        uiLtlOutput.Text += (string.Format(@"<table class='table table-striped' style='width:1200px !important'>"));
        if (uiRbCycling.Checked)
        {
            uiLtlOutput.Text += (string.Format(@"<tr><th class='w-108'>date</th><th></th><th class='w-84'>distance<br />km (miles)</th><th class='w-64''>time<br />(hrs:mins)</th><th class='w-84'>elevation gain</th><th class='w-72'>calories</th><th class='w-84'>avge watts</th><th class='w-96'>avge speed<br />km (miles) / hr</th><th class='w-84'>rides this month</th><th class='w-84'>distance this month</th></tr>"));
        }
        else if (uiRbSpinning.Checked)
        {
            uiLtlOutput.Text += (string.Format(@"<tr><th>date</th><th class='w-64'>calories</th><th class='w-108'>time<br />(hrs:mins)</th><th class='w-108'>avge HR</th><th class='w-64'>max HR</th></tr>"));
        }
        else if (uiRbParkrun.Checked)
        {
            uiLtlOutput.Text += (string.Format(@"<tr><th class='w-72'>date</th><th class='w-108'>avge HR</th><th class='w-64'>max HR</th><th class='w-72'>time (mins)</th></tr>"));
        }

        // render details
        foreach (SummaryActivity activity in result)
        {
            if (activity.StartDate > fromDate && activity.StartDate < endDate.AddDays(1))
            {
                if ((uiRbCycling.Checked) && ActivityIsCycling(activity))  // if distance > 0 it's road cycling 
                {
                    noRides += 1;

                    totalDistance += activity.Distance;
                    totalTimeCycled += activity.MovingTime;
                    totalTime += activity.MovingTime;
                    elevationGain += activity.TotalElevationGain;
                    var avgeSpeedInKmPerHr = activity.AverageSpeed * speedConversion;
                    var avgeSpeedInMilesPerHr = avgeSpeedInKmPerHr * 0.6;
                    var highestSpeedBackgroundClass = activity.AverageSpeedPosition > 0 ? "highlighted" : "";
                    var longestDistanceClass = activity.LongestDistancePosition > 0 ? "highlighted" : string.Empty;
                    var time = TimeSpan.FromSeconds((double)activity.MovingTime);

                    var activityDetails = GetActivityDetails(activity);

                    // if there's a description plant a link to it
                    var description = activityDetails.Description != null && activityDetails.Description.Trim().Length > 0 ?
                        activity.Name + "&nbsp;<a href='#demo" + noRides + "' data-toggle='collapse'>+</a>" + "<div id='demo" + noRides + "' class='collapse'>" + activityDetails.Description + "</div>" :
                        activity.Name;
                    var calories = activityDetails.Calories;

                    uiLtlOutput.Text += (string.Format(@"<tr {0}><td>{1:ddd dd MMM}</td><td>{2}</td><td class='alignright {14:0}'>{12:0} ({3:0})</td><td class='alignright'>{4:hh\:mm}</td><td class='alignright'>{5:0} m</td><td class='alignright'>{15:0}</td><td class='alignright'>{6:0}</td><td class='alignright {9}'>{7:0.0} ({8:0.0})</td><td class='alignright'>{10:0}</td><td class='alignright'>{11:0} {13:0}</td></tr>",
                        string.Empty,
                        activity.StartDateLocal,
                        description,
                        activity.Distance / 1000 * 0.6213712,
                        time,
                        activity.TotalElevationGain,
                        activity.AverageWatts,
                        avgeSpeedInKmPerHr,
                        avgeSpeedInMilesPerHr,
                        highestSpeedBackgroundClass,
                        activity.ActivitiesThisMonth != 0 ? activity.ActivitiesThisMonth.ToString() : string.Empty,
                        activity.TotalDistanceThisMonth / 1000,
                        activity.Distance / 1000,
                        activity.ActivitiesThisMonth != 0 ? string.Format("({0:0})", activity.TotalDistanceThisMonth / 1000 * 0.6213712) : string.Empty,
                        longestDistanceClass,
                        calories
                        ));
                }
                else if ((uiRbCycling.Checked) && ActivityIsSpinning(activity)) // special case, for cycling, add in time spent spinning
                {
                    totalTimeCycled += activity.MovingTime;
                }
                else if ((uiRbSpinning.Checked) && ActivityIsSpinning(activity)) // if distance = 0 it's a spinning class
                {
                    noRides += 1;
                    var time = TimeSpan.FromSeconds((double)activity.MovingTime);
                    totalTimeCycled += activity.MovingTime;

                    var highestAvgeHeartRateClass = activity.AverageHrPosition > 0 ? "highlighted" : string.Empty;
                    var highestMaxHeartRateClass = activity.MaxHrPosition > 0 ? "highlighted" : string.Empty;

                    var activityDetails = GetActivityDetails(activity);
                    var description = activityDetails.Description != null && activityDetails.Description.Trim().Length > 0 ?
                        activityDetails.Description :
                        string.Empty;
                    var calories = activityDetails.Calories;

                    uiLtlOutput.Text += string.Format(@"<tr><td>{0:ddd dd MMM} {6}</td><td class='alignright'>{7}</td><td class='alignright'>{1:hh\:mm}</td><td class='alignright {4}'>{2:0}</td><td class='alignright {5}'>{3:0}</td></tr>",
                        activity.StartDateLocal,
                        time,
                        activity.AvgeHeartRate,
                        activity.MaxHeartRate,
                        highestAvgeHeartRateClass,
                        highestMaxHeartRateClass,
                        description,
                        calories
                        );
                }
                else if ((uiRbParkrun.Checked) && (activity.Type.ToLower() == "walk"))
                {
                    noRides += 1;

                    TimeSpan time = TimeSpan.FromSeconds((double)activity.MovingTime);

                    uiLtlOutput.Text += string.Format(@"<tr><td>{0:ddd dd MMM}</td><td class='alignright'>{1:0}</td><td class='alignright'>{2:0}</td><td class='alignright'>{3:mm\:ss}</td></tr>", activity.StartDateLocal, activity.AvgeHeartRate, activity.MaxHeartRate, time);
                }
            }
        }


        // render footers
        uiLtlOutput.Text += (string.Format(@"<tfoot>"));
        uiLtlOutput.Text += (string.Format(@"<tr><td></td><td></td><td class='alignright'>------</td><td class='alignright'>------</td></tr>"));
        uiLtlOutput.Text += (string.Format(@"<tr><td></td><td>No rides {0}</td><td class='alignright'>{1:0}</td><td class='alignright'>{2:0.0}</td></tr>", noRides, totalDistance / 1000 * 0.6213712, FormatTimeInUnixTimestampToHrsMins(totalTime)));
        uiLtlOutput.Text += (string.Format(@"</tfoot>"));

        uiLtlOutput.Text += (string.Format(@"</table>"));
        uiLtlOutput.Text += "</p>";

        var tsTimeCycled = TimeSpan.FromSeconds((double)totalTimeCycled);
        var hoursCycled = tsTimeCycled.Days * 24 + tsTimeCycled.Hours;

        if (uiRbCycling.Checked)
        {
            uiLtlSummary.Text = "<p>";
            uiLtlSummary.Text += (string.Format(@"Total distance on bike since {0:dd/MM/yyyy} is {1:0} km or {2:0} miles<br />", fromDate, totalDistance / 1000, totalDistance / 1000 * 0.6213712));
            uiLtlSummary.Text += (string.Format(@"Total time on bike (including spinning) was {0}:{1:00} hrs:mins<br />", hoursCycled, tsTimeCycled.Minutes));
            uiLtlSummary.Text += "</p>";
        }
    }

    private void CalculateTopAverageSpeeds(System.Collections.Generic.List<SummaryActivity> result)
    {
        try
        {
            var sortedByAverageSpeed = result.OrderByDescending(o => o.AverageSpeed).ToList();

            if (sortedByAverageSpeed.Count > 0)
            {
                // get the ride with the highest average speed and update the AverageSpeedPosition property
                var averageSpeedPosition = sortedByAverageSpeed[0].Id;
                result.Find(i => i.Id == averageSpeedPosition).AverageSpeedPosition = 1;

                if (sortedByAverageSpeed.Count > 1)
                {
                    // find the ride with the second highest average speed
                    averageSpeedPosition = sortedByAverageSpeed[1].Id;
                    result.Find(i => i.Id == averageSpeedPosition).AverageSpeedPosition = 2;
                }

                if (sortedByAverageSpeed.Count > 2)
                {
                    // and find the 3rd ride ...
                    averageSpeedPosition = sortedByAverageSpeed[2].Id;
                    result.Find(i => i.Id == averageSpeedPosition).AverageSpeedPosition = 3;
                }
            }
        }
        catch (Exception)
        {

            throw new ApplicationException("Unexpected error in CalculateTopAverageSpeeds");
        }
    }

    private void CalculateTopAverageHR(System.Collections.Generic.List<SummaryActivity> result)
    {
        try
        {
            // only interested in HR for spinning
            var sortedByAverageHr = result.Where(i => ActivityIsSpinning(i)).OrderByDescending(o => o.AvgeHeartRate).ToList();

            if (sortedByAverageHr.Count > 0)
            {
                // get the ride with the highest average HR and update the averageHrPosition property
                var averageHrPosition = sortedByAverageHr[0].Id;
                result.Find(i => i.Id == averageHrPosition).AverageHrPosition = 1;

                if (sortedByAverageHr.Count > 1)
                {
                    // find the ride with the second highest average HR
                    averageHrPosition = sortedByAverageHr[1].Id;
                    result.Find(i => i.Id == averageHrPosition).AverageHrPosition = 2;
                }

                if (sortedByAverageHr.Count > 2)
                {
                    // and find the 3rd ride ...
                    averageHrPosition = sortedByAverageHr[2].Id;
                    result.Find(i => i.Id == averageHrPosition).AverageHrPosition = 3;
                }
            }
        }
        catch (Exception)
        {
            throw new ApplicationException("Unexpected error in CalculateTopAverageHR");
        }
    }

    private void CalculateTop3MaximumHR(System.Collections.Generic.List<SummaryActivity> result)
    {
        try
        {
            // only interested in HR for spinning - sift out any HR's above 165 as rogue readings
            var sortedByMaxHr = result.Where(i => ActivityIsSpinning(i) && i.MaxHeartRate < 165).OrderByDescending(o => o.MaxHeartRate).ToList();

            if (sortedByMaxHr.Count > 0)
            {
                // get the ride with the highest average HR and update the averageHrPosition property
                var maxHrPosition = sortedByMaxHr[0].Id;
                result.Find(i => i.Id == maxHrPosition).MaxHrPosition = 1;

                if (sortedByMaxHr.Count > 1)
                {
                    // find the ride with the second highest average HR
                    maxHrPosition = sortedByMaxHr[1].Id;
                    result.Find(i => i.Id == maxHrPosition).MaxHrPosition = 2;
                }

                if (sortedByMaxHr.Count > 2)
                {
                    // and find the 3rd ride ...
                    maxHrPosition = sortedByMaxHr[2].Id;
                    result.Find(i => i.Id == maxHrPosition).MaxHrPosition = 3;
                }
            }
        }
        catch (Exception)
        {
            throw new ApplicationException("Unexpected error in CalculateTop3MaximumHR");
        }
    }

    private void CalculateTopDistances(System.Collections.Generic.List<SummaryActivity> result)
    {
        try
        {
            var sortedByDistance = result.OrderByDescending(o => o.Distance).ToList();

            if (sortedByDistance.Count > 0)
            {
                // get the ride with the highest average speed and update the AverageSpeedPosition property
                var distancePosition = sortedByDistance[0].Id;
                result.Find(i => i.Id == distancePosition).LongestDistancePosition = 1;

                if (sortedByDistance.Count > 1)
                {
                    // find the ride with the second highest average speed
                    distancePosition = sortedByDistance[1].Id;
                    result.Find(i => i.Id == distancePosition).LongestDistancePosition = 2;
                }

                if (sortedByDistance.Count > 2)
                {
                    // and find the 3rd ride ...
                    distancePosition = sortedByDistance[2].Id;
                    result.Find(i => i.Id == distancePosition).LongestDistancePosition = 3;
                }
            }
        }
        catch (Exception)
        {
            throw new ApplicationException("Unexpected error in CalculateTopDistances");
        }
    }

    private void CalculateActivitiesByMonth(System.Collections.Generic.List<SummaryActivity> result)
    {
        try
        {
            // calculate no of activities and distance this month
            int activitiesThisMonth = 0;
            float? totalDistanceThisMonth = 0F;

            var sortByDate = result.OrderBy(i => i.StartDateLocal).ToList();
            int thisMonth = sortByDate[0].StartDateLocal.Value.Month;
            long? previousActivityId = null;

            foreach (var sortedEntry in sortByDate)
            {
                // only interested in cycle rides for now
                if ((uiRbCycling.Checked) && (sortedEntry.Distance > 0) && (sortedEntry.Type.ToLower() == "ride"))  // if distance > 0 it's road cycling 
                {
                    if (sortedEntry.StartDateLocal.HasValue)
                    {
                        if (sortedEntry.StartDateLocal.Value.Month != thisMonth) //the month has changed so update the totals on the previous month
                        {
                            if (previousActivityId != null)
                            {
                                result.Find(i => i.Id == previousActivityId).ActivitiesThisMonth = activitiesThisMonth;
                                result.Find(i => i.Id == previousActivityId).TotalDistanceThisMonth = totalDistanceThisMonth;
                            }

                            thisMonth = sortedEntry.StartDateLocal.Value.Month;
                            activitiesThisMonth = 1;
                            totalDistanceThisMonth = sortedEntry.Distance;
                            previousActivityId = null;
                        }
                        else
                        {
                            activitiesThisMonth += 1;
                            totalDistanceThisMonth += sortedEntry.Distance;
                            previousActivityId = sortedEntry.Id; //when the month changes want to update running count on previous activity
                        }
                    }
                }
            }

            //update counts for final activity
            if (previousActivityId != null)
            {
                result.Find(i => i.Id == previousActivityId).ActivitiesThisMonth = activitiesThisMonth;
                result.Find(i => i.Id == previousActivityId).TotalDistanceThisMonth = totalDistanceThisMonth;
            }
        }
        catch (Exception)
        {
            throw new ApplicationException("Unexpected error in CalculateActivitiesByMonth");
        }
    }

    /// <summary>
    /// converts a code to an access token
    /// </summary>
    private void GetTokenFromApi(string code)
    {
        try
        {
            var apiClient = new ApiClient();
            var authorizeApi = new AuthorizeApi(apiClient);
            var token = authorizeApi.GetToken(ClientId, ClientSecret, code);

            if (DEBUG)
            {
                Response.Write($"access token:{token.AccessToken} for scope:{Scope}<br />");
                Response.Write($"refresh token:{token.RefreshToken}<br />");
            }

            Token = token.AccessToken;
            TokenType = token.TokenType;
            RefreshToken = token.RefreshToken;
            ExpiresAt = token.ExpiresAt;
            ExpiresIn = token.ExpiresIn;            

            var seconds = int.Parse(ExpiresIn);
            var expiresIn = string.Format("{0:00} hrs {1:00} mins {2:00} secs", seconds / 3600, (seconds / 60) % 60, seconds % 60);
            var expiresAt = ConvertFromUnixTimestamp(double.Parse(ExpiresAt)).ToString("hh:mm:ss tt on dd/MM/yyyy");
            if (DEBUG) Response.Write($"expires in {expiresIn}<br />");
            if (DEBUG) Response.Write($"token expires at {expiresAt}<br /><br />");
            uiLtlStatus.Text = $"token expires at {expiresAt}";

            Session["Token"] = Token;
            Session["TokenExpires"] = uiLtlStatus.Text;
        }
        catch (Exception ex)
        {
            Response.Write("Exception when calling GetToken: " + ex.Message);
        }
    }

    /// <summary>
    /// the 'state' passed back from strava should be the same as the 'state' in the request
    /// </summary>
    private void CheckStateIsUnchanged()
    {
        if (!string.IsNullOrWhiteSpace(Request.QueryString["state"]))
        {
            if (State != Request.QueryString["state"].ToString())
            {
                Message += "\n\nState not recognised. Cannot trust response.";
                return;
            }
        }
        return;
    }

    public static DateTime ConvertFromUnixTimestamp(double timestamp)
    {
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return origin.AddSeconds(timestamp);
    }

    private string GetDaySuffix(int day)
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

    private bool ActivityIsCycling(SummaryActivity activity)
    {
        return (activity.Distance > 0) && (activity.Type.ToLower() == "ride");
    }

    private bool ActivityIsSpinning(SummaryActivity activity)
    {
        return (activity.Distance == 0);
    }

    private string FormatTimeInUnixTimestampToHrsMins(float? totalTimeCycled)
    {
        var ts = TimeSpan.FromSeconds((double)totalTimeCycled);
        var hours = ts.Days * 24 + ts.Hours;

        var rc = string.Format("{0}:{1:00}", hours, ts.Minutes);
        return rc;
    }
}