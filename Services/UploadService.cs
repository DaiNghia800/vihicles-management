using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using Public_Transport.Models.Entities;
using Public_Transport.Services.IServices;

namespace Public_Transport.Services
{
    public class UploadService : IUploadService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<UploadService> _logger;

        public UploadService(IOptions<CloudinarySettings> config, ILogger<UploadService> logger)
        {
            var settings = config.Value;
            var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "fastkart/products"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            return result.SecureUrl.ToString();
        }

        // ✅ FIX: Implement UploadSingleImageAsync cho avatar/profile images
        public async Task<string> UploadSingleImageAsync(IFormFile profileImageFile)
        {
            if (profileImageFile == null || profileImageFile.Length == 0)
            {
                _logger.LogWarning("UploadSingleImageAsync called with null or empty file");
                throw new ArgumentException("Profile image file is required", nameof(profileImageFile));
            }

            try
            {
                _logger.LogInformation("Uploading profile image: {FileName}, Size: {Size} bytes",
                    profileImageFile.FileName, profileImageFile.Length);

                using var stream = profileImageFile.OpenReadStream();

                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(profileImageFile.FileName, stream),
                    Folder = "public-transport/avatars", // ✅ Folder riêng cho avatars
                    Transformation = new Transformation()
                        .Width(500)
                        .Height(500)
                        .Crop("fill")
                        .Gravity("face") // Auto-crop focus on face
                };

                var result = await _cloudinary.UploadAsync(uploadParams);

                if (result.Error != null)
                {
                    _logger.LogError("Cloudinary upload error: {Error}", result.Error.Message);
                    throw new Exception($"Cloudinary upload failed: {result.Error.Message}");
                }

                _logger.LogInformation("Profile image uploaded successfully: {Url}", result.SecureUrl);
                return result.SecureUrl.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile image: {FileName}", profileImageFile.FileName);
                throw;
            }
        }
    }
}
