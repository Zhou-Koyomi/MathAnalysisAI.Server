using MathAnalysisAI.Data;
using MathAnalysisAI.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MathAnalysisAI.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuestionController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _config;

        private const string P2T_PATH =
            @"C:\Users\zhoux\AppData\Roaming\Python\Python314\Scripts\p2t.exe";

        public QuestionController(ApplicationDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        //---------------------------------------------------
        // 1. 上传图片 + OCR识别
        //---------------------------------------------------
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("文件为空");

            try
            {
                //-----------------------------------------
                // 保存图片
                //-----------------------------------------
                var uploadsPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads"
                );

                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                var fileName = Guid.NewGuid().ToString()
                               + Path.GetExtension(file.FileName);

                var filePath = Path.Combine(uploadsPath, fileName);

                var relativePath = "/uploads/" + fileName;

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                //-----------------------------------------
                // 调用 Pix2Text OCR
                //-----------------------------------------
                string rawLatex = await RunPix2Text(filePath);

                string cleanLatex = NormalizeLatex(rawLatex);

                //-----------------------------------------
                // 存数据库
                //-----------------------------------------
                var question = new WrongQuestion
                {
                    ImagePath = relativePath,

                    RawLatex = rawLatex,
                    CleanLatex = cleanLatex,

                    OverallEvaluation = "",
                    StudentAnswer = "",
                    ErrorAnalysis = "",
                    StandardSolution = "",
                    ImprovementSuggestion = "",
                    KnowledgePoint = "",

                    CreatedAt = DateTime.Now
                };

                _db.WrongQuestions.Add(question);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    question.Id,
                    question.ImagePath,
                    question.RawLatex,
                    question.CleanLatex
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        //---------------------------------------------------
        // 2. AI分析
        //---------------------------------------------------
        [HttpPost("analyze/{id}")]
        public async Task<IActionResult> Analyze(int id)
        {
            var question = await _db.WrongQuestions.FindAsync(id);

            if (question == null)
                return NotFound("题目不存在");

            try
            {
                string apiKey = _config["DeepSeek:ApiKey"];

                using var client = new HttpClient();

                client.DefaultRequestHeaders.Add(
                    "Authorization",
                    $"Bearer {apiKey}"
                );

                var prompt = $@"
你是数学AI错题老师。

请分析这道题：

{question.CleanLatex}

严格按照JSON返回：

{{
  ""overallEvaluation"": """",
  ""knowledgePoint"": """",
  ""studentAnswer"": """",
  ""errorAnalysis"": """",
  ""standardSolution"": """",
  ""improvementSuggestion"": """"
}}
";

                var requestBody = new
                {
                    model = "deepseek-chat",
                    messages = new[]
                    {
                        new {
                            role="user",
                            content=prompt
                        }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);

                var response = await client.PostAsync(
                    "https://api.deepseek.com/chat/completions",
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"
                    )
                );

                var result = await response.Content.ReadAsStringAsync();

                //-----------------------------------
                // 暂时先直接存完整返回
                //-----------------------------------
                question.OverallEvaluation = result;

                await _db.SaveChangesAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        //---------------------------------------------------
        // 3. 获取全部错题
        //---------------------------------------------------
        [HttpGet("list")]
        public IActionResult GetList()
        {
            var list = _db.WrongQuestions
                .OrderByDescending(x => x.Id)
                .ToList();

            return Ok(list);
        }

        //---------------------------------------------------
        // OCR方法
        //---------------------------------------------------
        private async Task<string> RunPix2Text(string filePath)
        {
            var psi = new ProcessStartInfo
            {
                FileName = P2T_PATH,
                Arguments = $"predict -i \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            string output =
                await process.StandardOutput.ReadToEndAsync();

            string error =
                await process.StandardError.ReadToEndAsync();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.WriteLine(error);
            }

            return ExtractLatex(output);
        }

        //---------------------------------------------------
        // 提取latex
        //---------------------------------------------------
        private string ExtractLatex(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";

            raw = Regex.Replace(
                raw,
                @"\x1B\[[0-9;]*[mK]",
                ""
            );

            var match = Regex.Match(
                raw,
                @"Outs:\s*(.*?)(?=\[INFO|$)",
                RegexOptions.Singleline
            );

            if (match.Success)
                return match.Groups[1].Value.Trim();

            return raw.Trim();
        }

        //---------------------------------------------------
        // 清洗latex
        //---------------------------------------------------
        private string NormalizeLatex(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            text = text.Replace("\\\\", "\\");
            text = text.Replace("$$", "");
            text = text.Replace("$", "");

            return text.Trim();
        }
    }
}