using System;
using Strava.NET.Api;
using Strava.NET.Client;
using Strava.NET.Model;
using ClassLibrary1.Strava.NET.Api;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Web.UI.WebControls;

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

    private int gNoRides = 0;
    float? gTotalDistance = 0.0F;
    float? gTotalTimeCycled = 0.0F;
    float? gTotalTime = 0.0F;

    // Gear Id's as per Strava
    protected const string Roadie2 = "b7095592";
    protected const string Roadie = "b6051881";
    protected const string GoodOlBoy = "b6051885";
    private const string Wattbike = "b8127428";

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

        // if !postback then either user just loaded the homepage or else the redirect from the oAuth authentication server
        if (!IsPostBack)
        {
            var month = DateTime.Today.Month != 1 ? DateTime.Today.Month - 1 : 12;  //if today is in Jan then last month is Dec
            var year = month != 12 ? DateTime.Today.Year : DateTime.Today.Year - 1;

            if (UserLoadedHomepage())
            {
                // set the default values
                uiTxtStartDate.Text = string.Format("01/{0:00}/{1}", month, year);
                uiTxtEndDate.Text = DateTime.Today.ToString("dd/MM/yyyy");
            }
            else  // we're returning from oAuth so check use any start/end date values in session
            {
                uiTxtStartDate.Text=Session["StartDate"] != null? Session["StartDate"].ToString(): uiTxtStartDate.Text = string.Format("01/{0:00}/{1}", month, year);
                uiTxtEndDate.Text = Session["EndDate"] != null? Session["EndDate"].ToString(): DateTime.Today.ToString("dd/MM/yyyy");
            }
        }
        else  // if it's a postback save the current values for start/end date - we could lose them if we need to oAuth authentice
        { 
            Session["StartDate"] = uiTxtStartDate.Text;
            Session["EndDate"] = uiTxtEndDate.Text;
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

                    UpdateWithActivityDetails(result);  // e.g. get calories 

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
    /// get some values from the DetailedActivity and store them in SummaryActivity for binding to the repeaters
    /// </summary>
    /// <param name="result"></param>
    private void UpdateWithActivityDetails(List<SummaryActivity> result)
    {
        foreach (SummaryActivity activity in result)
        {
            var activityDetails = GetActivityDetails(activity);
            activity.Calories = activityDetails.Calories;
            activity.Description = activityDetails.Description;
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

        //float? totalDistance = 0.0F;
        //float? totalTimeCycled = 0.0F;
        float? totalTime = 0.0F;
        float? elevationGain = 0.0F;
        int noRides = 0;
        float? speedConversion = 3.6F; //don't know what the units are for 'avge speed' returned by strava

        if (uiRbCycling.Checked)
        {
            bool activitiesForRoadie = uiDdlGearId.SelectedValue.ToLower()=="roadie";       // roadie is my first road bike, the one which was stolen
            bool activitiesForRoadie2 = uiDdlGearId.SelectedValue.ToLower() == "roadie2";   // roadie2 is the 2020 Giant Defy bought from PedalOn
            bool activitiesForGoodOlBoy = uiDdlGearId.SelectedValue.ToLower() == "mtb";     // mtb is my old mountain bike aka goodoldboy
            if (activitiesForRoadie2)
            {
                uiRptCycling.DataSource = result.Where(i => ActivityIsCycling(i) && GearIsRoadie2(i));
            }
            else if (activitiesForRoadie)
            {
                uiRptCycling.DataSource = result.Where(i => ActivityIsCycling(i) && GearIsRoadie(i));
            }
            else if (activitiesForGoodOlBoy)
            {
                uiRptCycling.DataSource = result.Where(i => ActivityIsCycling(i) && GearIsGoodOlBoy(i));
            }
            else
            {
                uiRptCycling.DataSource = result.Where(i => ActivityIsCycling(i));
            }

            uiRptCycling.DataBind();
            uiRptCycling.Visible = true;
        }
        else if (uiRbSpinning.Checked)
        {
            //uiLtlOutput.Text += (string.Format(@"<tr><th>date</th><th class='w-64'>calories</th><th class='w-108'>time<br />(hrs:mins)</th><th class='w-108'>avge HR</th><th class='w-64'>max HR</th><th class='w-84'>rides this month</th></tr>"));
            uiRptSpinning.DataSource = result.Where(i => ActivityIsSpinning(i));
            uiRptSpinning.DataBind();
            uiRptSpinning.Visible = true;
        }
        else if (uiRbParkrun.Checked)
        {
            uiLtlOutput.Text += (string.Format(@"<tr><th class='w-72'>date</th><th class='w-108'>avge HR</th><th class='w-64'>max HR</th><th class='w-72'>time (mins)</th></tr>"));
        }
        uiLtlOutput.Text += "</thead>";


        // render details
        foreach (SummaryActivity activity in result)
        {
            if (activity.StartDate > fromDate && activity.StartDate < endDate.AddDays(1))
            {
                if ((uiRbCycling.Checked) && ActivityIsSpinning(activity)) // special case, for cycling, add in time spent spinning
                {
                    gTotalTimeCycled += activity.MovingTime;
                }                
                else if ((uiRbParkrun.Checked) && (activity.Type.ToLower() == "walk"))  // not in use
                {
                    noRides += 1;

                    TimeSpan time = TimeSpan.FromSeconds((double)activity.MovingTime);

                    uiLtlOutput.Text += string.Format(@"<tr><td>{0:ddd dd MMM}</td><td class='alignright'>{1:0}</td><td class='alignright'>{2:0}</td><td class='alignright'>{3:mm\:ss}</td></tr>", activity.StartDateLocal, activity.AvgeHeartRate, activity.MaxHeartRate, time);
                }
            }
        }

        var tsTimeCycled = TimeSpan.FromSeconds((double)gTotalTimeCycled);
        var hoursCycled = tsTimeCycled.Days * 24 + tsTimeCycled.Hours;

        if (uiRbCycling.Checked)
        {
            uiLtlSummary.Text = "<p>";
            uiLtlSummary.Text += (string.Format(@"Total distance on bike since {0:dd/MM/yyyy} is {1:0} km or {2:0} miles<br />", fromDate, gTotalDistance / 1000, gTotalDistance / 1000 * 0.6213712));
            uiLtlSummary.Text += (string.Format(@"Total time spinning was {0}:{1:00} hrs:mins<br />", hoursCycled, tsTimeCycled.Minutes));
            uiLtlSummary.Text += "</p>";
        }
    }

    protected void CyclingRepeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        double kilometersToMilesConversionFactor = 0.6213712;
        try
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                gNoRides += 1;

                var activity = e.Item.DataItem as SummaryActivity;
                if (activity != null)
                {
                    gTotalDistance += activity.Distance;
                    gTotalTime += activity.MovingTime;

                    float? speedConversion = 3.6F; //don't know what the units are for 'avge speed' returned by strava
                    var avgeSpeedInKmPerHr = activity.AverageSpeed * speedConversion;
                    var avgeSpeedInMilesPerHr = avgeSpeedInKmPerHr * kilometersToMilesConversionFactor;
                    var highestSpeedBackgroundClass = activity.AverageSpeedPosition > 0 ? "highlighted" : "";

                    Literal uiLtlAvgeSpeed = (Literal)e.Item.FindControl("uiLtlAvgeSpeed");
                    if (uiLtlAvgeSpeed != null)
                    {
                        uiLtlAvgeSpeed.Text = string.Format("{0:0.0} ({1:0.0})", avgeSpeedInKmPerHr, avgeSpeedInMilesPerHr);
                    }

                    Literal uiLtlShowDescription = (Literal)e.Item.FindControl("uiLtlShowDescription");
                    if (uiLtlShowDescription != null)
                    {
                        /*
                         * <a href="#demo1" data-toggle="collapse">+</a>
                         * <div id="demo1" class="collapse">first ride with garmin</div>
                         */
                        // if there's a description plant a link to it
                        var linkToDescription = activity.Description != null && activity.Description.Trim().Length > 0 ?
                            "<a href='#demo" + gNoRides + "' data-toggle='collapse'>+</a>" + "<div id='demo" + gNoRides + "' class='collapse'>" + activity.Description + "</div>" : string.Empty;

                        uiLtlShowDescription.Text = linkToDescription;
                    }
                }
            }
            if (e.Item.ItemType == ListItemType.Footer)
            {
                Literal uiLtlNoRides = (Literal)e.Item.FindControl("uiLtlNoRides");
                if (uiLtlNoRides != null)
                {
                    uiLtlNoRides.Text = string.Format("{0:0}", gNoRides);
                }
                Literal uiLtlTotalDistance = (Literal)e.Item.FindControl("uiLtlTotalDistance");
                if (uiLtlTotalDistance != null)
                {
                    uiLtlTotalDistance.Text = string.Format("{0:0}", gTotalDistance / 1000 * 0.6213712);
                }
                Literal uiLtlTotalTime = (Literal)e.Item.FindControl("uiLtlTotalTime");
                if (uiLtlTotalTime != null)
                {
                    uiLtlTotalTime.Text = string.Format("{0:0}", FormatTimeInUnixTimestampToHrsMins(gTotalTime));
                }
            }
        }
        catch (Exception ex)
        {
            Response.Write("Exception when calling CyclingRepeater_ItemDataBound: " + ex.Message);
        }
    }

    protected void SpinningRepeater_ItemDataBound(object sender, RepeaterItemEventArgs e)
    {
        try
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                gNoRides += 1;
                var activity = e.Item.DataItem as SummaryActivity;
                if (activity != null)
                {
                    gTotalTime += activity.MovingTime;
                }
            }
            if (e.Item.ItemType == ListItemType.Footer)
            {
                Literal uiLtlNoRides = (Literal)e.Item.FindControl("uiLtlNoRides");
                if (uiLtlNoRides != null)
                {
                    uiLtlNoRides.Text = string.Format("{0:0}", gNoRides);
                }
                Literal uiLtlTotalTime = (Literal)e.Item.FindControl("uiLtlTotalTime");
                if (uiLtlTotalTime != null)
                {
                    uiLtlTotalTime.Text = string.Format("{0:0}", FormatTimeInUnixTimestampToHrsMins(gTotalTime));
                }
            }
        }
        catch (Exception ex)
        {
            Response.Write("Exception when calling SpinningRepeater_ItemDataBound: " + ex.Message);
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
        catch (Exception ex)
        {
            throw new ApplicationException("Unexpected error in CalculateTopAverageSpeeds" + ex.Message);
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
        catch (Exception ex)
        {
            throw new ApplicationException("Unexpected error in CalculateTopAverageHR" + ex.Message);
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
        catch (Exception ex)
        {
            throw new ApplicationException("Unexpected error in CalculateTop3MaximumHR" + ex.Message);
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
        catch (Exception ex)
        {
            throw new ApplicationException("Unexpected error in CalculateTopDistances" + ex.Message);
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
            if (sortByDate.Count <= 0)
            {
                return;
            }

            int thisMonth = sortByDate[0].StartDateLocal.Value.Month;
            long? previousActivityId = null;

            foreach (var sortedEntry in sortByDate)
            {
                // only interested in cycle rides and spinning for now
                if (((uiRbCycling.Checked) && (sortedEntry.Distance > 0) && (sortedEntry.Type.ToLower() == "ride") && (!ActivityIsWattbike(sortedEntry)))  // if distance > 0 it's road cycling 
                    ||
                    (uiRbSpinning.Checked) && (sortedEntry.Distance == 0))
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
        catch (Exception ex)
        {
            throw new ApplicationException("Unexpected error in CalculateActivitiesByMonth" + ex.Message);
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

    /// <summary>
    /// if it's not a postback then either the user loaded the homepage or we could be redirected by oAuth after an authentication
    /// </summary>
    /// <returns></returns>
    private bool UserLoadedHomepage()
    {
        return string.IsNullOrWhiteSpace(Request.QueryString["code"]) && !IsPostBack;
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
        return (activity.Distance > 0) && (activity.Type.ToLower() == "ride") && (!ActivityIsWattbike(activity));
    }

    private bool ActivityIsWattbike(SummaryActivity activity)
    {
        return (activity.GearId == Wattbike) || (activity.Name.ToLower().Contains("wattbike"));
    }

    private bool ActivityIsSpinning(SummaryActivity activity)
    {
        return ((activity.Distance == 0) || (ActivityIsWattbike(activity)));
    }

    private bool GearIsRoadie(SummaryActivity activity)
    {
        return (activity.GearId == Roadie);
    }

    private bool GearIsRoadie2(SummaryActivity activity)
    {
        return (activity.GearId == Roadie2);
    }

    private bool GearIsGoodOlBoy(SummaryActivity activity)
    {
        return (activity.GearId == GoodOlBoy);
    }

    private string FormatTimeInUnixTimestampToHrsMins(float? totalTimeCycled)
    {
        var ts = TimeSpan.FromSeconds((double)totalTimeCycled);
        var hours = ts.Days * 24 + ts.Hours;

        var rc = string.Format("{0}:{1:00}", hours, ts.Minutes);
        return rc;
    }
}
 