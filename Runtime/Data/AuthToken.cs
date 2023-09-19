/*
 * Copyright (C) 2023 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using Newtonsoft.Json;

namespace YourePlugin.Data
{
    [Serializable]
    public class AuthToken
    {
        [JsonProperty("id_token"), JsonRequired]
        public string IdToken { get; set; }
        
        [JsonProperty("access_token"), JsonRequired]
        public string AccessToken { get; set; }
        
        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        public AuthToken(string idToken, string accessToken, string refreshToken)
        {
            IdToken = idToken;
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }
    }
}