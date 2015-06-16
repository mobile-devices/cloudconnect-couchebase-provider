using Couchbase;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MD.CloudConnect.CouchBaseProvider
{
    public class CouchbaseManager
    {
        protected static CouchbaseClient _cluster = null;
        protected static ConnectionMultiplexer _redis = null;
        protected static string _redisConnection = null;

        #region singleton
        protected static readonly CouchbaseManager _instance = new CouchbaseManager();
        public static CouchbaseManager Instance
        {
            get
            {
                lock (_instance)
                {
                    return _instance;
                }
            }
        }

        static CouchbaseManager()
        {

        }
        #endregion

        public void RegisterBucketConfiguration(string sectionName, string bucketName, string redis_connection)
        {
            _redisConnection = redis_connection;
            _redis = ConnectionMultiplexer.Connect(redis_connection);
            _notificationRepository = new NotificationRepository();
            _deviceRepository = new DeviceRepository();
            _trackRepository = new TrackRepository();
            _fieldDefinitionRepository = new FieldDefinitionRepository();
            _dataListRepository = new DataListRepository();
        }

        public bool CanWriteInRedis
        {
            get
            {
                try
                {
                   // Should used INFO
                   //IGrouping<string, KeyValuePair<string,string>>[] infos =_redis.GetServer(_redisConnection, 6379).Info();
                   //tring used_memory_string = "0";// infos.Where(x => x.Key == "used_memory").First().Value;
                   // uint memory_used = Convert.ToUInt32(used_memory_string);
                   // return memory_used < 1024;
                    return false;
                    
                }
                catch
                {
                    return false;
                }
            }
        }

        public IDatabase RedisDatabase
        {
            get
            {

                return _redis.GetDatabase();
            }
        }

        private NotificationRepository _notificationRepository;
        public NotificationRepository NotificationRepository
        {
            get { return _notificationRepository; }
        }

        private DeviceRepository _deviceRepository;
        public DeviceRepository DeviceRepository
        {
            get { return _deviceRepository; }
        }

        private TrackRepository _trackRepository;
        public TrackRepository TrackRepository
        {
            get { return _trackRepository; }
        }

        private FieldDefinitionRepository _fieldDefinitionRepository;
        public FieldDefinitionRepository FieldDefinitionRepository
        {
            get { return _fieldDefinitionRepository; }
        }

        private DataListRepository _dataListRepository;
        public DataListRepository DataListRepository
        {
            get { return _dataListRepository; }
        }
    }

}
