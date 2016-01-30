using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Couchbase.Authentication;
using Couchbase.Core.Buckets;
using Couchbase.Management;

namespace CouchMan
{
    public class LocalBucketConfiguration
    {
        public LocalBucketConfiguration()
        {
            this.BucketPassword = "";
            this.RamQuota = 100;
            this.BucketType = BucketTypeEnum.Couchbase;
            this.ReplicaNumber = ReplicaNumber.Two;
            this.AuthType = AuthType.Sasl;
            this.ThreadNumber = ThreadNumber.Two;
        }
        public string Name { get; set; }
        public ulong RamQuota { get; set; }
        public BucketTypeEnum BucketType { get; set; }
        public ReplicaNumber ReplicaNumber { get; set; }
        public bool IndexReplicas { get; set; }
        public bool FlushEnabled { get; set; }
        public AuthType AuthType { get; set; }
        public string BucketPassword { get; set; }
        public ThreadNumber ThreadNumber { get; set; }
    }
}
