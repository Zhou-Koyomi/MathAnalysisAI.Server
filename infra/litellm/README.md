# LiteLLM Local Proxy

本目录用于本地启动 LiteLLM Proxy，并通过稳定 alias 转发到底层模型供应商。

## 目录结构

- `config.yaml`: LiteLLM 模型别名与路由配置
- `.env.example`: 环境变量示例（不含真实密钥）
- `docker-compose.litellm.yml`: Docker 启动方式

## AI Provider 接入边界

- ASP.NET 后端只持有 LiteLLM key（如 `LiteLLM__ApiKey`），不直接持有上游 provider key。
- 前端永远不接触任何模型 key。
- 上游 key（DashScope/SiliconFlow/OpenRouter/OpenAI/Gemini 等）仅放 LiteLLM 环境变量或代理层。
- 当前不支持用户自带 key（BYOK）。

## 稳定 alias（后端固定调用）

- `math-reviewer`
- `math-solver`
- `math-hint`
- `math-explainer`
- `photo-solution-ocr`
- `math-material-ocr`（预留）

说明：后端只调用 alias；切换上游 provider 只改 LiteLLM 配置，不改 ASP.NET 代码。
- 文本分析 alias（`math-reviewer/math-solver/math-hint/math-explainer`）固定走 DeepSeek（`deepseek/deepseek-chat`）。

## 国内账号推荐路线（视觉 OCR）

近期主线优先国内可用 Provider：

1. DashScope / 阿里云百炼 Qwen-VL
2. SiliconFlow Qwen2.5-VL 或兼容视觉模型
3. 火山方舟 / 豆包视觉模型
4. 腾讯云混元视觉
5. OpenRouter（备用）

原因：OpenRouter/GPT/Gemini 的账号与支付可用性在国内可能受限，先走国内 provider 更稳。

## 拍照解答 OCR（DashScope 主线）

- `photo-solution-ocr` alias 当前主线指向 DashScope OpenAI-compatible：
  - `api_base`: `https://dashscope.aliyuncs.com/compatible-mode/v1`
  - model 示例：`openai/qwen-vl-plus`
- 该链路已完成本地真实联调（`/api/photo-solutions/ocr`）。
- 当前实测可返回：`problemText` / `studentSolutionText` / `formulas`（`warnings` 允许 `section_split_uncertain`）。
- 请以百炼控制台实际可用模型为准（如 `qwen-vl-plus` / `qwen-vl-max`）。
- `deepseek-chat` 属于文本模型，不能用于 `photo-solution-ocr` 视觉输入。

## Provider Profile 说明

建议按 profile 管理（可映射到一个或多个 alias）：

- `text-deepseek`
- `vision-domestic-compatible`（主线）
- `vision-openrouter`（backup）
- `vision-official-openai`
- `vision-official-gemini`
- `proxy-cloudflare`

## 国内网络策略

1. 先走：`LiteLLM -> 国内视觉 Provider`
2. 如需跨境备用：`LiteLLM -> OpenRouter`
3. OpenRouter 不稳定时：`LiteLLM -> Cloudflare Worker -> OpenRouter`
4. 不要过早把 Cloudflare 放进主链路

## 环境变量

复制 `.env.example` 为 `.env` 并填入真实值（仅本地使用）：

- `LITELLM_MASTER_KEY`
- `DEEPSEEK_API_KEY`
- `QWEN_API_KEY`
- `DASHSCOPE_API_KEY`
- `SILICONFLOW_API_KEY`
- `OPENROUTER_API_KEY`（backup）
- `OPENAI_API_KEY`（可选）
- `GEMINI_API_KEY`（可选）
- `AI_PROXY_BASE_URL`（可选）
- `AI_PROXY_API_KEY`（可选）

## 启动方式 A：Python 本地运行

