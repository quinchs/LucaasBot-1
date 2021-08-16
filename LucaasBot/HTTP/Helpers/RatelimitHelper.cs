using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP
{
    public static class RatelimitHelper
    {
        private static List<Ratelimit> RateLimits = new();

        private static object lockObj = new();

        public static RatelimitResult ExecuteRatelimit(this HttpListenerRequest request, WebUser user)
        {
            var ratelimitInfo = ConfigService.Config.Ratelimits.FirstOrDefault(x => x.Page == request.RawUrl);

            if (ratelimitInfo == null)
                return null;

            lock (lockObj)
            {
                var rt = RateLimits.FirstOrDefault(x => x.Route == request.RawUrl && x.Session == user.SessionToken);

                if(rt == null)
                {
                    rt = new Ratelimit()
                    {
                        BucketId = HexHelper.ToHex(Guid.NewGuid().ToByteArray()),
                        Count = 1,
                        StartedAt = DateTime.UtcNow,
                        ResetAfter = ratelimitInfo.Duration,
                        Limit = ratelimitInfo.Limit,
                        Route = request.RawUrl,
                        Session = user.SessionToken
                    };

                    RateLimits.Add(rt);

                    return new RatelimitResult(rt);
                }
                else if(rt.IsExpired())
                {
                    RateLimits.Remove(rt);

                    rt = new Ratelimit()
                    {
                        BucketId = HexHelper.ToHex(Guid.NewGuid().ToByteArray()),
                        Count = 1,
                        StartedAt = DateTime.UtcNow,
                        ResetAfter = ratelimitInfo.Duration,
                        Limit = ratelimitInfo.Limit,
                        Route = request.RawUrl,
                        Session = user.SessionToken
                    };

                    RateLimits.Add(rt);

                    return new RatelimitResult(rt);
                }
                else
                {
                    var indx = RateLimits.IndexOf(rt);

                    rt.Count++;
                    rt.ResetAfter = (DateTime.UtcNow - rt.StartedAt).TotalSeconds;

                    RateLimits[indx] = rt;

                    return new RatelimitResult(rt);
                }
            }
        }
    
        internal class Ratelimit
        {
            public string BucketId { get; set; }
            public string Session { get; set; }
            public string Route { get; set; }
            public int Count { get; set; }
            public int Limit { get; set; }
            public DateTime StartedAt { get; set; }
            public double ResetAfter { get; set; }

            public bool IsExpired()
            {
                return DateTime.UtcNow >= StartedAt.AddSeconds(ResetAfter);
            }
        }
    }

    public class RatelimitResult
    {
        public readonly bool IsRatelimited;

        public readonly string BucketId;
        public readonly int Limit;
        public readonly int Remaining;
        public readonly DateTime ResetAt;
        public readonly double ResetAfter;

        internal RatelimitResult(RatelimitHelper.Ratelimit r) 
        {
            this.BucketId = r.BucketId;
            this.IsRatelimited = r.Count > r.Limit;
            this.Limit = r.Limit;
            this.Remaining = r.Limit - r.Count;
            this.ResetAt = r.StartedAt.AddSeconds(r.ResetAfter);
            this.ResetAfter = r.ResetAfter;
        }

        public void ApplyHeaders(HttpListenerResponse r)
        {
            r.AddHeader("x-ratelimit-bucket", this.BucketId);
            r.AddHeader("x-ratelimit-reset", $"{ResetAt:R}");

            if (IsRatelimited)
            {
                r.AddHeader("x-ratelimit-retry-after", $"{ResetAfter}");
            }
            else
            {
                r.AddHeader("x-ratelimit-limit", $"{Limit}");
                r.AddHeader("x-ratelimit-remaining", $"{Remaining}");
                r.AddHeader("x-ratelimit-reset-after", $"{ResetAfter}");
            }
        }
    }
}
