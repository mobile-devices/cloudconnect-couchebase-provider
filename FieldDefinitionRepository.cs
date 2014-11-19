using Couchbase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CloudConnect.CouchBaseProvider
{
    public class FieldDefinitionRepository : RepositoryBase<FieldDefinition>
    {
        public FieldDefinitionRepository(Cluster cluster, string bucketName) : base(cluster, bucketName) { }

        public bool BulkUpsert(List<FieldDefinition> data)
        {
            IDictionary<string, FieldDefinition> items = new Dictionary<string, FieldDefinition>();
            foreach (FieldDefinition d in data)
            {
                if (String.IsNullOrEmpty(d.Id))
                    d.Id = this.BuildKey(d);
                items.Add(d.Id, d);
            }
            BulkUpsert(items);
            return true;
        }

        public override string BuildKey(FieldDefinition model)
        {
            return model.Key;
        }

        public List<FieldDefinition> GetAllSortedFieldDefinition()
        {
            List<FieldDefinition> result = new List<FieldDefinition>();
            List<FieldDefinition> cache = new List<FieldDefinition>();
            int limit = 1000;
            string docid = "";
            do
            {
                cache = GetAll(limit, docid, true);
                if (!String.IsNullOrEmpty(docid))
                {
                    docid = cache.Last().Id;
                    cache.RemoveAt(cache.Count - 1);
                }
                else
                    docid = cache.First().Id;
                result.AddRange(cache);
            } while (cache.Count >= limit);

            if (result.Count > 0)
            {
                result = result.OrderBy(x => x.SortId).ToList();
            }
            return result;
        }
    }
}
