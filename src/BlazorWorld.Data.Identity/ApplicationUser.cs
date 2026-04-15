using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorWorld.Data.Identity
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        public string? FirstName { get; set; }
        [PersonalData]
        public string? LastName { get; set; }
        [PersonalData]
        public string? InvitiationCode { get; set; }
        [PersonalData]
        public int Reputation { get; set; }
        [PersonalData]
        public int Coins { get; set; }
        [PersonalData]
        public string? AvatarHash { get; set; }
    }
}
