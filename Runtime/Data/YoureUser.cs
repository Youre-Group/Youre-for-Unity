/*
 * Copyright (C) 2023 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

using System;
using Newtonsoft.Json;

namespace Data
{
    [Serializable]
    public class YoureUser
    {
        [JsonProperty("sub"),JsonRequired]
        public string Id { get; set; }
        [JsonProperty("email")]
        public string Email { get; set; }
        [JsonProperty("username")]
        public string UserName { get; set; }
        [JsonProperty("email_verified")]
        public string EmailVerified { get; set; }
    }
}