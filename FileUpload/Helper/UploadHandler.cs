namespace FileUpload.Helper
{
    public class UploadHandler : IUploadHandler
    {
        public string Upload(IFormFile file)
        {
            List<string> validExtensions = new List<string>() { ".doc", ".pdf" };
            string extension = Path.GetExtension(file.FileName);
            if (!validExtensions.Contains(extension))
            {
                return $"Extension is not valid ({string.Join(", ", validExtensions)})";
            }

            long size = file.Length;
            if (size > (5 * 1024 * 1024))
                return "Maximum size allowed is 5MB";

            string fileName = Guid.NewGuid().ToString() + extension;
            string uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            string fullPath = Path.Combine(uploadFolder, fileName);
            using FileStream stream = new FileStream(fullPath, FileMode.Create);
            file.CopyTo(stream);

            return fileName;
        }
    }
}
