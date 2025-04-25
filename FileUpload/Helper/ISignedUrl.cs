namespace FileUpload.Helper
{
    public interface ISignedUrl
    {
        public string CreateSignedUrl(string firebasePath, int expiresInMinute);
    }
}
