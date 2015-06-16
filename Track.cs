using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.CloudConnect.CouchBaseProvider
{
    public class Track : ModelBase, IComparable<Track>, IEquatable<Track>
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
        /// 2 : rebuild (timeout)
        /// 3 : rebuild (duplicate)
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

        [JsonProperty("updated_at")]
        public DateTime Updated_at { get; set; }

        [JsonProperty("connection_id")]
        public string ConnectionId { get; set; }

        [JsonProperty("index")]
        public uint? Index { get; set; }

        [JsonProperty("next_wainting_index")]
        public uint? NextWaitingIndex { get; set; }

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

        public int CompareTo(Track other)
        {
            UInt64 cloud_id_a = Convert.ToUInt64(this.CloudId);
            UInt64 cloud_id_b = Convert.ToUInt64(other.CloudId);
            return cloud_id_a.CompareTo(cloud_id_b);
        }

        public bool Equals(Track other)
        {
            UInt64 cloud_id_a = Convert.ToUInt64(this.CloudId);
            UInt64 cloud_id_b = Convert.ToUInt64(other.CloudId);

            return cloud_id_a == cloud_id_b;
        }
    }
}
