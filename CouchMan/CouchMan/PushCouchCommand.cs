using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using Couchbase.Configuration;
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

        protected override void ProcessRecord()
        {
            var cbConfig = new CouchbaseClientConfiguration()
            {
                Username = AdminUsername,
                Password = AdminPassword
            };

            cbConfig.Urls.Add(new Uri(InstanceUri));

            var cbc = new CouchbaseCluster(cbConfig);
            Bucket[] remoteBuckets = new Bucket[1];
            try
            {
                remoteBuckets = cbc.ListBuckets();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "Cannot connect to server:" + InstanceUri, ErrorCategory.ConnectionError, InstanceUri));
                return;
            }

            var localBucketsPath = this.SessionState.Path.CurrentFileSystemLocation.Path;
            WriteDebug("localBucketsPath:" + localBucketsPath);
            //get list of local buckets
            var localBucketsPaths = Directory.EnumerateDirectories(localBucketsPath, "*", SearchOption.TopDirectoryOnly);


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

                var bucketFromConfigFile = new Bucket();
                try
                {
                    bucketFromConfigFile = JsonConvert.DeserializeObject<Bucket>(jsonFile);
                }
                catch (Exception ex)
                {
                    WriteDebug(JsonConvert.SerializeObject(bucketFromConfigFile));
                    WriteError(new ErrorRecord(ex, "CannotParseConfigFile:" + localBucketName, ErrorCategory.WriteError, bucketFromConfigFile));
                }

                // if we don't have bucket name in the config use the folder name
                if (string.IsNullOrEmpty(bucketFromConfigFile.Name))
                {
                    bucketFromConfigFile.Name = localBucketName;
                }
                else
                {
                    localBucketName = bucketFromConfigFile.Name;
                }

                //if no remote buckets match the local folder name create one
                if (!remoteBuckets.Any(x => x.Name == localBucketName))
                {
                    try
                    {
                        cbc.CreateBucket(bucketFromConfigFile);
                    }
                    catch (WebException ex)
                    {
                        WriteDebug(JsonConvert.SerializeObject(bucketFromConfigFile));
                        WriteError(new ErrorRecord(ex, "CannotCreateBucket:" + localBucketName, ErrorCategory.WriteError, bucketFromConfigFile));
                    }
                }

                var designs = Directory.EnumerateDirectories(localbucketPath, "*", SearchOption.TopDirectoryOnly).ToList();

                foreach (var design in designs)
                {
                    var designDocs = new ViewsHolder();
                    var designName = new DirectoryInfo(design).Name;

                    try
                    {
                        cbc.DeleteDesignDocument(localBucketName, designName);
                    }
                    catch (WebException ex)
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

                    try
                    {
                        WriteObject("Attempting to create design: " + designName);
                        cbc.CreateDesignDocument(localBucketName, designName, JsonConvert.SerializeObject(designDocs));
                        WriteObject("Success creating design: " + designName);
                    }
                    catch (WebException ex)
                    {
                        WriteWarning("CannotCreateDesign:" + designName);

                    }
                }
            }
        }
    }
}
