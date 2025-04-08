using FileUpload.Data;
using FileUpload.Entities;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FileUpload.Controllers
{
    [ApiController]
    [Route("api/files")]
    public class FileController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext _context;
        public FileController(IWebHostEnvironment env, ApplicationDbContext context)
        {
            _env = env;
            _context = context;
        }

        [HttpPost("{taskId}/upload")]
        public async Task<IActionResult> UploadFile(int taskId, IFormFile file)
        {
            List<string> validExtensions = new List<string>() { ".doc", ".pdf", ".jpg" };
            string extension = Path.GetExtension(file.FileName);
            if (!validExtensions.Contains(extension))
            {
                return BadRequest("The file format is invalid. The file format must be: " + validExtensions);
            }

            long size = file.Length;
            if (size > (5 * 1024 * 1024))
                return BadRequest();

            string fileName = Guid.NewGuid().ToString() + extension;
            string taskFolder = Path.Combine(_env.ContentRootPath, "Uploads", $"Task_{taskId}");

            if (!Directory.Exists(taskFolder))
            {
                Directory.CreateDirectory(taskFolder);
            }

            string fullPath = Path.Combine(taskFolder, fileName);
            using (FileStream stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string firebasePath = $"uploads/task_{taskId}/{fileName}";
            string bucketName = "fireupload-demo.appspot.com";
            string serviceAccountPath = Path.Combine(_env.ContentRootPath, "Firebase", "firebase-key.json");

            var credential = GoogleCredential
                .FromFile(serviceAccountPath)
                .CreateScoped("https://www.googleapis.com/auth/devstorage.full_control");

            string accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

            var escapedPath = Uri.EscapeDataString(firebasePath);
            var uploadUrl = $"https://storage.googleapis.com/upload/storage/v1/b/{bucketName}/o?uploadType=media&name={escapedPath}";

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var firebaseStream = new FileStream(fullPath, FileMode.Open);
            var content = new StreamContent(firebaseStream);
            content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);

            var response = await httpClient.PostAsync(uploadUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Failed to upload data to Firebase! {errorText}");
            }

            string downloadUrl = $"https://storage.googleapis.com/v0/b/{bucketName}/o/{Uri.EscapeDataString(firebasePath)}?alt=media";

            var taskFile = new TaskFile
            {
                TaskId = taskId,
                FileName = fileName,
                FilePath = fullPath,
                FirebasePath = firebasePath,
                DownloadUrl = downloadUrl
            };

            await _context.TaskFiles.AddAsync(taskFile);
            await _context.SaveChangesAsync();
            
            return Ok(new { fileName, fullPath, firebasePath, downloadUrl });
        }

        [HttpGet("{taskId}")]
        public IActionResult GetFiles(int taskId)
        {
            var files = _context.TaskFiles.Where(f => f.TaskId == taskId).Select(f => new { f.Id, f.FileName, f.FilePath }).ToList();

            if (!files.Any())
            {
                return NotFound("There is no files uploaded.");
            }

            return Ok(files);
        }

        [HttpGet("download/{taskId}/{fileid}")]
        public async Task<IActionResult> DownloadFile(int fileid, int taskId)
        {
            var taskFile = await _context.TaskFiles.FirstOrDefaultAsync(f => f.Id == fileid);

            if (taskFile == null)
            {
                return NotFound("File not found!");
            }

            var filePath = Path.Combine(_env.ContentRootPath, "Uploads", $"Task_{taskId}", taskFile.FileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File does not exist in the server!");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(fileBytes, "application/octet-stream", taskFile.FileName);
        }

        [HttpDelete("{fileid}")]
        public async Task<IActionResult> DeleteFile(int fileid, int taskid)
        {
            var file = await _context.TaskFiles.FirstOrDefaultAsync(f => f.Id == fileid);

            if (file == null)
            {
                return NotFound("File not found!");
            }

            var filePath = Path.Combine(_env.ContentRootPath, "Uploads", $"Task_{taskid}", file.FileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File does not exist in the server!");
            }
            else
            {
                System.IO.File.Delete(filePath);
            }

            _context.TaskFiles.Remove(file);
            await _context.SaveChangesAsync();

            return Ok($"Delete {file.FileName} successfully!");
        }
    }
}
