using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.CloudConnect.CouchBaseProvider
{
    public enum FieldType
    {
        String,
        Integer,
        Boolean,
        Speed,
        Int100,
        Int1000,
        MilliSecond,
        Second,
        DateDbehav,
        TimeDbehav,
        Sensor4hz
    }

    public class FieldDefinition : ModelBase
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("field_type")]
        public FieldType FieldType { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("ingore_in_history")]
        public bool IgnoreInHistory { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        [JsonProperty("field_dependency")]
        public string FieldDependency { get; set; }

        [JsonProperty("sort_id")]
        public int SortId { get; set; }
        

        public override string Type
        {
            get { return "field_definition"; }
        }
    }
}
