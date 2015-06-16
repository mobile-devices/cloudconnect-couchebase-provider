using Couchbase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MD.CloudConnect.CouchBaseProvider
{
    public class FieldDefinitionRepository : RepositoryBase<FieldDefinition>
    {
        // public FieldDefinitionRepository(CouchbaseClient cluster) : base(cluster) { }

        public bool BulkUpsert(List<FieldDefinition> data)
        {
            IDictionary<string, FieldDefinition> items = new Dictionary<string, FieldDefinition>();
            List<string> added = new List<string>();
            foreach (FieldDefinition d in data)
            {
                if (String.IsNullOrEmpty(d.Id))
                {
                    d.Id = this.BuildKey(d);
                }
                added.Add(d.Id);
                items.Add(d.Id, d);
            }
            BulkUpsert(items);
            if (added.Count > 0)
            {
                CouchbaseManager.Instance.DataListRepository.PushToList("field_definition", added);
            }
            return true;
        }

        public override string BuildKey(FieldDefinition model)
        {
            return model.Key;
        }

        public List<FieldDefinition> GetAllSortedFieldDefinition()
        {
            int limit = 1000;

            List<FieldDefinition> result = new List<FieldDefinition>();
            List<FieldDefinition> tmp = new List<FieldDefinition>();
            uint id_page = 0;
            List<DataList> pages = new List<DataList>();
            DataList page = null;
            do
            {
                page = CouchbaseManager.Instance.DataListRepository.GetPage("field_definition", id_page);
                if (page != null)
                {
                    tmp = CouchbaseManager.Instance.FieldDefinitionRepository.GetDocs(page.Keys);
                    if (tmp.Count > 0)
                    {
                        pages.Add(page);
                        result.AddRange(tmp);
                    }
                }
                id_page++;
            } while (result.Count < limit && page != null);

            if (result.Count > 0)
            {
                result = result.OrderBy(x => x.SortId).ToList();
            }

            return result;
        }
    }
}
