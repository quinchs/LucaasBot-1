using Discord.WebSocket;
using LucaasBot.DataModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.Handlers
{
    public class PingGraphHandler : DiscordHandler
    {
        private DiscordSocketClient Client;

        public override void Initialize(DiscordSocketClient client)
        {
            this.Client = client;

            client.InteractionCreated += Client_InteractionCreated;
        }

        private async Task Client_InteractionCreated(SocketInteraction arg)
        {
            if(arg is not SocketMessageComponent component)
            {
                return;
            }

            if(component.Data.CustomId == "refresh_ping")
            {
                await component.AcknowledgeAsync();
                _ = Task.Run(async () => await RefreshGraph(component.Message));
            }
        }

        public async Task<Discord.Embed> GenerateGraphEmbed()
        {
            HttpClient c = new HttpClient();
            var resp = await c.GetAsync("https://discord.statuspage.io/metrics-display/ztt4777v23lf/day.json");
            var cont = await resp.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<DiscordApiPing>(cont);
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var tm = epoch.AddSeconds(data.Metrics.First().Data.Last().Timestamp);

            var gfp = Generate(data);

            gfp.Save($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}Ping.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

            return new Discord.EmbedBuilder()
            {
                Title = "Discord Ping and Status",
                Color = Discord.Color.Green,
                Description = $"You can view Discord's status page [here](https://status.discord.com/)" +
                              $"```Gateway:     {Client.Latency}ms\n" +
                              $"Api Latest:  {data.Summary.Last}ms\n" +
                              $"Api Average: {data.Summary.Mean}ms```",
                Timestamp = tm,
                Footer = new Discord.EmbedFooterBuilder()
                {
                    Text = "Last Updated: "
                },
                Author = new Discord.EmbedAuthorBuilder()
                {
                    Name = "Powered by quin#3017",
                    IconUrl = "https://cdn.discordapp.com/avatars/259053800755691520/1b1a1c1a62756406ebb3319138e08d4b.webp?size=256"
                },
                ImageUrl = await HapsyService.GetImageLink($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}Data{Path.DirectorySeparatorChar}Ping.jpg")
            }.Build();
        }

        public async Task RefreshGraph(SocketUserMessage msg)
        {
            if (msg == null)
                return;

            var embed = msg.Embeds.First();

            if (embed.Title != "Discord Ping and Status")
                return;

            try
            {
                await msg.ModifyAsync(x => x.Embed = new Discord.EmbedBuilder()
                {
                    Title = "Discord Ping and Status",
                    Color = Discord.Color.Green,
                    Description = $"You can view Discord's status page [here](https://status.discord.com/)\n" +
                              $"```\nGateway:     Fetching...\n" +
                              $"Api Latest:  Fetching...\n" +
                              $"Api Average: Fetching...```",
                    Footer = new Discord.EmbedFooterBuilder()
                    {
                        Text = "Last Updated: Fetching..."
                    }
                }.Build());

                var em = await GenerateGraphEmbed();

                await msg.ModifyAsync(x => x.Embed = em);
            }
            catch (Exception x)
            {
                Logger.Write($"Failed to refresh graph: {x}", Severity.Core, Severity.Error);
            }
        }

        static System.Drawing.Image ChartImage = new Bitmap(950, 600);
        static Graphics ChartGraphics = Graphics.FromImage(ChartImage);
        static Pen BlurplePen = new Pen(new SolidBrush(Color.FromArgb(114, 137, 218)), 3);
        static Pen WhitePen = new Pen(new SolidBrush(Color.FromArgb(255, 255, 255)), 4);
        static Pen WhitePenSS = new Pen(new SolidBrush(Color.FromArgb(255, 255, 255)), 1);
        static Font Font = new Font("Bahnschrift", 9, FontStyle.Regular);
        static Font TitleFont = new Font("Bahnschrift", 18);
        public static System.Drawing.Image Generate(DiscordApiPing data)
        {
            ChartGraphics.Clear(Color.FromArgb(47, 49, 54));
            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var curTime = DateTime.UtcNow;
            var mets = data.Metrics.First().Data;
            int yMin = 0;
            int yMax = (int)mets.Max(x => x.Value);
            yMax += (int)(yMax * 0.1);
            float yOffset = ((float)ChartImage.Height - 120) / (yMax - yMin);
            float xOffset = ((float)ChartImage.Width - 120) / mets.Count;

            ChartGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            ChartGraphics.DrawLine(WhitePen, new PointF(ChartImage.Width - 80f, ChartImage.Height - 60f), new PointF(ChartImage.Width - 80f, 60f));
            ChartGraphics.DrawLine(WhitePen, new PointF(30f, ChartImage.Height - 60f), new PointF(ChartImage.Width - 80f, ChartImage.Height - 60f));

            ChartGraphics.DrawString("Time (Hours)", TitleFont, new SolidBrush(Color.White), ChartImage.Width / 2, ChartImage.Height - 30, new StringFormat() { Alignment = StringAlignment.Center });
            ChartGraphics.DrawString($"Discord Ping (Past 24 Hours) Generated on {DateTime.UtcNow.ToString("f")} UTC", TitleFont, new SolidBrush(Color.White), ChartImage.Width / 2, 20, new StringFormat() { Alignment = StringAlignment.Center });

            var chtr = yMax / 10;
            var chtr2 = yMax / 20;
            bool odd = true;
            for (float i = 1; i != yMax + 1; i++)
            {
                if (i % chtr2 == 0)
                {
                    if (!odd)
                    {
                        string n = i.ToString();
                        if (i > 1000)
                            n = ((double)i / 1000).ToString("0.#k") + " ";
                        ChartGraphics.DrawLine(WhitePenSS, new PointF(30f, (ChartImage.Height - 60) - i * yOffset), new PointF(ChartImage.Width - 75f, (ChartImage.Height - 60) - i * yOffset));
                        ChartGraphics.DrawString($"{n}ms", Font, new SolidBrush(Color.FromArgb(255, 255, 255)), new PointF(ChartImage.Width - 70f, ((ChartImage.Height - 60) - i * yOffset) - 6));

                    }
                    else
                        ChartGraphics.DrawLine(WhitePenSS, new PointF(30f, (ChartImage.Height - 60) - i * yOffset), new PointF(ChartImage.Width - 80f, (ChartImage.Height - 60) - i * yOffset));
                    odd = !odd;
                }
            }
            var hSpace = (ChartImage.Width - 120) / 24;
            for (float i = 1; i != hSpace; i++)
            {
                if (i % 4 == 0)
                {
                    ChartGraphics.DrawLine(WhitePenSS, new PointF((ChartImage.Width - 80) - i * hSpace, (ChartImage.Height - 60)), new PointF((ChartImage.Width - 80) - i * hSpace, 60f));
                    ChartGraphics.DrawString($"-{i}", Font, new SolidBrush(Color.FromArgb(255, 255, 255)), new PointF((ChartImage.Width - 80) - i * hSpace, (ChartImage.Height - 50)), new StringFormat() { Alignment = StringAlignment.Center });
                }
            }
            ChartGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            for (int i = 0; i != mets.Count - 2; i++)
            {
                var cur = mets[i];
                var nxt = mets[i + 1];
                ChartGraphics.DrawLine(BlurplePen, new PointF(xOffset * i + 40f, (ChartImage.Height - 60) - ((cur.Value) * yOffset)), new PointF(xOffset * (i + 1) + 40f, (ChartImage.Height - 60) - ((nxt.Value) * yOffset)));
            }

            return ChartImage;
        }
    }
}
