using MathAnalysisAI.Data;
using MathAnalysisAI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MathAnalysisAI.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionController : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("文件为空");

            // 1️⃣ 保存图片
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var filePath = Path.Combine(uploadsPath, file.FileName);
            var relativePath = "/uploads/" + file.FileName;

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 2️⃣ 调用 Pix2Text
            var p2tPath = @"C:\Users\zhoux\AppData\Roaming\Python\Python314\Scripts\p2t.exe";

            var psi = new ProcessStartInfo
            {
                FileName = p2tPath,
                Arguments = $"predict -i \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };


            var process = Process.Start(psi);

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            // ⭐ 清理日志（关键新增）
            output = CleanText(output);
            error = CleanText(error);

            // 合并
            string combined = output + "\n" + error;
            // 提取
            string ocrText = NormalizeLatex(ExtractOcrResult(combined));
            // 保存到数据库
            var question = new WrongQuestion
            {
                ContentHtml = ocrText,
                ErrorCategory = "",   // 先留空（后面AI分析用）
                AIDiagnosis = "",
                ImagePath = filePath
            };

            _db.WrongQuestions.Add(question);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = question.Id,
                filePath = relativePath,
                ocrResult = ocrText
            });
        }

        // 提取 OCR 结果
        private string ExtractOcrResult(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var startIndex = text.IndexOf("Outs:");
            if (startIndex == -1)
                return text.Trim(); // ⭐ 如果没找到，直接返回全部（关键）

            var resultPart = text.Substring(startIndex + "Outs:".Length).Trim();

            var endIndex = resultPart.IndexOf("[INFO");
            if (endIndex != -1)
                resultPart = resultPart.Substring(0, endIndex);

            return resultPart.Trim();
        }

        // 清理 ANSI 颜色字符
        private string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "";

            return Regex.Replace(input, @"\x1B\[[0-9;]*[mK]", "");
        }



       private string NormalizeLatex(string text)
{
    if (string.IsNullOrEmpty(text))
        return "";

    text = text.Replace("\\\\", "\\");
    text = text.Replace("\\;", " ");
    text = text.Replace("~", " ");
    text = Regex.Replace(text, @"\s+", " ");

    return text.Trim(); // ❗ 不要再加 $$
}


        private readonly ApplicationDbContext _db;

        public QuestionController(ApplicationDbContext db)
        {
            _db = db;
        }
        // 2️⃣ 获取错题列表
        [HttpGet("list")]
        public IActionResult GetList()
        {
            var list = _db.WrongQuestions
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.ContentHtml,
                    x.ImagePath,
                    x.CreatedAt
                })
                .ToList();

            return Ok(list);
        }
    }
}