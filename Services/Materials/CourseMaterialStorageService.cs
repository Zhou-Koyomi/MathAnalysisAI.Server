using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace MathAnalysisAI.Server.Services.Materials
{
    public class CourseMaterialStorageService
    {
        private readonly IWebHostEnvironment _env;

        public CourseMaterialStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<StoredMaterialFile> SavePdfAsync(IFormFile file, CancellationToken cancellationToken = default)
        {
            var uploadsRoot = Path.Combine(_env.ContentRootPath, "uploads", "course-materials");
            Directory.CreateDirectory(uploadsRoot);

            var safeName = SanitizeFileName(Path.GetFileNameWithoutExtension(file.FileName));
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var random = Guid.NewGuid().ToString("N")[..8];
            var storageFileName = $"{stamp}_{safeName}_{random}{ext}";
            var absolutePath = Path.Combine(uploadsRoot, storageFileName);

            await using (var stream = new FileStream(absolutePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            var fileHash = await ComputeSha256Async(absolutePath, cancellationToken);
            var relativePath = Path.Combine("uploads", "course-materials", storageFileName).Replace("\\", "/");

            return new StoredMaterialFile
            {
                AbsolutePath = absolutePath,
                RelativePath = relativePath,
                FileHash = fileHash
            };
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "material";
            }

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var ch in name.Trim())
            {
                if (invalid.Contains(ch))
                {
                    continue;
                }

                if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' || ch == '.')
                {
                    sb.Append(ch);
                }
                else
                {
                    sb.Append('_');
                }
            }

            var sanitized = sb.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(sanitized) ? "material" : sanitized;
        }

        private static async Task<string> ComputeSha256Async(string filePath, CancellationToken cancellationToken)
        {
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var sha = SHA256.Create();
            var hashBytes = await sha.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hashBytes);
        }
    }

    public class StoredMaterialFile
    {
        public string AbsolutePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty;
        public string FileHash { get; set; } = string.Empty;
    }
}
