using FileUpload.Data;
using FileUpload.Entities;
using FileUpload.Helper;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Linq;

namespace FileUpload.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        private readonly FirebaseConfig _config;
        private readonly ISignedUrl _signedUrl;
        private readonly string bucketName;
        private readonly string credentialPath;
        private readonly StorageClient storageClient;
        private readonly GoogleCredential credential;

        public FileController(IWebHostEnvironment env, ApplicationDbContext context, IOptions<FirebaseConfig> config, ISignedUrl signedUrl)
        {
            _env = env;
            _context = context;
            _config = config.Value;
            _signedUrl = signedUrl;

            bucketName = _config.BucketName;
            credentialPath = Path.Combine(_env.ContentRootPath, "Firebase", _config.ServiceAccountPath);
            credential = GoogleCredential.FromFile(credentialPath);
            storageClient = StorageClient.Create(credential);
        }

        [HttpPost("{taskId}/upload")]
        public async Task<IActionResult> UploadFile(int taskId, IFormFile file)
        {
            List<string> validExtensions = new List<string>() { ".doc", ".pdf", ".jpg" };
            string extension = Path.GetExtension(file.FileName);
            if (!validExtensions.Contains(extension))
            {
                return BadRequest("The file format is invalid. The file format must be: " + string.Join(", ", validExtensions));
            }

            long size = file.Length;
            if (size > (5 * 1024 * 1024))
                return BadRequest("File too large. Max 5MB.");

            string fileName = Guid.NewGuid().ToString() + extension;
            string taskFolder = Path.Combine(_env.ContentRootPath, "Uploads", $"Task_{taskId}");

            if (!Directory.Exists(taskFolder))
                Directory.CreateDirectory(taskFolder);

            string localPath = Path.Combine(taskFolder, fileName);
            using (FileStream stream = new FileStream(localPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string firebasePath = $"tasks/{taskId}/{fileName}";

            using (var fileStream = new FileStream(localPath, FileMode.Open))
            {
                var uploadObject = storageClient.UploadObject(bucketName, firebasePath, file.ContentType, fileStream);
                fileStream.Close();
            }

            var taskFile = new TaskFile
            {
                TaskId = taskId,
                FileName = fileName,
                FilePath = localPath,
                FirebasePath = firebasePath
            };

            await _context.TaskFiles.AddAsync(taskFile);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                fileName,
                localPath,
                firebasePath = $"gs://{bucketName}/{firebasePath}"
            });
        }

        [HttpGet("{taskId}")]
        public async Task<IActionResult> GetFiles(int taskId)
        {
            string firebasePath = $"tasks/{taskId}/";

            var files = new List<string>();

            await foreach (var obj in storageClient.ListObjectsAsync(bucketName, firebasePath))
            {
                files.Add(Path.GetFileName(obj.Name));
            }

            return Ok(files);
        }

        [HttpGet("download/{taskId}/{fileName}")]
        public IActionResult GetDownloadUrl(int taskId, string fileName)
        {
            string firebasePath = $"tasks/{taskId}/{fileName}";
            var url = _signedUrl.CreateSignedUrl(firebasePath, _config.ExpireTimer);
            return Ok(new { url });
        }

        [HttpDelete("{taskId}/{fileName}")]
        public async Task<IActionResult> DeleteFile(int taskId, string fileName)
        {
            string firebasePath = $"tasks/{taskId}/{fileName}";

            try
            {
                await storageClient.DeleteObjectAsync(bucketName, firebasePath);
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("File not found in Firebase Storage");
            }

            return Ok($"File {fileName} was successfully delete from Firebase Storage!");
        }
    }
}
