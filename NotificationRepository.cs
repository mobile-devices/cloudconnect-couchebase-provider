using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Operations;
using Enyim.Caching.Memcached.Results;
using StackExchange.Redis;

namespace MD.CloudConnect.CouchBaseProvider
{
    public class NotificationRepository //: RepositoryBase<Notification>
    {
        private const string KEY = "notif";
        private const int MAX_SIZE = 500000;

        public bool PushNotificationCache(string data) //(string key, string data, DateTime recorded_date, int index = 0, int expiration = 604800, PersistTo persist = PersistTo.Zero)
        {
            IDatabase db = CouchbaseManager.Instance.RedisDatabase;
            if (db.ListLength(KEY) > MAX_SIZE)
                throw new Exception("too many data");
            else
                return db.ListRightPush(KEY, data) > 0;

        }

        public bool PushNotificationCache(string[] data) //(string key, string data, DateTime recorded_date, int index = 0, int expiration = 604800, PersistTo persist = PersistTo.Zero)
        {
            List<RedisValue> values = new List<RedisValue>();
            foreach (string d in data)
                values.Add(d);
            IDatabase db = CouchbaseManager.Instance.RedisDatabase;
            if (db.ListLength(KEY) > MAX_SIZE)
                throw new Exception("too many data");
            else
                return db.ListRightPush(KEY, values.ToArray()) > 0;

        }

        public List<string> RequestNotificationCache(int limit = 5000)
        {
            IDatabase db = CouchbaseManager.Instance.RedisDatabase;
            RedisValue[] values = db.ListRange(KEY, 0, limit - 1);
            return (from v in values select v.ToString()).ToList();
        }

        public void DropListRange(int limit = 5000)
        {
            IDatabase db = CouchbaseManager.Instance.RedisDatabase;
            db.ListTrim(KEY, limit, -1);
        }
    }
}
