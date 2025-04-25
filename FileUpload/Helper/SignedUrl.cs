using FileUpload.Entities;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Options;

namespace FileUpload.Helper
{
    public class SignedUrl : ISignedUrl
    {
        private readonly FirebaseConfig _config;
        private readonly UrlSigner _signer;
        private readonly IWebHostEnvironment _env;

        public SignedUrl(IOptions<FirebaseConfig> config, IWebHostEnvironment env)
        {
            _config = config.Value;
            _env = env;
            var configPath = Path.Combine(_env.ContentRootPath, "Firebase", _config.ServiceAccountPath);
            _signer = UrlSigner.FromCredentialFile(configPath);
        }
        public string CreateSignedUrl(string firebasePath, int expiresInMinute)
        {
            return _signer.Sign(
                _config.BucketName,
                firebasePath,
                TimeSpan.FromMinutes(expiresInMinute),
                HttpMethod.Get
            );
        }
    }
}
