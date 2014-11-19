using Couchbase;
using Couchbase.Configuration.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CloudConnect.CouchBaseProvider
{
    public class CouchbaseManager
    {
        protected static Cluster _cluster = null;

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

        public void RegisterBucketConfiguration(string sectionName, string bucketName)
        {
            ClusterHelper.Initialize(sectionName);
            _cluster = ClusterHelper.Get();
            _notificationRepository = new NotificationRepository(_cluster, bucketName);
            _deviceRepository = new DeviceRepository(_cluster, bucketName);
            _trackRepository = new TrackRepository(_cluster, bucketName);
            _fieldDefinitionRepository = new FieldDefinitionRepository(_cluster, bucketName);
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
    }

}
