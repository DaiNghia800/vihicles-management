namespace Public_Transport.Services.IServices
{
    public interface IUploadService
    {
        Task<string> UploadImageAsync(IFormFile file);
    }
}
