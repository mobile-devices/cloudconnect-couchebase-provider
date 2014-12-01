using Couchbase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Couchbase.Operations;
using Enyim.Caching.Memcached.Results;

namespace MD.CloudConnect.CouchBaseProvider
{
    public class TrackRepository : RepositoryBase<Track>
    {
        //public TrackRepository(CouchbaseClient cluster) : base(cluster) { }

        public List<Track> GetAllByKeyAndStatusOk(object[] startKey = null, object[] endKey = null,
            int limit = 1000, bool allowStale = false, bool inclusiveEnd = false, string startKeyDocId = null)
        {
            return GetViewResult("all_by_imei_and_date_key", startKey, endKey, limit, allowStale
                , inclusiveEnd: inclusiveEnd, startKeyDocId: startKeyDocId);
        }

        public bool CreateTrack(Track data, int expiration = 2592000, PersistTo persist = PersistTo.Zero)
        {
            DateTime created = DateTime.UtcNow;
            data.Created_at = created;
            data.DateKey = data.Recorded_at.Year * 10000 + data.Recorded_at.Month * 100 + data.Recorded_at.Day;
            return this.CreateWithExpireTime(data, expiration, persist);
        }

        public List<Track> GetData(string imei, int datekey, string startDocId = null, bool allowStale = false, int limit = 1000)
        {
            //string key = String.Format("[\"{0}\",{1}]", imei, datekey);
            return GetAllByKeyAndStatusOk(new object[] { imei, datekey }, new object[] { imei, datekey }, limit, allowStale, true, startDocId);
        }

        public List<Track> GetNotDecodedTrack(int limit = 1000, bool stale = false, string startKeyDocId = "")
        {
            return GetViewResult("all_by_status", new object[] { 0 }, new object[] { 0 }, limit, stale, true, startKeyDocId);
        }

        //public int SizeOfCache(string asset)
        //{
        //    List<int> result = GetViewResult<int>("stack_size_by_asset", new object[] { asset }, new object[] { asset }, 1, false, true);
        //    if (result.Count == 0)
        //        return 0;
        //    else
        //        return result.First<int>();
        //}

        public bool BulkUpsert(List<Track> data, uint expiration = 2592000)
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

                items.Add(t.Id, t);
            }

            IDictionary<string, IStoreOperationResult> result = BulkUpsert(items, expiration);
            foreach (KeyValuePair<string, IStoreOperationResult> item in result)
            {
                if (!item.Value.Success)// && (item.Value.StatusCode != 2 || item.Value.StatusCode != 1))
                {
                    int index = 0;
                    while (index < 3)
                    {
                        try
                        {
                            if (Save(items[item.Key]))
                                break;
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

        public override string BuildKey(Track model)
        {
            return String.Format("{0}:{1}", model.Imei, model.CloudId);
            //            return String.Format("{0}:{1}:{2:00000}", model.Imei, model.ConnectionId, model.Index);
        }
    }
}
