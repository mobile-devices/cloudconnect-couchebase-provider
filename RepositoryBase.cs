using Couchbase;
using Couchbase.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
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

        public virtual string BuildKey(T model)
        {
            if (string.IsNullOrEmpty(model.Id))
            {
                return Guid.NewGuid().ToString();
            }
            return model.Id.InflectTo().Underscored;
        }

        protected readonly string _designDoc;


        public IDictionary<string, IOperationResult<T>> BulkUpsert(IDictionary<string, T> items, uint expiration = 0)
        {
            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                return bucket.Upsert(items, expiration);
            }
        }

        public virtual List<T> GetAll(int limit = 1000, string startKeyDocId = null, bool allowStale = false)
        {
            return this.GetViewResult<T>("all", limit: limit, startKeyDocId: startKeyDocId, allowStale: allowStale);
        }

        public virtual List<T> GetViewResult<T>(string viewName, string startKey = null, string endKey = null,
            int limit = 0, bool allowStale = false, bool inclusiveEnd = false, string startKeyDocId = null)
        {
            List<T> finalResult = new List<T>();
            try
            {
                using (var bucket = _cluster.OpenBucket(_bucketName))
                {
                    var query = bucket.CreateQuery(_designDoc, viewName, false).InclusiveEnd(inclusiveEnd);
                    if (limit > 0) query = query.Limit(limit);
                    if (!allowStale)
                        query = query.Stale(Couchbase.Views.StaleState.False);
                    else
                        query = query.Stale(Couchbase.Views.StaleState.UpdateAfter);
                    if (!string.IsNullOrEmpty(startKey)) query = query.StartKey(startKey);
                    if (!string.IsNullOrEmpty(endKey)) query = query.EndKey(endKey);
                    if (!string.IsNullOrEmpty(startKeyDocId)) query = query.StartKeyDocId(startKeyDocId);

                    IViewResult<dynamic> result = bucket.Query<dynamic>(query);
                    List<string> docIds = new List<string>();
                    foreach (var row in result.Rows)
                    {
                        //in case of map only value should be null
                        //value exist for reduce only
                        if (row.id == null)
                            finalResult.Add(row.Value<T>("value"));
                        else
                            docIds.Add(row.Value<string>("id"));
                    }
                    if (docIds.Count > 0)
                    {
                        IDictionary<string, IOperationResult<T>> docResult = null;
                        int retry = 0;
                        while (retry <= 3)
                        {
                            try
                            {
                                docResult = bucket.Get<T>((IList<string>)docIds);
                                break;
                            }
                            catch
                            {
                                retry++;
                            }
                        }
                        if (docResult.Count() > 0)
                        {
                            foreach (KeyValuePair<string, IOperationResult<T>> item in docResult)
                            {
                                if (!item.Value.Success || item.Value.Value == null)                                
                                {
                                    IOperationResult<T> temp = null;
                                    while (retry <= 3)
                                    {
                                        try
                                        {
                                            temp = bucket.Get<T>(item.Key);
                                            if (temp.Success)
                                                break;
                                        }
                                        catch
                                        {}
                                        retry++;

                                    }
                                    if (retry >= 3)
                                        return new List<T>();
                                    docResult[item.Key] = temp;
                                }
                            }
                            finalResult = (from d in docResult orderby d.Key select d.Value.Value).ToList<T>();
                        }


                    }
                }
            }
            catch (Exception ex)
            { //temporary - some unstable feature with Couchbase .net 2.0 beta
                _cluster = ClusterHelper.Get();
                Console.WriteLine("Error: " + ex.Message);
                return new List<T>();
            }
            return finalResult;
        }

        public virtual bool Create(T value, PersistTo persist = PersistTo.Zero)
        {
            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                string key = BuildKey(value);
                value.Id = key;
                IOperationResult<T> result = null;
                int retry = 0;
                while (retry <= 3)
                {
                    try
                    {
                        result = bucket.Upsert<T>(key, value, ReplicateTo.Zero, persist);
                        break;
                    }
                    catch
                    {
                        retry++;
                    }
                }

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
                IOperationResult<T> result = null;
                int retry = 0;
                while (retry <= 3)
                {
                    try
                    {
                        //If zerop we use Upsert without persist to be compatible with memcached bucket
                        if (persist == PersistTo.Zero)
                            result = bucket.Upsert<T>(key, value, ttl);
                        else
                            result = bucket.Upsert<T>(key, value, ttl, ReplicateTo.Zero, persist);
                        break;
                    }
                    catch
                    {
                        retry++;
                    }
                }
                if (result.Exception != null) throw result.Exception;
                return result.Success;
            }
        }

        public virtual bool Update(T value, PersistTo persist = PersistTo.Zero)
        {
            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                IOperationResult<T> result = null;
                int retry = 0;
                while (retry <= 3)
                {
                    try
                    {
                        result = bucket.Upsert<T>(value.Id, value, ReplicateTo.Zero, persist);
                        break;
                    }
                    catch
                    {
                        retry++;
                    }
                }
                if (result.Exception != null) throw result.Exception;
                return result.Success;
            }
        }

        public virtual bool Save(T value, PersistTo persist = PersistTo.Zero)
        {
            var key = string.IsNullOrEmpty(value.Id) ? BuildKey(value) : value.Id;
            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                IOperationResult<T> result = null;
                int retry = 0;
                while (retry <= 3)
                {
                    try
                    {
                        result = bucket.Replace<T>(key, value, ReplicateTo.Zero, persist);
                        break;
                    }
                    catch
                    {
                        retry++;
                    }
                }
                if (result.Exception != null) throw result.Exception;
                return result.Success;
            }
        }

        public virtual T Get(string key)
        {
            int retry = 0;

            while (retry <= 3)
            {
                try
                {
                    using (var bucket = _cluster.OpenBucket(_bucketName))
                    {
                        var result = bucket.GetDocument<T>(key);
                        if (result.Success)
                        {
                            return result.Value;
                        }
                        else return null;
                    }
                }

                catch
                {
                    retry++;
                }
            }
            return null;
        }

        public virtual bool Delete(string key, PersistTo persist = PersistTo.Zero)
        {
            using (var bucket = _cluster.OpenBucket(_bucketName))
            {
                IOperationResult result = null;
                int retry = 0;
                while (retry <= 3)
                {
                    try
                    {
                        result = bucket.Remove(key, ReplicateTo.Zero, persist);
                        break;
                    }
                    catch
                    {
                        retry++;
                    }
                }
                return result.Success;
            }
        }
    }
}
