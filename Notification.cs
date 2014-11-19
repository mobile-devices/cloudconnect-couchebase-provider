using MD.CloudConnect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudConnect.CouchBaseProvider
{
    public class Notification : ModelBase, INotificationData
    {
        [JsonProperty("created_at")]
        public DateTime Created_at { get; set; }
        [JsonProperty("received_at")]
        public long Received_at { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        public override string Type
        {
            get { return "notification"; }
        }
    }
}
