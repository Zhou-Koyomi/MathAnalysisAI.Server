using System.Text;
using System.Text.Json;

namespace MathAnalysisAI.Server.Services
{
    public class LLMService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public LLMService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task<string> AnalyzeMath(string questionText)
        {
            var apiKey = _config["DeepSeek:ApiKey"];
            var url = _config["DeepSeek:BaseUrl"];

            var requestBody = new
            {
                model = "deepseek-chat",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = @"你是数学分析AI导师，请严格按照以下格式输出：

# 整体评价
# 存在的问题
# 修改建议
# 正确解法
# 知识点总结"
                    },
                    new
                    {
                        role = "user",
                        content = questionText
                    }
                },
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Add("Authorization", $"Bearer {apiKey}");

            request.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            var result = await response.Content.ReadAsStringAsync();

            return result;
        }
    }
}