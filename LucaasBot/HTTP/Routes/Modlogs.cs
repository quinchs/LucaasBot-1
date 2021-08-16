using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP.Routes.API
{
    public class Modlogs : RestModuleBase
    {
        [Route("/modlog", "DELETE")]
        public async Task<RestResult> deleteModlog()
        {
            var user = Context.GetWebUser();

            if (user == null)
                return RestResult.Unauthorized;

            var rt = Request.ExecuteRatelimit(user);

            rt.ApplyHeaders(Response);

            if (rt.IsRatelimited)
            {
                return RestResult.Ratelimited;
            }

            // Check the body
            if (!Request.HasEntityBody)
            {
                return RestResult.BadRequest;
            }

            string cont = "";
            using (var sr = new StreamReader(Request.InputStream))
            {
                cont = sr.ReadToEnd();
            }

            ModlogDeleteBody body;

            try
            {
                body = JsonConvert.DeserializeObject<ModlogDeleteBody>(cont);
            }
            catch
            {
                return RestResult.BadRequest;
            }

            if (body.Modlog == null || body.Uid == null)
            {
                return RestResult.BadRequest;
            }
           
            ulong uid = 0;

            if (!ulong.TryParse(body.Uid, out uid))
            {
                return RestResult.BadRequest;
            }

            // Check if there is even a modlog for the requested modlog id
            var modlogUser = MongoService.DiscordUserCollection.Find(x => x.UserId == uid).FirstOrDefault();

            if (modlogUser == null)
            {
                return RestResult.NotFound;
            }

            var oid = HexHelper.FromHex(body.Modlog);

            var log = modlogUser.UserModlogs.FirstOrDefault(x => x._id.ToByteArray().SequenceEqual(oid));

            if (log == null)
            {
                return RestResult.NotFound;
            }

            // Delete the log!
            modlogUser.DelModlog(log._id);

            var discordUser = await UserService.GetLucaasUser(modlogUser.UserId);
            var moderator = await UserService.GetLucaasUser(modlogUser.UserId);

            // Post an alert
            await Program.LucaasGuild.GetTextChannel(663060075740659715).SendMessageAsync(embed: new EmbedBuilder()
            {
                Title = "Modlog deleted",
                Description = $"Modlog {HexHelper.ToHex(log._id.ToByteArray())} was deleted by {user.Username} (<@{user.Id}>)",
                Fields = new List<EmbedFieldBuilder>()
                {
                    new EmbedFieldBuilder()
                    {
                        Name = "Log Details",
                        Value = $"User: {(discordUser?.ToString() ?? "Unknown username")} <@{log.UserID}>\n" +
                        $"Action: {log.Action}\n" +
                        $"Reason: {log.Reason}\n" +
                        $"Moderator: {(moderator?.ToString() ?? "Unknown username")}\n" +
                        $"Date: {log.DateCreated}",
                    }
                },
                Color = Color.Orange
            }.WithCurrentTimestamp().Build());
            
            _ = Task.Run(async () =>
            {
                // Send to browsers that a log has been deleted
                await WebsocketServer.PushEvent("modlog.removed", new
                {
                    userId = modlogUser.UserId,
                    infracId = HexHelper.ToHex(log._id.ToByteArray()),
                    action = log.Action,
                    moderatorId = log.ModID,
                    reason = log.Reason,
                });

            });

            return RestResult.OK;
        }
    }
}
