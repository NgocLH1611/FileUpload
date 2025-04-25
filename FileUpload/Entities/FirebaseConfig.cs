namespace FileUpload.Entities
{
    public class FirebaseConfig
    {
        public string BucketName { get; set; }
        public string ServiceAccountPath { get; set; }
        public int ExpireTimer { get; set; }
    }
}
