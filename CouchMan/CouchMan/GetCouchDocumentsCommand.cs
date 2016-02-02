using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Configuration.Client;
using Couchbase.Configuration.Server.Serialization;
using Couchbase.Core;
using Couchbase.Views;
using Newtonsoft.Json;

namespace CouchMan
{
    [Cmdlet(VerbsCommon.Get, "CouchDocs")]
    public class GetCouchDocumentsCommand: PSCmdlet
    {
        public GetCouchDocumentsCommand()
        {
            InstanceUri = "http://localhost:8091/pools";
        }

        [Alias("path", "p")]
        [Parameter(Mandatory = false)]
        public string SavePath { get; set; }
         
        [Alias("InstanceUrl", "uri", "url")]
        [Parameter(Mandatory = false)]
        public string InstanceUri { get; set; }

        [Alias("b")]
        [Parameter(Mandatory = true)]
        public string Bucket { get; set; }

        [Alias("d")]
        [Parameter(Mandatory = true)]
        public string DesignDoc { get; set; }

        [Alias("v")]
        [Parameter(Mandatory = true)]
        public string View { get; set; }

        [Alias("bpw")]
        [Parameter(Mandatory = false)]
        public string BucketPassword { get; set; }

        protected override async void ProcessRecord()
        {

            var clientConfig = new ClientConfiguration();

            clientConfig.Servers.Add(new Uri(InstanceUri));

            var cbc = new Cluster(clientConfig);
            

            var localSavePath = SavePath ?? this.SessionState.Path.CurrentFileSystemLocation.Path;

            WriteDebug("local save path:" + localSavePath);
            IBucket workBucket;
            try
            { 
                if (string.IsNullOrEmpty(BucketPassword))
                {
                    workBucket = cbc.OpenBucket(Bucket);
                }
                else
                {
                    workBucket = cbc.OpenBucket(Bucket,BucketPassword);
                } 
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, ex.Message,ErrorCategory.ConnectionError, Bucket));
                return;
            }

            var query = workBucket.CreateQuery(DesignDoc, View);
            
            var results = workBucket.Query<dynamic>(query);

            var  keys = results.Rows.Select(x => x.Id);

            WriteObject("Fetching Data");
            var multiget = workBucket.Get<dynamic>(keys.ToList(), new ParallelOptions
            {
                MaxDegreeOfParallelism = 20
            },
            20);

            WriteObject("Done fetching");

            WriteObject($"Writing Files to: {localSavePath}");

            foreach (var operationResult in multiget)
            {
                try
                {
                    var doc = operationResult.Value.Value;

                    string serializedValues = JsonConvert.SerializeObject(doc, Formatting.Indented);

                    string filename = operationResult.Key.ToString();

                    foreach (var c in Path.GetInvalidFileNameChars())
                    {
                        filename = filename.Replace(c.ToString(),string.Empty);
                    }

                    File.WriteAllText(localSavePath + $"\\{filename}.json", serializedValues);

                    WriteObject($"saved {operationResult.Key}");
                }
                catch (Exception ex)
                {
                    WriteError(new ErrorRecord(ex, ex.Message, ErrorCategory.InvalidData, operationResult));
                    
                }
            }
             
            WriteObject("Success!");
            
            workBucket.Dispose();
        }
    }
}
