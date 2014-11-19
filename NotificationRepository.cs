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

        public bool PushNotificationCache(string key, string data, DateTime recorded_date, int index = 0, uint expiration = 86400, PersistTo persist = PersistTo.Zero)
        {
            Notification notif = new Notification()
            {
                Created_at = DateTime.Now,
                Data = data,
                Status = 0,
                Key = key,
                Index = index,
                Received_at = recorded_date.Ticks
            };
            return this.CreateWithExpireTime(notif, expiration, persist);
        }

        public List<Notification> GetAllByStatus(string startKey = null, string endKey = null, int limit = 0, bool allowStale = false)
        {
            return GetViewResult<Notification>("by_status", startKey, endKey, limit, allowStale, inclusiveEnd: true);
        }

        public List<Notification> GetAllByKeyAndStatus(string startKey = null, int limit = 0, bool allowStale = false, string startDocID = null)
        {
            return GetViewResult<Notification>("by_key_and_status", startKey, startKey, limit, allowStale, inclusiveEnd: true, startKeyDocId: startDocID);
        }

        public List<Notification> RequestNotificationCache(string key, int limit = 5000, bool slate = false, string startDocId = null)
        {
            List<Notification> result = GetAllByKeyAndStatus(String.Format("[\"{0}\", 0]", key), limit, slate, startDocId);
            if (result.Count > 0)
            {
                return result.OrderBy(x => x.Received_at).ToList();
            }
            else
                return new List<Notification>();
        }

        public int SizeOfCache()
        {
            List<int> result = GetViewResult<int>("stack_size");
            if (result.Count == 0)
                return 0;
            else
                return result.First<int>();
        }

        public bool BulkUpsert(List<Notification> data, uint expiration = 86400)
        {
            IDictionary<string, Notification> items = new Dictionary<string, Notification>();
            foreach (Notification notif in data)
            {
                if (String.IsNullOrEmpty(notif.Id))
                    notif.Id = this.BuildKey(notif);
                items.Add(notif.Id, notif);
            }
            IDictionary<string, IOperationResult<Notification>> result = BulkUpsert(items, expiration);
            foreach (KeyValuePair<string, IOperationResult<Notification>> item in result)
            {
                if (!item.Value.Success)
                {
                    int index = 0;
                    while (index < 3)
                    {
                        try
                        {
                            if (Save(item.Value.Value))
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

        public override string BuildKey(Notification model)
        {
            return String.Format("{0}:{1}:{2:0000}", model.Key, model.Created_at.Ticks, model.Index);
        }

    }
}
