using Microsoft.AspNetCore.Mvc;
using Public_Transport.Services.IServices;

namespace Public_Transport.Controllers.Admin
{
    [Route("/admin/upload")]    
    public class UploadController : Controller
    {
        private readonly IUploadService _uploadService;

        public UploadController(IUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage([FromForm] List<IFormFile> files)
        {
            if (files == null || !files.Any())
                return BadRequest("File không hợp lệ");

            var urls = new List<string>();
            foreach (var file in files)
            {
                var url = await _uploadService.UploadImageAsync(file);
                urls.Add(url);
            }

            return Ok(new { urls });
        }
    }
}