```bash
pip install litellm
cd infra/litellm
export LITELLM_MASTER_KEY="sk-local-litellm-master-key"
export DEEPSEEK_API_KEY="你的真实 DeepSeek Key"
export DASHSCOPE_API_KEY="你的真实 DashScope Key"
# 或 export SILICONFLOW_API_KEY="你的真实 SiliconFlow Key"
litellm --config config.yaml --port 4000
```

说明：
- 文本分析 alias（`math-reviewer/math-solver/math-hint/math-explainer`）只依赖 `DEEPSEEK_API_KEY`。
- `photo-solution-ocr` 视觉 OCR alias 只依赖 `DASHSCOPE_API_KEY`（或切换到其他视觉 provider 时对应的 key）。
- ASP.NET 后端不直接持有 `DEEPSEEK_API_KEY` / `DASHSCOPE_API_KEY`。

后端（ASP.NET）只需要：
```bash
export LLMGateway__Mode="litellm"
export LiteLLM__ApiKey="sk-local-litellm-master-key"
```

拍照 OCR 只测 alias：
```bash
curl -X POST http://localhost:4000/v1/chat/completions \
  -H "Authorization: Bearer sk-local-litellm-master-key" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "photo-solution-ocr",
    "messages": [
      {
        "role": "user",
        "content": [
          { "type": "text", "text": "请识别图像中的题目与解答，输出简短 JSON。" }
        ]
      }
    ]
  }'
```

## 启动方式 B：Docker

```bash
cd infra/litellm
cp .env.example .env
# 编辑 .env，填入真实 key

docker compose -f docker-compose.litellm.yml up -d
```

如果 LiteLLM 启动后仍表现得像在使用占位值：
- 检查 `infra/litellm/.env.local`、`.env` 或当前 shell 环境里是否仍是 `placeholder`；
- 修改 `DEEPSEEK_API_KEY` / `DASHSCOPE_API_KEY` 后，必须重启 LiteLLM 进程或容器；
- 若服务部署在 compose/服务器上，检查 `/etc/mathanalysis-ai/litellm.env` 是否已替换真实值。

## 测试建议

- 先做单张图片 OCR（`photo-solution-ocr`）联调。
- 稳定后再评估扫描 PDF 的 OCR 批处理。

## 安全说明

- 不要把真实 key 写入仓库。
- `.env` 仅本地使用，不提交。
- key 不进入前端代码。
- 日志中不要输出 `Authorization` 或完整 header。
- DeepSeek / DashScope key 仅注入 LiteLLM 进程环境；ASP.NET 不直接持有上游 provider key。

## 常见鉴权错误

- `DeepseekException - Authentication Fails`（`code=401`）：
  - 检查 `DEEPSEEK_API_KEY` 是否有效；
  - 检查该环境变量是否在 LiteLLM 启动终端中生效；
  - 变更 key 后必须重启 LiteLLM；
  - 若 LiteLLM 仍读取到 `placeholder`，检查 `infra/litellm/.env.local`、`.env` 或 `/etc/mathanalysis-ai/litellm.env`；
  - 报错含 `Received Model Group=math-reviewer` 时，表示 alias 路由正常，失败在上游 DeepSeek 鉴权。

## compose 环境文本分析联调结果

- `math-reviewer` 已在 compose 环境下通过 LiteLLM -> DeepSeek 跑通。
- 典型闭环顺序：
  - `docker compose up -d`
  - 首次新 SQL volume 执行 `dotnet ef database update --connection ...`
  - `test_student` 登录获取 session
  - 直测 `POST http://localhost:4000/v1/chat/completions` 且 `model=math-reviewer` 返回 `200`
  - 调用 `/api/learning-analysis/analyze` 返回结构化分析结果
- 注意：
  - LiteLLM 自己直测成功，不代表 ASP.NET 一定成功；
  - ASP.NET 侧还要求 `LiteLLM__BaseUrl` 指向完整 endpoint（例如 `http://litellm:4000/v1/chat/completions`）。

## 参考

- [LiteLLM Docs](https://docs.litellm.ai/)
- [LiteLLM DeepSeek Provider](https://docs.litellm.ai/docs/providers/deepseek)
