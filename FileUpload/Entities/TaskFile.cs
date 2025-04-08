namespace FileUpload.Entities
{
    public class TaskFile
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FirebasePath { get; set; }
        public string DownloadUrl { get; set; }
    }
}
