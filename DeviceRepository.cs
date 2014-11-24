using Couchbase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MD.CloudConnect.CouchBaseProvider
{
    public class DeviceRepository : RepositoryBase<Device>
    {
  
        public void SaveDevice(Device d)
        {
            this.Create(d);
        }

        public override string BuildKey(Device model)
        {
            return model.Imei.InflectTo().Underscored;
        }

        public bool BulkUpsert(List<Device> data)
        {

            IDictionary<string, Device> items = new Dictionary<string, Device>();
            foreach (Device d in data)
            {
                if (String.IsNullOrEmpty(d.Id))
                    d.Id = this.BuildKey(d);
                items.Add(d.Id, d);
            }
            BulkUpsert(items);

            return true;
        }
    }
}
