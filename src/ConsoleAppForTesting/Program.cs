using System;
using System.Diagnostics;
using Strava.NET.Api;
using Strava.NET.Client;
using Strava.NET.Model;

namespace ConsoleAppForTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            // Configure OAuth2 access token for authorization: strava_oauth
            //Configuration.Default.AccessToken = "31a808cc0c54980db382e99ad2cc5bcfec97ebed";



            /// ATHLETE DETAILS - WORKING - just need to update access_token
            try
            {
                var apiClient = new ApiClient();
                apiClient.AccessToken = "1a8c919b477c0c7b03e218a41267636f80b6691e";

                var apiInstance = new AthletesApi(apiClient);
                var result = apiInstance.GetLoggedInAthlete();

                Debug.WriteLine(result);
            }
            catch (Exception ex)
            {
                Debug.Print("Exception when calling AthletesApi.GetLoggedInAthlete: " + ex.Message);
            }


            /// Activity DETAILS - update access_token with the current activity:read token 
            try
            {
                var apiClientActivity = new ApiClient();
                apiClientActivity.AccessToken = "17417ac5199f23d2904341aa48c234ed5e932812";  //activity:read token

                var apiInstance = new ActivitiesApi(apiClientActivity);
                var result = apiInstance.GetLoggedInAthleteActivities(null, null, page:1, perPage:30);

                float? distance = 0.0F;
                float? time = 0.0F;
                DateTime fromDate = DateTime.Parse("01/09/2019");

                foreach (SummaryActivity activity in result)
                {
                    if ((activity.Type.ToLower() == "ride") && (activity.Name.ToLower()!="spinning"))
                    {
                        if (activity.StartDate > fromDate)
                        {
                            distance += activity.Distance;
                            time += activity.MovingTime;
                        }
                    }
                }
                Console.WriteLine(string.Format(@"Total distance on bike since {0:dd/MM/yyyy} is {1:0} km", fromDate, distance/1000));
                Console.WriteLine(string.Format(@"Total time on bike was {0:0.00} hrs", time / 3600));
            }
            catch (Exception ex)
            {
                Debug.Print("Exception when calling ActivitiesApi.GetLoggedInAthleteActivities: " + ex.Message);
            }


        }
    }
}
