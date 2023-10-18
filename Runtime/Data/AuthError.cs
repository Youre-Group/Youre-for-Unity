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
    public class AuthError
    {
        [JsonProperty("error")]
        public string Error { get; set; }
        public int Code { get; set; }
        public AuthError(int code, string error)
        {
            Error = error;
            Code = code;
        }
    }
}