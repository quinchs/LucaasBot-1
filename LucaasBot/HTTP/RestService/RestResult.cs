using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucaasBot.HTTP
{
    public class RestResult
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; } = null;

        /// <summary>
        ///     Includes the 200 status code with this response
        /// </summary>
        public static RestResult OK
            => new RestResult(200);

        /// <summary>
        ///     Includes the 401 status code with this response
        /// </summary>
        public static RestResult Unauthorized
            => new RestResult(401);

        /// <summary>
        ///     Includes the 403 status code with this response
        /// </summary>
        public static RestResult Forbidden
           => new RestResult(403);

        /// <summary>
        ///     Includes the 404 status code with this response
        /// </summary>
        public static RestResult NotFound
           => new RestResult(404);

        /// <summary>
        ///     Includes the 400 status code with this response
        /// </summary>
        public static RestResult BadRequest
          => new RestResult(400);

        /// <summary>
        ///     Includes the 500 status code with this response
        /// </summary>
        public static RestResult InternalServerError
          => new RestResult(500);

        /// <summary>
        ///     Includes the 429 status code with this response
        /// </summary>
        public static RestResult Ratelimited
            => new RestResult(429);

        public RestResult WithData(object data)
        {
            this.Data = data;
            return this;
        }

        public RestResult WithCode(int code)
        {
            this.Code = code;
            return this;
        }

        public RestResult() { }
        public RestResult(int code)
        {
            this.Code = code;
        }

        /// <summary>
        ///     Keeps the current request open
        /// </summary>
        public static RestResult KeepOpen
            => new RestResult(int.MaxValue);
    }
}
