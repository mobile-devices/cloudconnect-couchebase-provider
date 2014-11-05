using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Couchbase;

namespace CloudConnect.CouchBaseProvider
{
    public class NotificationRepository : RepositoryBase<Notification>
    {
        public NotificationRepository(Cluster cluster, string bucketName) : base(cluster, bucketName) { }

        public bool PushNotificationCache(string key, string data, DateTime recorded_date, uint expiration = 604800, PersistTo persist = PersistTo.Zero)
        {
            Notification notif = new Notification()
            {
                Created_at = DateTime.Now,
                Data = data,
                Dropped = false,
                Key = key,
                Received_at = recorded_date.Ticks
            };
            return this.CreateWithExpireTime(notif, expiration, persist);
        }

        public List<Notification> GetAllByDropped(string startKey = null, string endKey = null, int limit = 0, bool allowStale = false)
        {
            return GetViewResult<Notification>("by_dropped", startKey, endKey, limit, allowStale);
        }

        public List<Notification> GetAllByKeyAndDropped(string startKey = null, int limit = 0, bool allowStale = false)
        {
            return GetViewResult<Notification>("all_by_key_and_dropped", startKey, startKey, limit, allowStale);
        }

        public List<Notification> RequestNotificationCache(string key)
        {
            List<Notification> result = GetAllByKeyAndDropped(String.Format("[ \"{0}\" ]", key), 1000);
            return result.OrderBy(x => x.Received_at).ToList();
        }

        public int SizeOfCache()
        {
            List<Notification> result = GetAllByDropped("[ 'false' ]", "[ 'false' ]");
            return result.Count();
        }

    }
}
