using FileUpload.Entities;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace FileUpload.Helper
{
    public class StorageService : IStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly FirebaseConfig _config;

        public StorageService (IWebHostEnvironment env, IOptions<FirebaseConfig> config)
        {
            _env = env;
            _config = config.Value;
        }

        public async Task<StorageClient> GetClientAsync()
        {
            string credentialPath = Path.Combine(_env.ContentRootPath, "Firebase", _config.ServiceAccountPath);
            var credential = GoogleCredential.FromFile(credentialPath);
            return await StorageClient.CreateAsync(credential);
        }
    }
}
