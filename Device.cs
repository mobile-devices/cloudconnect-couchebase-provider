using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudConnect.CouchBaseProvider
{
    public class Device : ModelBase
    {
        [JsonProperty("imei")]
        public String Imei { get; set; }
        [JsonProperty("last_received_at")]
        public DateTime LastReceivedAt { get; set; }
        [JsonProperty("last_recorded_at")]
        public DateTime LastRecordedAt { get; set; }
        [JsonProperty("last_longitude")]
        public double LastLongitude { get; set; }
        [JsonProperty("last_latitude")]
        public double LastLatitude { get; set; }
        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [JsonProperty("fields")]
        public Dictionary<string, Field> Fields { get; set; }

        [JsonProperty("last_cloud_id")]
        public String LastCloudId { get; set; }
        [JsonProperty("last_connection_id")]
        public String LastConnectionId { get; set; }
        [JsonProperty("last_index")]
        public uint LastIndex { get; set; }
        [JsonProperty("next_waiting_index")]
        public uint NextWaitingIndex { get; set; }

        [JsonProperty("last_track_id")]
        public String LastTrackId { get; set; }


        public override string Type
        {
            get { return "device"; }
        }
    }
}
