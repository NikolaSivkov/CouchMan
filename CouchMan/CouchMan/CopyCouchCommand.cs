using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Server.Serialization;
using Couchbase.Core;
using Couchbase.Core.Buckets;
using Couchbase.Management;
using Newtonsoft.Json;


namespace CouchMan
{
    /// <summary>
    /// This Cmdlet pushes couchbase views to the respective buckets
    /// </summary>
    [Cmdlet(VerbsCommon.Copy, "Couch")]
    public class CopyCouchCommand : PSCmdlet
    {
        public CopyCouchCommand()
        {
            // by default we assume it's the local instance unless this property is set explicitly
            InstanceUri = "http://localhost:8091/pools";
        }

        [Alias("path", "p")]
        [Parameter(Mandatory = false)]
        public string ViewsPath { get; set; }

        [Alias("u")]
        [Parameter(Mandatory = true)]
        public string AdminUsername { get; set; }

        [Alias("pw")]
        [Parameter(Mandatory = true)]
        public string AdminPassword { get; set; }

        [Alias("InstanceUrl", "uri", "url")]
        [Parameter(Mandatory = false)]
        public string InstanceUri { get; set; }

        [Alias("b")]
        [Parameter(Mandatory = false)]
        public string Bucket { get; set; }

        protected override void ProcessRecord()
        {
            var clientConfig = new ClientConfiguration();
            //by default it contains the url to localhost, so we need to remove it if it's a remote server.
            clientConfig.Servers.Clear();

            clientConfig.Servers.Add(new Uri(InstanceUri));

            var cbc = new Cluster(clientConfig);
            cbc.Configuration.ApiPort = 8091;
            var clusterManager = cbc.CreateManager(AdminUsername, AdminPassword);

            IList<BucketConfig> remoteBuckets = new List<BucketConfig>();

            var remoteBucketsResult = clusterManager.ListBuckets();
            if (!remoteBucketsResult.Success)
            {
                WriteError(new ErrorRecord(new Exception(), "Cannot connect to server:" + InstanceUri, ErrorCategory.ConnectionError, InstanceUri));
                return;
            }

            remoteBuckets = remoteBucketsResult.Value;


            if (!string.IsNullOrEmpty(Bucket))
            {
                remoteBuckets = remoteBuckets.Where(x => String.Equals(x.Name, Bucket, StringComparison.CurrentCultureIgnoreCase)).ToList();
                if (!remoteBuckets.Any())
                {
                    WriteError(new ErrorRecord(new Exception("No bucket with name " + Bucket), "No bucket with name " + Bucket, ErrorCategory.ObjectNotFound, Bucket));
                    return;
                }
            }

            var savePath = this.SessionState.Path.CurrentFileSystemLocation.Path;
            WriteDebug("localBucketsPath:" + savePath);
            //get list of local buckets


            foreach (var bucket in remoteBuckets.Where(x => x.BucketType == "membase"))
            {
                //gets just the name of the folder
                WriteDebug("processing " + bucket.Name);
                IBucketManager bm = null;
                try
                {
                    var bb = bucket.AuthType == "sasl" ? cbc.OpenBucket(bucket.Name, bucket.SaslPassword) : cbc.OpenBucket(bucket.Name);

                    bm = bb.CreateManager(AdminUsername, AdminPassword);
                }
                catch (System.AggregateException ex)
                {

                    throw ex;
                }


                var designDocsResult = bm.GetDesignDocuments();
                if (!designDocsResult.Success)
                {
                    WriteObject("Cannot Process: " + bucket.Name + " ; Error:" + designDocsResult.Exception.Message);
                }

                var bucketDir = Directory.CreateDirectory(Path.Combine(savePath, bucket.Name));

                
                //Write Bucket Config file.
                File.WriteAllText(Path.Combine(bucketDir.FullName, "config.json"), JsonConvert.SerializeObject(ToLocalConfig(bucket)));

                var deserializedDesigns = JsonConvert.DeserializeObject<Rootobject>(bm.GetDesignDocuments().Value);

                foreach (var design in deserializedDesigns.rows)
                {
                    var designDir = Directory.CreateDirectory(Path.Combine(bucketDir.FullName, design.doc.meta.id.Replace("_design/", "")));

                    foreach (var view in design.doc.json.views)
                    {
                        File.WriteAllText(Path.Combine(designDir.FullName, view.Key + ".js"), view.Value.map);
                    }
                }
            }
        }

        private static LocalBucketConfiguration ToLocalConfig(BucketConfig bc)
        {
            return new LocalBucketConfiguration()
            {
                AuthType = bc.AuthType == "sasl" ? AuthType.Sasl : AuthType.None,
                BucketPassword = bc.SaslPassword,
                BucketType = BucketTypeEnum.Couchbase,
                IndexReplicas = bc.ReplicaIndex,
                Name = bc.Name,
                RamQuota = (bc.Quota.Ram/1024/1024),// the value is stored in bytes so we convert it back to megabytes
                // and it should be done only when saving the config to local file
                ReplicaNumber = (ReplicaNumber)bc.ReplicaNumber,
                ThreadNumber = (ThreadNumber)bc.ThreadsNumber
            };
        }

    }
}
