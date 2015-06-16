using MD.CloudConnect.CouchBaseProvider;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.CloudConnect.CouchBaseProvider
{
    public class DataList : ModelBase
    {
        [JsonProperty("page")]
        public uint Page { get; set; }

        [JsonProperty("data_type")]
        public string DataType { get; set; }

        [JsonProperty("keys")]
        public List<string> Keys { get; set; }

        [JsonProperty("first")]
        public uint First { get; set; }

        [JsonProperty("last")]
        public uint Last { get; set; }

        [JsonProperty("size")]
        public uint Size { get; set; }

        public override string Type
        {
            get { return "data_list"; }
        }
    }
}
