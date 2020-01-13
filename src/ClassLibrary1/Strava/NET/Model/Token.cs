using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Strava.NET.Model
{

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class Token
    {
        /// <summary>
        /// The token type e.g. Bearer
        /// </summary>
        /// <value>The token type</value>
        [DataMember(Name = "token_type", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }

        /// <summary>
        /// The Access Token
        /// </summary>
        /// <value>The Access Token</value>
        [DataMember(Name = "access_token", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }


        /// <summary>
        /// The Refresh Token
        /// </summary>
        /// <value>The Access Token</value>
        [DataMember(Name = "refresh_token", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "refresh_token")]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets or Sets Athlete
        /// </summary>
        [DataMember(Name = "athlete", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "athlete")]
        public MetaAthlete Athlete { get; set; }

        /// <summary>
        /// Gets or Sets ExpiresAt
        /// </summary>
        [DataMember(Name = "expires_at", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "expires_at")]
        public string ExpiresAt { get; set; }

        /// <summary>
        /// Gets or Sets ExpiresIn
        /// </summary>
        [DataMember(Name = "expires_in", EmitDefaultValue = false)]
        [JsonProperty(PropertyName = "expires_in")]
        public string ExpiresIn { get; set; }


    }
}
