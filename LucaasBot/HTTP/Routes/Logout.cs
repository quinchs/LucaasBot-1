using LucaasBot.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Routes
{
    /// <summary>
    ///     This class will manage the logout function for users
    /// </summary>
    public class Logout : RestModuleBase
    {
        [Route("/logout", "GET")]
        public async Task<RestResult> logout()
        {
            // Check if we're even logged in
            if (!Request.Cookies.Any(x => x.Name == "csSessionID"))
            {
                return RestResult.BadRequest;
            }

            var user = Context.GetWebUser();

            if (user == null)
            {
                // they have a session token but we dont have it stored.
                Response.Headers.Add("Set-Cookie", "csSessionID=deleted; expires=Thu, 01 Jan 1970 00:00:00 GMT");
                return RestResult.Unauthorized;
            }

            // Log them out!
            DiscordAuthKeeper.LogoutUser(user.SessionToken);

            Response.Headers.Add("Set-Cookie", "csSessionID=deleted; expires=Thu, 01 Jan 1970 00:00:00 GMT");
            return RestResult.OK;
        }
    }
}
