using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudConnect.CouchBaseProvider
{
    public class Track : ModelBase
    {
        [JsonProperty("imei")]
        public String Imei { get; set; }

        [JsonProperty("date_key")]
        public int DateKey { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }
        [JsonProperty("latitude")]
        public double Latitude { get; set; }


        [JsonProperty("recorded_at")]
        public DateTime Recorded_at { get; set; }

        [JsonProperty("received_at")]
        public DateTime Received_at { get; set; }

        [JsonProperty("created_at")]
        public DateTime Created_at { get; set; }

        [JsonProperty("fields")]
        public Dictionary<string, Field> Fields { get; set; }

        public override string Type
        {
            get { return "track"; }
        }
    }
}
