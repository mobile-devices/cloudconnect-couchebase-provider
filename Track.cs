using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.CloudConnect.CouchBaseProvider
{
    public class Track : ModelBase
    {
        public const int NOT_REBUILD = 0;
        public const int OK_REBUILDED = 1;
        public const int DUPLICATE_REBUILDED = 2;
        public const int TIMEOUT_REBUILDED = 3;
        public const int DATA_FEED_REBUILDED = 4;

        [JsonProperty("imei")]
        public String Imei { get; set; }

        [JsonProperty("account")]
        public String Account { get; set; }

        [JsonProperty("date_key")]
        public int DateKey { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }
        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// 0 : not rebuild
        /// 1 : rebuild and ok
        /// 2 : rebuild (duplicate)
        /// 3 : rebuild (timeout)
        /// 4 : rebuild (feed)
        /// </summary>
        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("recorded_at")]
        public DateTime Recorded_at { get; set; }

        [JsonProperty("received_at")]
        public DateTime Received_at { get; set; }

        [JsonProperty("created_at")]
        public DateTime Created_at { get; set; }

        [JsonProperty("connection_id")]
        public string ConnectionId { get; set; }

        [JsonProperty("index")]
        public uint? Index { get; set; }

        [JsonProperty("internal_index")]
        public int InternalIndex { get; set; }

        [JsonProperty("cloud_id")]
        public String CloudId { get; set; }

        [JsonProperty("fields")]
        public Dictionary<string, Field> Fields { get; set; }

        [JsonIgnore]
        public string OrderingKey
        {
            get
            {
                return String.Format("{0}:{1:00000}", ConnectionId, Index);
            }
        }

        public override string Type
        {
            get { return "track"; }
        }
    }
}
