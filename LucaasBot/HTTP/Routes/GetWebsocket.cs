using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Routes
{
    public class GetWebsocket : RestModuleBase
    {
        [Route("/socket", "GET")]
        public async Task<RestResult> Socket()
        {
            if (!Request.IsWebSocketRequest)
            {
                return RestResult.BadRequest;
            }

            var webuser = Context.GetWebUser();

            if(webuser == null)
            {
                return RestResult.Unauthorized;
            }

            await AcceptWebsocketAsync();
            
            return RestResult.KeepOpen;
        }
    }
}
