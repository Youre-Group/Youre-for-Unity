/*
 * Copyright (C) 2023 YOURE Games GmbH.
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 */

namespace Data
{
    public class YoureUser
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string AccessToken { get; set; }
       // public bool NewsletterAccepted{ get; set; }
       // public bool TermsAccepted{ get; set; }
    }
}