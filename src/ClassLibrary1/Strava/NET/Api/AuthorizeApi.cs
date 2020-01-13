using System;
using System.Collections.Generic;
using System.Web;
using RestSharp;
using Strava.NET.Client;
using Strava.NET.Model;

namespace ClassLibrary1.Strava.NET.Api
{
    public class AuthorizeApi
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizeApi"/> class.
        /// </summary>
        /// <param name="apiClient"> an instance of ApiClient (optional)</param>
        /// <returns></returns>
        public AuthorizeApi(ApiClient apiClient = null)
        {
            if (apiClient == null) // use the default one in Configuration
                this.ApiClient = Configuration.DefaultApiClient;
            else
                this.ApiClient = apiClient;
        }

        /// <summary>
        /// Sets the base path of the API client.
        /// </summary>
        /// <param name="basePath">The base path</param>
        /// <value>The base path</value>
        public void SetBasePath(String basePath)
        {
            this.ApiClient.BasePath = basePath;
        }

        /// <summary>
        /// Gets the base path of the API client.
        /// </summary>
        /// <param name="basePath">The base path</param>
        /// <value>The base path</value>
        public String GetBasePath(String basePath)
        {
            return this.ApiClient.BasePath;
        }

        /// <summary>
        /// Gets or sets the API client.
        /// </summary>
        /// <value>An instance of the ApiClient</value>
        public ApiClient ApiClient { get; set; }

        /// <summary>
        /// gets a code for a given scope
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="scope"></param>
        public void GetCode(string clientId, string clientSecret, string scope, string redirectUri, string state)
        {
            //string redirectUri = "http://stravanet.local/stravahome";

            // www.strava.com/oauth/authorize shows the strava login box (if not already logged in) and the strava authorization dialog
            // then redirects back to the page specified in redirectUrl
            var authUrl = $"https://www.strava.com/oauth/authorize?client_id={clientId}&scope={scope}&redirect_uri={redirectUri}&response_type=code&response_mode=query&state={state}";

            HttpContext.Current.Response.Redirect(authUrl, false);

        }

        /// <summary>
        /// converts a code to an access token
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public Token GetToken (string clientId, string clientSecret, string code)
        {
            string path = "https://www.strava.com/oauth/token";

            string grantType = "authorization_code";

            var formParams = new Dictionary<String, String>();
            String postBody = null;
            formParams.Add("client_id", ApiClient.ParameterToString(clientId)); // form parameter
            formParams.Add("client_secret", ApiClient.ParameterToString(clientSecret));
            formParams.Add("grant_type", ApiClient.ParameterToString(grantType));
            formParams.Add("code", ApiClient.ParameterToString(code));

            var request = new RestRequest(path, Method.POST);
            // add form parameter, if any
            foreach (var param in formParams)
                request.AddParameter(param.Key, param.Value, ParameterType.GetOrPost);

            var RestClient = new RestClient();
            IRestResponse response = RestClient.Execute(request);

            return (Token)ApiClient.Deserialize(response.Content, typeof(Token), response.Headers);


        }
    }
}
