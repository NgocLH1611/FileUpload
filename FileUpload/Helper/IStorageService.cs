using Google.Cloud.Storage.V1;

namespace FileUpload.Helper
{
    public interface IStorageService
    {
        Task<StorageClient> GetClientAsync();
    }
}
