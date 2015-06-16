using Couchbase;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Couchbase.Operations;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Couchbase.Protocol;


namespace MD.CloudConnect.CouchBaseProvider
{
    public abstract class RepositoryBase<T> where T : ModelBase
    {
        protected static CouchbaseClient _cluster { get; set; }
        protected readonly string _designDoc;

        public RepositoryBase()
        {
            _cluster = new CouchbaseClient();
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

        private string serializeAndIgnoreId(T obj)
        {
            var json = JsonConvert.SerializeObject(obj, new JsonSerializerSettings()
            {
                ContractResolver = new DocumentIdContractResolver(),
            });
            return json;
        }

        private class DocumentIdContractResolver : CamelCasePropertyNamesContractResolver
        {
            protected override List<MemberInfo> GetSerializableMembers(Type objectType)
            {
                return base.GetSerializableMembers(objectType).Where(o => o.Name != "Id").ToList();
            }
        }

        public IDictionary<string, IStoreOperationResult> BulkUpsert(IDictionary<string, T> items, uint expiration = 0, bool createOnly = false)
        {
            ConcurrentDictionary<string, IStoreOperationResult> result = new ConcurrentDictionary<string, IStoreOperationResult>();
            Parallel.ForEach(items, x =>
            {
                if (createOnly)
                    result.TryAdd(x.Key, _cluster.ExecuteStore(StoreMode.Add, BuildKey(x.Value), serializeAndIgnoreId(x.Value), new TimeSpan(0, 0, (int)expiration)));
                else
                    result.TryAdd(x.Key, _cluster.ExecuteStore(StoreMode.Set, BuildKey(x.Value), serializeAndIgnoreId(x.Value), new TimeSpan(0, 0, (int)expiration)));
            });
            return result;
        }

        public virtual List<T> GetAll(int limit = 1000, string startKeyDocId = null, bool allowStale = false)
        {
            return GetViewResult("all", limit: limit, startKeyDocId: startKeyDocId, allowStale: allowStale);
        }

        public virtual List<T> GetDocs(List<string> keys)
        {
            List<T> docs = new List<T>();

            ConcurrentDictionary<string, IGetOperationResult<string>> dict = new ConcurrentDictionary<string, IGetOperationResult<string>>();
            Parallel.ForEach(keys, x =>
            {
                dict.TryAdd(x, _cluster.ExecuteGet<string>(x));
            });

            // re-order data before deserialize
            Dictionary<string, IGetOperationResult<string>> listOperations = (from d in dict orderby d.Key select d).ToDictionary(x => x.Key, x => x.Value);

            foreach (var item in listOperations)
            {
                if (item.Value.Exception != null)
                    throw item.Value.Exception;
                if (item.Value.StatusCode != 1)
                {
                    if (item.Value.Value == null)
                    {
                        return new List<T>();
                    }
                    var model = JsonConvert.DeserializeObject<T>(item.Value.Value);
                    model.Id = item.Key;
                    docs.Add(model);
                }
            }

            return docs;
        }

        public virtual List<T> GetViewResult(string viewName, object[] startKey = null, object[] endKey = null,
                                                int limit = 0, bool allowStale = false, bool inclusiveEnd = false, string startKeyDocId = null)
        {
            var query = _cluster.GetView(_designDoc, viewName, true).WithInclusiveEnd(inclusiveEnd);

            if (limit > 0)
                query = query.Limit(limit);
            if (!allowStale)
                query = query.Stale(StaleMode.False);
            else
                query = query.Stale(StaleMode.UpdateAfter);
            if (startKey != null)
            {
                if (startKey.Count() == 1)
                    query = query.StartKey(startKey.First());
                else
                    query = query.StartKey(startKey);
            }
            if (endKey != null)
            {
                if (endKey.Count() == 1)
                    query = query.EndKey(endKey.First());
                else
                    query = query.EndKey(endKey);
            }
            if (!string.IsNullOrEmpty(startKeyDocId))
                query = query.StartDocumentId(startKeyDocId);

            List<T> docs = new List<T>();

            List<string> keys = query.Select(x => x.ItemId).ToList();
            ConcurrentDictionary<string, IGetOperationResult<string>> dict = new ConcurrentDictionary<string, IGetOperationResult<string>>();
            Parallel.ForEach(keys, x =>
            {
                dict.TryAdd(x, _cluster.ExecuteGet<string>(x));
            });

            // re-order data before deserialize
            Dictionary<string, IGetOperationResult<string>> listOperations = (from d in dict orderby d.Key select d).ToDictionary(x => x.Key, x => x.Value);

            foreach (var item in listOperations)
            {
                if (item.Value.Exception != null)
                    throw item.Value.Exception;
                if (item.Value.StatusCode != 1)
                {
                    if (item.Value.Value == null)
                    {
                        return new List<T>();
                    }
                    var model = JsonConvert.DeserializeObject<T>(item.Value.Value);
                    model.Id = item.Key;
                    docs.Add(model);
                }
            }

            return docs;
        }

        public virtual bool Create(T value, PersistTo persistTo = PersistTo.Zero)
        {
            var result = _cluster.ExecuteStore(StoreMode.Add, BuildKey(value), serializeAndIgnoreId(value), persistTo);
            if (result.Exception != null)
                throw result.Exception;
            return result.Success;
        }

        public virtual bool CreateWithExpireTime(T value, int ttl, PersistTo persistTo = PersistTo.Zero)
        {
            var result = _cluster.ExecuteStore(StoreMode.Add, BuildKey(value), serializeAndIgnoreId(value), new TimeSpan(0, 0, ttl), persistTo);
            if (result.Exception != null)
                throw result.Exception;
            return result.Success;
        }

        public virtual bool Update(T value, PersistTo persistTo = PersistTo.Zero)
        {
            var result = _cluster.ExecuteStore(StoreMode.Replace, value.Id, serializeAndIgnoreId(value), persistTo);
            if (result.Exception != null)
                throw result.Exception;
            return result.Success;
        }

        public virtual bool SaveWithExpireTime(T value, int ttl, PersistTo persistTo = PersistTo.Zero)
        {
            var key = string.IsNullOrEmpty(value.Id) ? BuildKey(value) : value.Id;
            var result = _cluster.ExecuteStore(StoreMode.Set, key, serializeAndIgnoreId(value), new TimeSpan(0, 0, ttl), persistTo);
            if (result.Exception != null)
                throw result.Exception;
            return result.Success;
        }

        public virtual bool Save(T value, PersistTo persistTo = PersistTo.Zero)
        {
            var key = string.IsNullOrEmpty(value.Id) ? BuildKey(value) : value.Id;
            IStoreOperationResult result = _cluster.ExecuteStore(StoreMode.Set, key, serializeAndIgnoreId(value), persistTo);
            if (result.Exception != null)
                throw result.Exception;
            return result.Success;
        }

        public virtual bool SaveWithCas(T value, ulong cas, PersistTo persistTo = PersistTo.Zero)
        {
            var key = string.IsNullOrEmpty(value.Id) ? BuildKey(value) : value.Id;
            IStoreOperationResult casResult = _cluster.ExecuteCas(StoreMode.Set, key, serializeAndIgnoreId(value), cas);
            if (casResult.Exception != null)
                throw casResult.Exception;
            return casResult.Success;
        }

        public virtual void UnLock(T value)
        {
            if (value.CasID > 0)
                _cluster.ExecuteUnlock(value.Id, value.CasID);
        }

        public virtual bool KeyExist(string key)
        {
            return _cluster.KeyExists(key);
        }

        public virtual T Get(string key)
        {
            var result = _cluster.ExecuteGet<string>(key);
            if (result.Exception != null)
                throw result.Exception;

            if (result.Value == null)
            {
                return null;
            }

            var model = JsonConvert.DeserializeObject<T>(result.Value);
            model.Id = key; //Id is not serialized into the JSON document on store, so need to set it before returning
            return model;
        }

        public virtual T GetWithLock(string key, out bool locked)
        {
            var result = _cluster.ExecuteGetWithLock<string>(key, new TimeSpan(0, 5, 0));
            if (result.Exception != null)
                throw result.Exception;
            if (result.StatusCode == (int)CouchbaseStatusCodeEnums.LockError)
                locked = true;
            else
                locked = false;

            if (result.Value == null)
            {
                return null;
            }

            var model = JsonConvert.DeserializeObject<T>(result.Value);
            model.Id = key; //Id is not serialized into the JSON document on store, so need to set it before returning
            model.CasID = result.Cas;
            return model;
        }

        public virtual bool Delete(string key, PersistTo persistTo = PersistTo.Zero)
        {
            var result = _cluster.ExecuteRemove(key, persistTo);
            if (result.Exception != null)
                throw result.Exception;
            return result.Success;
        }


    }
}
