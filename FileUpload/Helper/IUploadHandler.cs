namespace FileUpload.Helper
{
    public interface IUploadHandler
    {
        public string Upload(IFormFile file);
    }
}
