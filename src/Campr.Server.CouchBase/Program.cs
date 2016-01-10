using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Couchbase;
using Couchbase.Configuration.Client;
using Newtonsoft.Json;

namespace Campr.Server.CouchBase
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var prog = new Program();
            prog.MainAsync().GetAwaiter().GetResult();
        }

        public Program()
        {
            this.serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
        }

        private readonly JsonSerializerSettings serializerSettings;

        private async Task MainAsync()
        {
            // Read the views and build the design documents.
            var designDocumentTasks = Directory.GetDirectories(Path.Combine("DesignDocuments", "Dev"))
                .Concat(Directory.GetDirectories(Path.Combine("DesignDocuments", "Prod")))
                .Select(this.ReadDesignDocumentAsync).ToList();
            await Task.WhenAll(designDocumentTasks);
            var designDocuments = designDocumentTasks.Select(t => t.Result).ToList();

            // Configuration our connection to the cluster.
            var cluster = new Cluster(new ClientConfiguration
            {
                Servers = { new Uri("http://localhost:8091") },
                BucketConfigs = { { "camprdb-dev", new BucketConfiguration { BucketName = "camprdb-dev" } } }
            });

            // Get an instance of the manager for the target bucket.
            var bucket = cluster.OpenBucket("camprdb-dev");
            var bucketManager = bucket.CreateManager("Administrator", "CbPass");

            // Create/Update the design documents.
            var upsertDesignDocumentTasks = designDocuments.Select(d => bucketManager.UpdateDesignDocumentAsync(d.Name, d.Json)).ToList();
            await Task.WhenAll(upsertDesignDocumentTasks);
        }

        private async Task<DesignDocument> ReadDesignDocumentAsync(string path)
        {
            // Read the views for this design document.
            var viewTasks = Directory.GetDirectories(path).Select(this.ReadDesignDocumentViewAsync).ToList();
            await Task.WhenAll(viewTasks);
            var views = viewTasks.Select(t => t.Result).ToList();

            // Check if this is a developement document or not.
            var isDev = Path.GetFileName(Path.GetDirectoryName(path)) == "Dev";

            // Return a design document object with the JSON version already serialized.
            return new DesignDocument
            {
                Name = (isDev ? "dev_" : "") + this.ToSnakeCase(Path.GetFileName(path)),
                Json = JsonConvert.SerializeObject(new
                {
                    views = views.ToDictionary(v => v.Name, v => new
                    {
                        map = v.Map,
                        reduce = v.Reduce  
                    })
                }, this.serializerSettings)
            };
        }

        private async Task<DesignDocumentView> ReadDesignDocumentViewAsync(string path)
        {
            return new DesignDocumentView
            {
                Name = this.ToSnakeCase(Path.GetFileName(path)),
                Map = await this.ReadFileAsync(Path.Combine(path, "Map.js")),
                Reduce = await this.ReadFileAsync(Path.Combine(path, "Reduce.js"))
            };
        }

        private string ToSnakeCase(string src)
        {
            return string.Concat(src.Select((x, i) => char.IsUpper(x) ? (i > 0 ? "_" : "") + x.ToString().ToLowerInvariant() : x.ToString()));
        }

        private async Task<string> ReadFileAsync(string path)
        {
            try
            {
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                using (var reader = new StreamReader(fileStream))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch
            {
                return null;
            }
        }

        private class DesignDocument
        {
            public string Name { get; set; }
            public string Json { get; set; }
        }

        private class DesignDocumentView
        {
            public string Name { get; set; }
            public string Map { get; set; }
            public string Reduce { get; set; }
        }
    }
}
