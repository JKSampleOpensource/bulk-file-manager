using System;
using System.Collections.Generic;
using System.Text;

namespace Ac.Net.Authentication.Models
{
    public interface IAuthParamProvider
    {
        string ClientId { get; set; }
        string ClientSecret { get; set; }
        string AuthCallback { get; set; }
        string RefreshToken { get; set; }
        string ForgeTwoLegScope { get; set; }
        string ForgeThreeLegScope { get; set; }

    }

    
}
