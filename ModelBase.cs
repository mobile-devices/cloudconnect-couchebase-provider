using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.CloudConnect.CouchBaseProvider
{
    public abstract class ModelBase
    {
        public virtual string Id { get; set; }
        public abstract string Type { get; }
    }
}
