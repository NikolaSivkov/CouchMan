using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using Couchbase;
using Couchbase.Configuration;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Server.Serialization;
using Couchbase.Management;
using Newtonsoft.Json;

namespace CouchMan
{
    /// <summary>
    /// This Cmdlet pushes couchbase views to the respective buckets
    /// </summary>
    [Cmdlet(VerbsCommon.Push, "Couch")]
    public class PushCouchCommand : PSCmdlet
    {
        public PushCouchCommand()
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

            clientConfig.Servers.Add(new Uri(InstanceUri));

            var cbc = new Cluster(clientConfig);

            var clusterManager = cbc.CreateManager(AdminUsername, AdminPassword);

            var remoteBuckets = new List<BucketConfig>();

            var resultListBuckets = clusterManager.ListBuckets();
            if (resultListBuckets.Success != true)
            {
                WriteError(new ErrorRecord(new Exception("Cannot connect to server" + InstanceUri), "Cannot connect to server:" + InstanceUri, ErrorCategory.ConnectionError, InstanceUri));
                return;
            }

            remoteBuckets = resultListBuckets.Value.ToList();





            var localBucketsPath = this.SessionState.Path.CurrentFileSystemLocation.Path;
            WriteDebug("localBucketsPath:" + localBucketsPath);

            //get list of local buckets
            var localBucketsPaths = Directory.EnumerateDirectories(localBucketsPath, "*", SearchOption.TopDirectoryOnly);

            if (!string.IsNullOrEmpty(Bucket))
            {
                remoteBuckets = remoteBuckets.Where(x => String.Equals(x.Name, Bucket, StringComparison.CurrentCultureIgnoreCase)).ToList();
                localBucketsPaths = localBucketsPaths.Where(x => String.Equals(new DirectoryInfo(x).Name, Bucket, StringComparison.CurrentCultureIgnoreCase)).ToList();

                if (!remoteBuckets.Any())
                {
                    WriteError(new ErrorRecord(new Exception("No bucket with name " + Bucket), "No bucket with name " + Bucket, ErrorCategory.ObjectNotFound, Bucket));
                    return;
                }
            }

            foreach (var localbucketPath in localBucketsPaths)
            {

                //gets just the name of the folder
                var localBucketName = new DirectoryInfo(localbucketPath).Name;

                WriteDebug("processing " + localBucketName);

                var jsonFiles = Directory.EnumerateFiles(localbucketPath, "config.json",
                        SearchOption.TopDirectoryOnly).ToList();

                if (!jsonFiles.Any())
                {
                    WriteWarning("No Configuration file for bucket:" + localBucketName);
                    continue;
                }

                var jsonFile = File.ReadAllText(jsonFiles.FirstOrDefault());

                var bucketConfig = new LocalBucketConfiguration();

                try
                {
                    bucketConfig = JsonConvert.DeserializeObject<LocalBucketConfiguration>(jsonFile);
                }
                catch (Exception ex)
                {
                    WriteDebug(JsonConvert.SerializeObject(bucketConfig));
                    WriteError(new ErrorRecord(ex, "CannotParseConfigFile:" + localBucketName, ErrorCategory.WriteError, bucketConfig));
                }

                // if we don't have bucket name in the config use the folder name
                if (string.IsNullOrEmpty(bucketConfig.Name))
                {
                    bucketConfig.Name = localBucketName;
                }
                else
                {
                    localBucketName = bucketConfig.Name;
                }

                //if no remote buckets match the local folder name create one
                if (!remoteBuckets.Any(x => x.Name == localBucketName))
                {

                    var createBucketResult = clusterManager.CreateBucket(
                            name: bucketConfig.Name,
                            ramQuota: (uint)bucketConfig.RamQuota,
                            bucketType: bucketConfig.BucketType,
                            replicaNumber: bucketConfig.ReplicaNumber,
                            authType: bucketConfig.AuthType,
                            indexReplicas: bucketConfig.IndexReplicas,
                            flushEnabled: bucketConfig.FlushEnabled,
                            saslPassword: bucketConfig.BucketPassword,
                            threadNumber: bucketConfig.ThreadNumber
                            );

                    if (!createBucketResult.Success)
                    {
                        WriteDebug(JsonConvert.SerializeObject(bucketConfig));
                        WriteError(new ErrorRecord(createBucketResult.Exception, "CannotCreateBucket:" + localBucketName, ErrorCategory.WriteError, createBucketResult));
                    }

                }

                var remoteBucketConfig = remoteBuckets.First(x => x.Name == localBucketName);

                var designs = Directory.EnumerateDirectories(localbucketPath, "*", SearchOption.TopDirectoryOnly).ToList();
                var remoteBucket = cbc.OpenBucket(remoteBucketConfig.Name, remoteBucketConfig.SaslPassword);
                var remoteBucketManager = remoteBucket.CreateManager(AdminUsername, AdminPassword);

                foreach (var design in designs)
                {
                    var designDocs = new ViewsHolder();
                    var designName = new DirectoryInfo(design).Name;


                    var removeDesignResult = remoteBucketManager.RemoveDesignDocument(designName);

                    if (!removeDesignResult.Success)
                    {
                        WriteDebug("design " + designName + " doesn't exist.");
                    }

                    var localViews =
                        Directory.EnumerateFiles(design, "*.js", SearchOption.TopDirectoryOnly).ToList();

                    designDocs.views = new Dictionary<string, MapString>();

                    foreach (var view in localViews)
                    {
                        var viewName = new FileInfo(view).Name.Replace(".js", "");
                        designDocs.views.Add(viewName, new MapString(File.ReadAllText(view)));
                    }


                    WriteObject("Attempting to create design: " + designName);
                    var insertDesingResult = remoteBucketManager.InsertDesignDocument(designName, JsonConvert.SerializeObject(designDocs));

                    if (!insertDesingResult.Success)
                    {
                        WriteWarning("CannotCreateDesign:" + designName);
                    }
                    else
                    {
                        WriteObject("Success creating design: " + designName);
                    }
                }
            }
        }
    }
}
