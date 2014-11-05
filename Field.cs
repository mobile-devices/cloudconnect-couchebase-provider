using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudConnect.CouchBaseProvider
{
    public class Field
    {
        public string Key { get; set; }
        public string B64Value { get; set; }

        [JsonIgnore]
        public int IntegerValue { get; set; }
        [JsonIgnore]
        public bool BooleanValue { get; set; }
        [JsonIgnore]
        public string StringValue { get; set; }
    }
}
