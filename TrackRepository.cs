using Couchbase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CloudConnect.CouchBaseProvider
{
    public class TrackRepository : RepositoryBase<Track>
    {
        public TrackRepository(Cluster cluster, string bucketName) : base(cluster, bucketName) { } 

        public List<Track> GetAllByKeyAndDropped(string startKey = null, string endKey = null, int limit = 0, bool allowStale = false)
        {
            return GetViewResult<Track>("all_by_imei_and_date_key", startKey, endKey, limit, allowStale);
        }

        public bool CreateTrack(Track data, uint expiration = 3672000, PersistTo persist = PersistTo.Zero)
        {
            DateTime created = DateTime.UtcNow;

            data.Created_at = created;
            data.DateKey = data.Recorded_at.Year * 10000 + data.Recorded_at.Month * 100 + data.Recorded_at.Day;
            return this.CreateWithExpireTime(data, expiration, persist);
        }
    }
}
