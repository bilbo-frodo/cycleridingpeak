<%@ WebHandler Language="C#" Class="StravaApiHandler" %>

using System;
using System.Web;
using System.Web.SessionState;
using Strava.NET.Api;
using Strava.NET.Client;
using Strava.NET.Model;
using Newtonsoft.Json;

public class StravaApiHandler : IHttpHandler, IRequiresSessionState {

    public void ProcessRequest(HttpContext context)
    {
        var detailedAthlete = GetAthleteDetails(context);

        context.Response.ContentType = "application/json";
        context.Response.Write(JsonConvert.SerializeObject(detailedAthlete));
        context.Response.StatusCode = 200;
    }

    public bool IsReusable {
        get {
            return false;
        }
    }

    private DetailedAthlete GetAthleteDetails(HttpContext context)
    {
        try
        {
            var apiClient = new ApiClient();
            apiClient.AccessToken = context.Session["Token"].ToString();

            var apiInstance = new AthletesApi(apiClient);
            var result = apiInstance.GetLoggedInAthlete();
            return result;
        }
        catch (Exception ex)
        {
            //Response.Write("Exception when calling AthletesApi.GetLoggedInAthlete: " + ex.Message);
            return new DetailedAthlete();
        }
    }
}
