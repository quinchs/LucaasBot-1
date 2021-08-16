using LucaasBot.HTTP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Routes
{
    public class GetSessionDetails : RestModuleBase
    {
        [Route("/me", "GET")]
        public async Task<RestResult> GetSession()
        {
            var user = Context.GetWebUser();

            if (user == null)
                return RestResult.Unauthorized;

            return RestResult.OK.WithData(new SelfUserResult(user));
        }
    }
}
