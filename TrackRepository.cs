using Couchbase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Couchbase.Operations;
using Enyim.Caching.Memcached.Results;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace MD.CloudConnect.CouchBaseProvider
{
    public class TrackRepository : RepositoryBase<Track>
    {
        private const string KEY = "track";
        //public TrackRepository(CouchbaseClient cluster) : base(cluster) { }

        public List<Track> GetAllByKeyAndStatusOk(object[] startKey = null, object[] endKey = null,
            int limit = 1000, bool allowStale = false, bool inclusiveEnd = false, string startKeyDocId = null)
        {
            return GetViewResult("all_by_imei_and_date_key", startKey, endKey, limit, allowStale
                , inclusiveEnd: inclusiveEnd, startKeyDocId: startKeyDocId);
        }

        //public bool CreateTrack(Track data, int expiration = 2592000, PersistTo persist = PersistTo.Zero)
        //{
        //    IDatabase db = CouchbaseManager.Instance.RedisDatabase;
        //    string track = JsonConvert.SerializeObject(data);
        //    return db.ListRightPush(KEY, track) > 0;

        //    //DateTime created = DateTime.UtcNow;
        //    //data.Created_at = created;
        //    //data.DateKey = data.Recorded_at.Year * 10000 + data.Recorded_at.Month * 100 + data.Recorded_at.Day;
        //    //if (!CouchbaseManager.Instance.DataListRepository.PushToList(data, expiration))
        //    //    throw new Exception("DataList Error");
        //    //return this.CreateWithExpireTime(data, expiration, persist);
        //}

        public List<Track> GetData(string imei, int datekey, string startDocId = null, bool allowStale = false, int limit = 1000)
        {
            //string key = String.Format("[\"{0}\",{1}]", imei, datekey);
            return GetAllByKeyAndStatusOk(new object[] { imei, datekey }, new object[] { imei, datekey }, limit, allowStale, true, startDocId);
        }

        public List<Track> GetNotDecodedTrack(int limit = 1000)
        {
            List<Track> result = new List<Track>();

            IDatabase db = CouchbaseManager.Instance.RedisDatabase;
            RedisValue[] values = db.ListRange(KEY, 0, limit - 1);

            foreach (RedisValue val in values)
                result.Add(JsonConvert.DeserializeObject<Track>(val.ToString()));
            return result;
        }

        public void DropListRange(List<Track> removed_tracks, List<Track> all)
        {
            IDatabase db = CouchbaseManager.Instance.RedisDatabase;
            db.ListTrim(KEY, all.Count, -1);
            List<string> cloudIDS = removed_tracks.Select(x => x.CloudId).ToList();
            List<RedisValue> values = new List<RedisValue>();
            foreach (Track t in all)
            {
                if (!cloudIDS.Contains(t.CloudId))
                    values.Add(JsonConvert.SerializeObject(t));
            }
            db.ListLeftPush(KEY, values.ToArray());
        }

        public List<Track> GetAllDecodedTrack(int limit = 1000, bool stale = false, string startKeyDocId = "")
        {
            return GetViewResult("all_by_status", new object[] { 1 }, new object[] { 10 }, limit, stale, true, startKeyDocId);
        }

        //public int SizeOfCache(string asset)
        //{
        //    List<int> result = GetViewResult<int>("stack_size_by_asset", new object[] { asset }, new object[] { asset }, 1, false, true);
        //    if (result.Count == 0)
        //        return 0;
        //    else
        //        return result.First<int>();
        //}

        public bool BulkUpsert(List<Track> data, uint expiration = 604800)
        {
            if (data.Count == 0) return true;
            IDictionary<string, Track> items = new Dictionary<string, Track>();
            foreach (Track t in data)
            {

                if (String.IsNullOrEmpty(t.Id))
                {
                    t.Id = this.BuildKey(t);
                    t.Created_at = DateTime.UtcNow;
                }
                if (t.DateKey == 0)
                    t.DateKey = t.Recorded_at.Year * 10000 + t.Recorded_at.Month * 100 + t.Recorded_at.Day;
                if (!items.ContainsKey(t.Id))
                    items.Add(t.Id, t);
            }
            IDictionary<string, Track> monthItmems = items.Where(k => k.Value.Account == "municio").ToDictionary(x => x.Key, x => x.Value);
            IDictionary<string, IStoreOperationResult> result = BulkUpsert(monthItmems, 2592000);
            foreach (var element in BulkUpsert(items.Where(k => k.Value.Account != "municio").ToDictionary(x => x.Key, x => x.Value), expiration))
                result.Add(element);

            foreach (KeyValuePair<string, IStoreOperationResult> item in result)
            {
                if (!item.Value.Success)
                {
                    int index = 0;
                    while (index < 3)
                    {
                        try
                        {
                            if (items[item.Key].Status > 0)
                            {
                                if (SaveWithExpireTime(items[item.Key], 0))
                                    break;

                            }
                            else
                            {
                                if (Save(items[item.Key]))
                                    break;
                            }

                        }
                        catch (Exception ex) { Console.WriteLine(ex.Message); }
                        index++;
                    }
                    if (index >= 3)
                        return false;
                }
            }
            return true;
        }

        public void Drop(Track model)
        {
            model.Fields.Clear();
            SaveWithExpireTime(model, 10);
        }

        public override string BuildKey(Track model)
        {
            return String.Format("{0}:{1}", model.Imei, model.CloudId);
            //            return String.Format("{0}:{1}:{2:00000}", model.Imei, model.ConnectionId, model.Index);
        }

        public void InsertListShouldBeTreat(List<MD.CloudConnect.CouchBaseProvider.Track> tracks)
        {
            IDatabase db = CouchbaseManager.Instance.RedisDatabase;
            List<RedisValue> data = new List<RedisValue>();
            
            foreach (Track t in tracks)           
                data.Add(JsonConvert.SerializeObject(t));             
            db.ListRightPush(KEY, data.ToArray());
        }
    }
}
