using FileUpload.Helper;
using Microsoft.AspNetCore.Mvc;

namespace FileUpload.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]

    public class MainController : ControllerBase
    {
        private readonly IUploadHandler _uploadHandler;

        public MainController(IUploadHandler uploadHandler)
        {
            _uploadHandler = uploadHandler;
        }

        [HttpPost]
        public IActionResult UploadFile(IFormFile file)
        {
            return Ok(_uploadHandler.Upload(file));
        }
    }
}
