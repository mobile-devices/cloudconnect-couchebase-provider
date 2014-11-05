using Couchbase;
using Couchbase.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CloudConnect.CouchBaseProvider
{
    public abstract class RepositoryBase<T> where T : ModelBase
    {
        protected Cluster _cluster;
        protected string _bucketName;

        protected RepositoryBase(Cluster cluster, string bucketName)
        {
            _cluster = cluster;
            _bucketName = bucketName;
            _designDoc = typeof(T).Name.ToLower().InflectTo().Pluralized;
        }

        protected virtual string BuildKey(T model)
        {
            if (string.IsNullOrEmpty(model.Id))
            {
                return Guid.NewGuid().ToString();
            }
            return model.Id.InflectTo().Underscored;
        }

        protected readonly string _designDoc;

        public virtual List<T> GetAll(int limit = 0)
        {
            return this.GetViewResult<T>("all");
        }

        public virtual List<T> GetViewResult<T>(string viewName, string startKey = null, string endKey = null, int limit = 0, bool allowStale = false)
        {
            List<T> finalResult = new List<T>();

            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                var query = bucket.CreateQuery(_designDoc, viewName, false);
                if (limit > 0) query = query.Limit(limit);
                if (!allowStale) query = query.Stale(Couchbase.Views.StaleState.False);
                if (!string.IsNullOrEmpty(startKey)) query = query.StartKey(startKey);
                if (!string.IsNullOrEmpty(endKey)) query = query.EndKey(endKey);

                var result = bucket.Query<dynamic>(query);
                foreach (var row in result.Rows)
                {
                    finalResult.Add(JsonConvert.DeserializeObject<T>(row["value"].ToString()));
                }

            }
            return finalResult;
        }

        public virtual bool Create(T value, PersistTo persist = PersistTo.Zero)
        {
            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                string key = BuildKey(value);
                value.Id = key;
                var result = bucket.Upsert<T>(key, value, ReplicateTo.Zero, persist);

                if (result.Exception != null) throw result.Exception;
                return result.Success;
            }
        }

        public virtual bool CreateWithExpireTime(T value, uint ttl, PersistTo persist = PersistTo.Zero)
        {
            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                string key = BuildKey(value);
                value.Id = key;
                var result = bucket.Upsert<T>(key, value, ttl, ReplicateTo.Zero, persist);
                if (result.Exception != null) throw result.Exception;
                return result.Success;
            }
        }

        public virtual bool Update(T value, PersistTo persist = PersistTo.Zero)
        {
            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                var result = bucket.Upsert<T>(value.Id, value, ReplicateTo.Zero, persist);

                if (result.Exception != null) throw result.Exception;
                return result.Success;
            }
        }

        public virtual bool Save(T value, PersistTo persist = PersistTo.Zero)
        {
            var key = string.IsNullOrEmpty(value.Id) ? BuildKey(value) : value.Id;
            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                var result = bucket.Replace<T>(key, value, ReplicateTo.Zero, persist);

                if (result.Exception != null) throw result.Exception;
                return result.Success;
            }
        }

        public virtual T Get(string key)
        {
            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                var result = bucket.GetDocument<T>(key);
                if (result.Success)
                {
                    return result.Value;
                }
                return null;
            }
        }

        public virtual bool Delete(string key, PersistTo persist = PersistTo.Zero)
        {
            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                var result = bucket.Remove(key, ReplicateTo.Zero, persist);
                return result.Success;
            }
        }
    }
}
