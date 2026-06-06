-- R22 fake chunk seed for retrieval-context local verification
-- Scope:
-- - CourseId = 200
-- - ChapterId = 307
-- - Material Title = N'开发测试资料-反常积分'
-- Notes:
-- - This is development-only SQL.
-- - Do NOT use as production seed.
-- - If you want to demo retrieval hits in the current compose database,
--   rerun this script before calling:
--   GET /api/course-materials/search?courseId=200&chapterId=307&q=反常积分 比较判别法 收敛&topK=3

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @CourseId INT = 200;
DECLARE @ChapterId INT = 307;
DECLARE @MaterialTitle NVARCHAR(128) = N'开发测试资料-反常积分';
DECLARE @MaterialId INT;
DECLARE @ChunkId INT;
DECLARE @ComparisonKpId INT;
DECLARE @ChunkCount INT = 0;

-- Optional cleanup of previous fake data (idempotent rerun)
SELECT @MaterialId = Id
FROM CourseMaterials
WHERE CourseId = @CourseId
  AND Title = @MaterialTitle;

IF @MaterialId IS NOT NULL
BEGIN
    DELETE mckp
    FROM MaterialChunkKnowledgePoints mckp
    INNER JOIN MaterialChunks mc ON mc.Id = mckp.MaterialChunkId
    WHERE mc.CourseMaterialId = @MaterialId;

    DELETE FROM MaterialChunks
    WHERE CourseMaterialId = @MaterialId;

    DELETE FROM CourseMaterials
    WHERE Id = @MaterialId;
END

-- Insert fake CourseMaterial
INSERT INTO CourseMaterials
(
    CourseId, Title, MaterialKind, Language, Visibility,
    OriginalFileName, FileExtension, ContentType, FileSizeBytes,
    FileHash, StoragePath, ParseStatus, ParseMessage, UploadedByUserId,
    UploadedAt, ParsedAt
)
VALUES
(
    @CourseId,
    @MaterialTitle,
    N'dev_test',
    N'zh-CN',
    N'course_internal',
    N'dev-test-improper-integral.txt',
    N'.txt',
    N'text/plain',
    128,
    N'dev-r22-fake-chunk-hash',
    N'uploads/dev/r22-fake-chunk.txt',
    N'success',
    N'dev fake chunk for retrieval context test',
    1,
    @Now,
    @Now
);

SET @MaterialId = SCOPE_IDENTITY();

-- Insert fake MaterialChunk
INSERT INTO MaterialChunks
(
    CourseMaterialId, CourseId, ChapterId, ChunkIndex, ChunkType,
    SemanticTitle, SectionTitle, SectionPath, PageStart, PageEnd,
    Content, ContentPreview, FormulaText, NormalizedFormulaText,
    TokenCountEstimate, StartOffset, EndOffset, DifficultyLevel,
    IsVerified, VerifiedByUserId, VerifiedAt, CreatedAt
)
VALUES
(
    @MaterialId,
    @CourseId,
    @ChapterId,
    1,
    N'method',
    N'反常积分比较判别法',
    N'反常积分判别法',
    N'数学分析/反常积分/比较判别法',
    1,
    1,
    N'反常积分收敛可通过比较判别法与 p 积分判别进行说明。当 p>1 时，∫_1^∞ 1/x^p dx 收敛。',
    N'反常积分 收敛 比较判别法 p积分 当 p>1 时收敛',
    NULL,
    NULL,
    30,
    NULL,
    NULL,
    N'medium',
    1,
    1,
    @Now,
    @Now
);

SET @ChunkId = SCOPE_IDENTITY();

-- Optional MCKP binding: ma.improper_integral.comparison_test
SELECT TOP 1 @ComparisonKpId = Id
FROM KnowledgePoints
WHERE CourseId = @CourseId
  AND Code = N'ma.improper_integral.comparison_test';

IF @ComparisonKpId IS NOT NULL
BEGIN
    INSERT INTO MaterialChunkKnowledgePoints
    (
        MaterialChunkId, KnowledgePointId, RelationType, IsPrimary,
        Confidence, Source, CreatedAt
    )
    VALUES
    (
        @ChunkId,
        @ComparisonKpId,
        N'primary',
        1,
        1.0000,
        N'dev_seed',
        @Now
    );
END

SELECT @ChunkCount = COUNT(1)
FROM MaterialChunks
WHERE CourseMaterialId = @MaterialId;

SELECT
    @MaterialId AS MaterialId,
    @ChunkId AS ChunkId,
    @ComparisonKpId AS BoundKnowledgePointId,
    @ChunkCount AS ChunkCount;

-- Cleanup SQL (run manually when needed):
-- DECLARE @CleanupMaterialId INT;
-- SELECT @CleanupMaterialId = Id
-- FROM CourseMaterials
-- WHERE CourseId = 200 AND Title = N'开发测试资料-反常积分';
--
-- IF @CleanupMaterialId IS NOT NULL
-- BEGIN
--     DELETE mckp
--     FROM MaterialChunkKnowledgePoints mckp
--     INNER JOIN MaterialChunks mc ON mc.Id = mckp.MaterialChunkId
--     WHERE mc.CourseMaterialId = @CleanupMaterialId;
--
--     DELETE FROM MaterialChunks WHERE CourseMaterialId = @CleanupMaterialId;
--     DELETE FROM CourseMaterials WHERE Id = @CleanupMaterialId;
-- END
