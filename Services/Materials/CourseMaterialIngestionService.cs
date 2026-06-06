using MathAnalysisAI.Server.Data;
using MathAnalysisAI.Server.DTOs.CourseMaterials;
using MathAnalysisAI.Server.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace MathAnalysisAI.Server.Services.Materials
{
    public class CourseMaterialIngestionService
    {
        private const long MaxFileSizeBytes = 100L * 1024L * 1024L;
        private const int MinExtractedTextLength = 100;

        private readonly ApplicationDbContext _db;
        private readonly CourseMaterialStorageService _storageService;
        private readonly PdfTextExtractionService _pdfTextExtractionService;
        private readonly MaterialChunkingService _materialChunkingService;
        private readonly ILogger<CourseMaterialIngestionService> _logger;

        public CourseMaterialIngestionService(
            ApplicationDbContext db,
            CourseMaterialStorageService storageService,
            PdfTextExtractionService pdfTextExtractionService,
            MaterialChunkingService materialChunkingService,
            ILogger<CourseMaterialIngestionService> logger)
        {
            _db = db;
            _storageService = storageService;
            _pdfTextExtractionService = pdfTextExtractionService;
            _materialChunkingService = materialChunkingService;
            _logger = logger;
        }

        public async Task<CourseMaterialUploadResponseDto> IngestPdfAsync(
            int courseId,
            int? chapterId,
            string? title,
            string? materialKind,
            string? visibility,
            IFormFile file,
            int? uploadedByUserId = null,
            CancellationToken cancellationToken = default)
        {
            if (courseId <= 0)
            {
                throw new ArgumentException("courseId is required.");
            }

            if (file == null || file.Length <= 0)
            {
                throw new ArgumentException("file is required.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new ArgumentException("file size exceeds 100MB limit.");
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".pdf")
            {
                throw new ArgumentException("当前阶段仅支持 PDF 上传。");
            }

            var courseExists = await _db.Courses
                .AsNoTracking()
                .AnyAsync(x => x.Id == courseId, cancellationToken);
            if (!courseExists)
            {
                throw new ArgumentException("courseId not found.");
            }

            if (chapterId.HasValue)
            {
                var chapterExists = await _db.Chapters
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == chapterId.Value && x.CourseId == courseId, cancellationToken);
                if (!chapterExists)
                {
                    throw new ArgumentException("chapterId not found in this course.");
                }
            }

            var stored = await _storageService.SavePdfAsync(file, cancellationToken);

            var duplicate = await _db.CourseMaterials
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.CourseId == courseId && x.FileHash != null && x.FileHash == stored.FileHash,
                    cancellationToken);

            if (duplicate != null)
            {
                return new CourseMaterialUploadResponseDto
                {
                    MaterialId = duplicate.Id,
                    Title = duplicate.Title,
                    OriginalFileName = duplicate.OriginalFileName,
                    ParseStatus = duplicate.ParseStatus,
                    ParseMessage = "duplicate file detected",
                    ChunkCount = await _db.MaterialChunks.CountAsync(x => x.CourseMaterialId == duplicate.Id, cancellationToken)
                };
            }

            var material = new CourseMaterial
            {
                CourseId = courseId,
                Title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(file.FileName) : title.Trim(),
                MaterialKind = NormalizeMaterialKind(materialKind),
                Language = "zh-CN",
                Author = null,
                Edition = null,
                Publisher = null,
                Visibility = NormalizeVisibility(visibility),
                CopyrightNote = null,
                OriginalFileName = Path.GetFileName(file.FileName),
                FileExtension = ext,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                FileHash = stored.FileHash,
                StoragePath = stored.RelativePath,
                ParseStatus = "processing",
                ParseMessage = null,
                UploadedByUserId = uploadedByUserId,
                UploadedAt = DateTime.UtcNow,
                ParsedAt = null
            };

            _db.CourseMaterials.Add(material);
            await _db.SaveChangesAsync(cancellationToken);

            var response = new CourseMaterialUploadResponseDto
            {
                MaterialId = material.Id,
                Title = material.Title,
                OriginalFileName = material.OriginalFileName,
                ParseStatus = material.ParseStatus,
                ParseMessage = material.ParseMessage,
                ChunkCount = 0
            };

            try
            {
                var extraction = await _pdfTextExtractionService.ExtractAsync(stored.AbsolutePath, cancellationToken);
                if (extraction.TotalTextLength < MinExtractedTextLength)
                {
                    material.ParseStatus = "ocr_pending";
                    material.ParseMessage = "PDF text extraction returned too little text. OCR is required but not implemented in this stage.";
                    material.ParsedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(cancellationToken);

                    response.ParseStatus = material.ParseStatus;
                    response.ParseMessage = material.ParseMessage;
                    return response;
                }

                var chunks = _materialChunkingService.BuildChunks(
                    material.Id,
                    courseId,
                    chapterId,
                    extraction.Pages);

                if (chunks.Count > 0)
                {
                    _db.MaterialChunks.AddRange(chunks);
                }

                material.ParseStatus = "success";
                material.ParseMessage = chunks.Count == 0 ? "No chunks generated from extracted text." : null;
                material.ParsedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);

                response.ParseStatus = material.ParseStatus;
                response.ParseMessage = material.ParseMessage;
                response.ChunkCount = chunks.Count;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Course material PDF ingestion failed. MaterialId={MaterialId}", material.Id);
                material.ParseStatus = "failed";
                material.ParseMessage = BuildShortError(ex.Message);
                material.ParsedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);

                response.ParseStatus = material.ParseStatus;
                response.ParseMessage = material.ParseMessage;
                return response;
            }
        }

        private static string NormalizeMaterialKind(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "textbook";
            }

            var lower = value.Trim().ToLowerInvariant();
            return lower switch
            {
                "textbook" or "lecture_note" or "exercise_book" or "handout" or "user_note" or "other" => lower,
                _ => "other"
            };
        }

        private static string NormalizeVisibility(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "course_internal";
            }

            var lower = value.Trim().ToLowerInvariant();
            return lower is "private" or "course_internal" ? lower : "course_internal";
        }

        private static string BuildShortError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Material ingestion failed.";
            }

            var trimmed = message.Trim();
            return trimmed.Length <= 300 ? trimmed : trimmed[..300];
        }
    }
}
