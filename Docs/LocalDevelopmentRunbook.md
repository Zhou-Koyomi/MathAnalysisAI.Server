# 本地开发运行手册（Local Development Runbook）

## 1. 说明
- 本文用于本地联调：SQL Server Docker + LiteLLM + ASP.NET Core 后端。
- 不要把任何真实 API Key 写入仓库文件。
- 推荐使用本地 secrets 文件（`.env.local`）+ 启动脚本，避免每次手动 `export`。
- 生产部署与自动重启方案见：`Docs/DeploymentRunbook.md`。
- 本地演示操作手册见：`Docs/DemoRunbook.md`。
- 演示冻结规则见：`Docs/DemoFreeze.md`。
- 全项目审计报告见：`Docs/ProjectAudit.md`。
- Legacy QuestionController 审计见：`Docs/LegacyQuestionControllerAudit.md`（R35-a）。
- 生产认证设计见：`Docs/AuthDesign.md`。
- 认证数据模型设计见：`Docs/AuthDataModelDesign.md`。
- SQL 恢复脚本安全设计见：`Docs/SqlRestoreScriptDesign.md`。
- SQL 手动备份与临时恢复手册见：`Docs/BackupRestoreRunbook.md`。
- 生产 compose 模板见：`docker-compose.prod.yml`（含 `restart: unless-stopped` 与 server 本地构建配置）。
- 已验证：`docker compose -f docker-compose.prod.yml build server` 可生成 `mathanalysis-server:latest`。
- 已验证：生产 compose 在本机可 `up -d` 正常启动，`/api/health` 返回 `ok`，`mathanalysis-server` health 为 `healthy`。
- 本地日常开发仍建议继续使用 `dotnet run` + 单独 SQL/LiteLLM，避免误动生产 compose 数据卷或占用固定端口。
- 本地演示 / 开发前如需做数据保护，可按 `Docs/BackupRestoreRunbook.md` 执行一次手动备份或临时恢复验证。

## 1.1 本地 secrets 文件方案（推荐）
- LiteLLM secrets：`infra/litellm/.env.local`
  - `LITELLM_MASTER_KEY`
  - `DEEPSEEK_API_KEY`
  - `DASHSCOPE_API_KEY`
- Server secrets：项目根目录 `.env.local`
  - `ConnectionStrings__DefaultConnection`
  - `LLMGateway__Mode=litellm`
  - `LiteLLM__BaseUrl`
  - `LiteLLM__ApiKey`
  - `Auth__Mode=DevelopmentUsername`

职责边界：
- `DEEPSEEK_API_KEY` 只提供给 LiteLLM 进程。
- `DASHSCOPE_API_KEY` 只提供给 LiteLLM 进程。
- ASP.NET 后端不直接持有 DeepSeek / DashScope key。
- ASP.NET 后端只持有 `LiteLLM__ApiKey`（以及 `LiteLLM__BaseUrl`）。

加载方式（脚本内部同样使用）：
```bash
set -a
source .env.local
set +a
```

可直接使用：
```bash
./scripts/start-litellm-local.sh
./scripts/start-server-local.sh
```

数据库手动备份：
```bash
MSSQL_SA_PASSWORD='YourStrongPassword@123' ./scripts/backup-sqlserver.sh
```

说明：
- 默认备份目录：`~/Backups/mathanalysis-ai`
- 可通过 `BACKUP_HOST_DIR` 指向其他非仓库目录
- 备份文件为 `.bak`，不会进入 Git

临时库恢复（phase 1）：
```bash
MSSQL_SA_PASSWORD='YourStrongPassword@123' \
BACKUP_FILE='/path/to/MathAnalysisAI_YYYYMMDD_HHMMSS.bak' \
./scripts/restore-sqlserver.sh
```

说明：
- 默认恢复到 `MathAnalysisAI_RestoreTest_YYYYMMDD_HHMMSS`
- 当前不支持覆盖主库
- 当前不会停止 `server`

临时库清理示例：
```bash
docker exec mathanalysis-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -C -S localhost -U sa -P 'YourStrongPassword@123' \
  -Q "DROP DATABASE [MathAnalysisAI_RestoreTest_YYYYMMDD_HHMMSS]"
```

## 1.2 AuthMode 说明
- 支持值：
  - `DevelopmentUsername`
  - `LocalPassword`
  - `Oidc`
  - `Disabled`
- 本地开发 / 演示推荐：
  - `Auth__Mode=DevelopmentUsername`
- Production 规则：
  - `DevelopmentUsername` 会被启动时 fail-fast 拒绝
  - `EnableDevelopmentFallback` / `EnableDevelopment*Override` 也必须为 `false`
- 当前状态：
  - 只有 `DevelopmentUsername` 具备现有登录链路
  - `LocalPassword` / `Oidc` 仅完成配置抽象，尚未实现

### 1.2.1 本地演示 compose 示例
若需要继续使用 compose 做本地演示，而不是 production-safe 部署，可使用“演示型 server.env”思路：

```bash
ASPNETCORE_ENVIRONMENT=Development
Auth__Mode=DevelopmentUsername
Auth__EnableDevelopmentFallback=true
Auth__DevelopmentFallbackUser=test_student
```

说明：
- 这类配置只适用于本地演示 / 开发；
- 不可直接复用到公网 Production。

## 2. 启动 SQL Server Docker（示例）
```bash
docker run -d \
  --name mathanalysis-sqlserver \
  -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrongPassword@123" \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest
```

检查容器状态：
```bash
docker ps | grep mathanalysis-sqlserver
```

## 3. 启动 LiteLLM（示例）
### 方式 A：Python 本地
```bash
cd infra/litellm
export DEEPSEEK_API_KEY="你的真实DeepSeekKey"
export DASHSCOPE_API_KEY="你的真实DashScopeKey"
export LITELLM_MASTER_KEY="sk-local-litellm-master-key"
litellm --config config.yaml --port 4000
```

### 方式 B：Docker Compose（若已提供）
```bash
cd infra/litellm
docker compose -f docker-compose.litellm.yml up -d
```

健康检查：
```bash
curl -I http://localhost:4000/v1/chat/completions
```

## 4. 启动后端（dotnet run）
进入项目目录：
```bash
cd MathAnalysisAI.Server
```

### direct 模式（直连 DeepSeek）
```bash
export LLMGateway__Mode="direct"
export DeepSeek__ApiKey="你的真实DeepSeekKey"
dotnet run
```

### litellm 模式（推荐）
```bash
export LLMGateway__Mode="litellm"
export LiteLLM__BaseUrl="http://localhost:4000/v1/chat/completions"
export LiteLLM__ApiKey="sk-local-litellm-master-key"
dotnet run
```

说明：
- 本地 `dotnet run` 场景：`LiteLLM__BaseUrl` 应为 `http://localhost:4000/v1/chat/completions`
- compose 场景：`LiteLLM__BaseUrl` 应为 `http://litellm:4000/v1/chat/completions`
- 文本分析 alias（`math-reviewer/math-solver/math-hint/math-explainer`）走 DeepSeek，依赖 `DEEPSEEK_API_KEY`。
- 若要真实测试“拍照解答 OCR”，优先配置国内视觉 provider（`DASHSCOPE_API_KEY` 或 `SILICONFLOW_API_KEY`）并确保 LiteLLM `photo-solution-ocr` alias 指向对应模型。
- `OPENROUTER_API_KEY` 保留为备用路线。
- DashScope 推荐 OpenAI-compatible base url：`https://dashscope.aliyuncs.com/compatible-mode/v1`。

## 5. direct 与 litellm 模式区别
- `direct`
  - 后端直接调用 DeepSeek。
  - Provider 记录为 `deepseek`。
- `litellm`
  - 后端调用 LiteLLM OpenAI-compatible 接口。
  - 按 `RequestType` 映射 model alias（如 `math-reviewer`）。
  - Provider 记录为 `litellm`。

## 6. 多页面访问说明
本地启动后访问：
- [http://localhost:5131/](http://localhost:5131/) 或 `/index.html`：学习分析首页
- [http://localhost:5131/materials.html](http://localhost:5131/materials.html)：课程资料管理
- [http://localhost:5131/stats.html](http://localhost:5131/stats.html)：学习统计与排行榜
- [http://localhost:5131/dev.html](http://localhost:5131/dev.html)：开发工具 / legacy OCR

## 7. 前端检查项
- 顶部导航 active 状态是否正确。
- 当前测试用户提示是否显示。
- `index.html` 不再显示完整排行榜和 legacy OCR。
- `materials.html` 可上传 PDF。
- `stats.html` 可加载排行榜。
- `dev.html` 可查看 legacy OCR。
- 浏览器 Console 不应有 JS 空节点错误。

## 7.1 本地 MVP 演示推荐路径（R27）
更完整的演示前检查、固定样例与现场话术见：`Docs/DemoRunbook.md`。
演示前变更限制、P0/P1/P2/P3 处理规则见：`Docs/DemoFreeze.md`。

建议按下面顺序演示当前版本：
1. 打开 `/login.html`，使用 `test_student` 登录。
2. 跳转 `/index.html`，确认顶部显示当前用户。
3. 上传作业图片，点击“识别题目与解答”。
4. 检查 OCR 回填的 `problemText` / `studentSolutionText` / `formulas[]`。
5. 在公式卡片中直接编辑 MathLive 公式，观察“当前 LaTeX”变化。
6. 选择“插入到题目末尾”或“插入到我的解答末尾”。
7. 手动点击“开始分析”，展示学习反馈报告。
8. 打开 `/stats.html`，查看公开排行榜是否更新。
9. 以 student 身份访问 `/materials.html` 与 `/dev.html`，确认页面显示无权访问或不触发受限管理流。

## 8. 调用分析接口（curl 示例）
```bash
curl -X POST http://localhost:5131/api/learning-analysis/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "courseId": 200,
    "chapterId": 307,
    "problemText": "判断反常积分 ∫_1^∞ 1/x^2 dx 是否收敛。",
    "studentSolutionText": "因为 1/x^2 趋于 0，所以积分收敛。",
    "analysisMode": "review_solution",
    "userId": 1
  }'
```

compose 环境登录 + analyze 验证：
```bash
curl -i -c /tmp/mathauth.cookie -X POST http://localhost:5131/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"test_student"}'
```

```bash
curl -b /tmp/mathauth.cookie http://localhost:5131/api/auth/me
```

```bash
curl -i -b /tmp/mathauth.cookie -X POST http://localhost:5131/api/learning-analysis/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "courseId": 200,
    "chapterId": 307,
    "problemText": "判断反常积分 ∫_1^∞ 1/x^2 dx 是否收敛。",
    "studentSolutionText": "因为 1/x^2 趋于 0，所以积分收敛。",
    "analysisMode": "review_solution",
    "userId": 1
  }'
```

compose 环境成功样例（已验证）：
- HTTP `200`
- `problemType=判断`
- `difficulty=简单`
- `knowledgePoints` 包含 `ma.improper_integral.convergence_criteria`
- `studentSolutionReview.isCorrect=false`
- `mainIssue` 指出“被积函数趋于0不是反常积分收敛充分条件”

## 9. 排行榜接口测试（curl 示例）
```bash
curl "http://localhost:5131/api/leaderboard/public?courseId=200&take=10"
```

## 10. PDF 课程资料上传测试（curl 示例）
```bash
curl -X POST http://localhost:5131/api/course-materials/upload \
  -F "courseId=200" \
  -F "chapterId=307" \
  -F "title=数学分析教材-反常积分" \
  -F "materialKind=textbook" \
  -F "visibility=course_internal" \
  -F "file=@/absolute/path/to/your-book.pdf;type=application/pdf"
```

扫描型 PDF 说明：
- 若文本提取结果过少，会返回 `parseStatus=ocr_pending`。
- 表示该 PDF 疑似扫描版，当前阶段需要后续 OCR 流程处理。

资料列表 curl 示例：
```bash
curl "http://localhost:5131/api/course-materials?courseId=200"
```

资料检索调试 API curl 示例：
```bash
curl "http://localhost:5131/api/course-materials/search?courseId=200&chapterId=307&q=反常积分%20收敛%20比较判别法&topK=3"
```

前端课程资料页测试：
- 打开 `http://localhost:5131/materials.html`
- 上传 PDF
- 查看“已上传资料”
- 在“资料检索调试”输入关键词并检索

说明：
- 如果资料是扫描版 PDF 且 `parseStatus=ocr_pending`，检索可能返回空数组。
- 当前检索只返回 `ContentPreview`，不返回教材全文。
- 当前检索尚未自动影响分析结果。

## 11. 拍照解答 OCR 状态与接口
- 拍照解答入口已存在（`index.html`）。
- `/api/photo-solutions/ocr` endpoint 已存在。
- 真实识别需要：
  - 国内 provider key（`DASHSCOPE_API_KEY` 或 `SILICONFLOW_API_KEY`）
  - LiteLLM `photo-solution-ocr` alias
  - （备用）`OPENROUTER_API_KEY`
- 未配置 key 时，前端会提示：
  - `视觉 OCR 服务暂未配置或调用失败，请手动输入题目和解答。`

拍照 OCR curl 示例：
```bash
curl -X POST http://localhost:5131/api/photo-solutions/ocr \
  -F "courseId=200" \
  -F "chapterId=307" \
  -F "userHint=题目在上半部分，解答在下半部分" \
  -F "file=@/absolute/path/to/solution.jpg;type=image/jpeg"
```

DashScope 联调建议（本地环境变量注入，不入库）：
```bash
cd infra/litellm
export DEEPSEEK_API_KEY="你的真实DeepSeekKey"
export DASHSCOPE_API_KEY="你的真实key"
export LITELLM_MASTER_KEY="sk-local-litellm-master-key"
litellm --config config.yaml --port 4000
```

```bash
cd MathAnalysisAI.Server
export LLMGateway__Mode="litellm"
export LiteLLM__BaseUrl="http://localhost:4000/v1/chat/completions"
export LiteLLM__ApiKey="sk-local-litellm-master-key"
dotnet run
```

DashScope 真实联调成功判定（已验证）：
- `problemText` 非空
- `studentSolutionText` 非空
- `formulas` 可选非空
- `warnings` 允许出现 `section_split_uncertain`
- `rawProvider=litellm`
- `modelName=photo-solution-ocr`

排查补充：
- `ocr_json_parse_failed`：
  - 检查模型返回是否为合法 JSON；
  - 检查 LaTeX 反斜杠转义（已支持非法反斜杠容错修复）。
- `section_split_uncertain`：
  - 表示题干/解答分区不确定，结果可继续人工检查再提交分析。
- `studentSolutionText=[unclear]`：
  - 常见于版面混杂、分区边界不明显，建议提升图片清晰度或补充 `userHint`。
- `model not found`：
  - 检查百炼控制台可用视觉模型名与 LiteLLM alias 配置。
- `401/403`：
  - 检查 `DASHSCOPE_API_KEY` 是否有效。
- `DeepseekException - Authentication Fails` / `code=401`：
  - 先确认 `DEEPSEEK_API_KEY` 有效；
  - 确认该变量已注入 LiteLLM 进程环境；
  - 修改环境变量后重启 LiteLLM；
  - 如果 LiteLLM 启动后仍使用 `placeholder`，检查 `infra/litellm/.env.local`、`.env` 或当前 shell 环境；
  - 若报错包含 `Received Model Group=math-reviewer`，说明后端 alias 路由正常，失败点在 DeepSeek 上游鉴权。
- `Method Not Allowed`：
  - 当前多见于 `LiteLLM__BaseUrl` 配成了根地址而不是完整 endpoint。
  - 正确值：
    - 本地：`http://localhost:4000/v1/chat/completions`
    - compose：`http://litellm:4000/v1/chat/completions`

## 12. 数据核对建议（只读）
- AppUserSeeder 是否写入 `test_student`
  - 查 `AppUsers`：`Username=test_student`
- LLMRequestLog 是否记录 LiteLLM
  - 查 `LLMRequestLogs`：`Provider=litellm`
- MistakeRecord 是否绑定知识点
  - 查 `MistakeRecords`：`KnowledgePointId` 非空
- UserCourseStats 是否更新
  - 查 `UserCourseStats`：`AttemptCount/CorrectCount/WrongCount`
- UserKnowledgeState 如何验证
  - 查 `UserKnowledgeStates`：按 `UserId + KnowledgePointId` 是否新增/更新

## 13. R19 开发期登录测试
- 访问：`/login.html`
- 输入 `test_student`，或点击“使用测试学生登录”。
- 登录成功后跳转首页，顶部应显示当前用户信息。
- 打开 `/api/auth/me`，应返回当前用户。
- 点击导航“退出”后，再访问 `/api/auth/me`：
  - 无 session 时应返回未登录（或开发 fallback 用户，取决于当前配置与环境）。

当前登录说明：
- 当前为开发期 username-only 登录。
- 不处理密码，不可视为生产级安全认证。
- Development fallback `test_student` 仍可用。

Auth curl 示例：
```bash
curl -X POST http://localhost:5131/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{ "username": "test_student" }'
```

```bash
curl http://localhost:5131/api/auth/me
```

```bash
curl -X POST http://localhost:5131/api/auth/logout
```

## 14. R19 权限测试（teacher/admin 与 legacy 限制）
- 登录 `test_student` 后（student）：
  - `materials.html` 不应显示课程资料管理区。
  - `GET /api/course-materials?courseId=200` 应返回 `403`。
  - `GET /api/Question/list` 应返回 `403`（legacy 默认受限）。
- admin 用户登录后：
  - `materials.html` 可访问。
  - `dev.html` 可访问。
  - legacy API（如 `/api/Question/list`）可访问。
- Development override 配置（默认都为 `false`）：
  - `Auth:EnableDevelopmentMaterialAccessOverride=false`
  - `Auth:EnableDevelopmentLegacyAccessOverride=false`
- analyze userId 收敛测试：
  - 登录 `test_student`，请求 `userId=1`：应成功。
  - 登录 `test_student`，请求不传 `userId`：应成功，后端自动补当前用户。
  - 登录 `test_student`，请求 `userId=999`：应返回 `403`。
  - 未登录且 fallback 关闭：应返回 `401`。
- 说明：
  - 前端 page guard 只是体验，后端 `401/403` 才是安全边界。
  - 前端 `userId` 只是兼容字段，后端 session 当前用户才是归属来源。
  - 当前 username-only 登录仍非生产认证。
  - 上线前必须关闭 Development fallback / override。

## 15. R16 拆分后回归检查建议
- 先执行：
```bash
dotnet build
```
- 再用 `review_solution` 示例请求回归：
  - `/api/learning-analysis/analyze`
- 重点检查：
  - `LLMRequestLog.AnalysisResultId` 是否正确关联到 `AnalysisResult.Id`
  - `MistakeRecord.KnowledgePointId` 是否已绑定真实知识点
  - `UserCourseStats` 是否按 `FinalCorrect > AiJudgedCorrect > null` 规则更新
  - `UserKnowledgeState` 是否按错误/正确分支规则更新
- 说明：
  - R16 拆分不改变 API 路径
  - 不需要 migration
  - 不改变 `RawResponseJson` 保存策略

## 16. R13-b 分析结果展示检查项（学生端）
- 提交 `review_solution` 示例后，结果区应显示判定状态：
  - `正确` / `需要修正` / `待判断`
- 结果区应展示：
  - `mainIssue`、`logicGaps`、`mistakeTags`
- 标准解答应以步骤卡片展示（步骤号/标题/内容）。
- 知识点应显示中文名，并弱化展示 code。
- `reviewSuggestions` 应展示为“下一步建议”（可与学生建议合并去重）。
- 不应在页面显示 `RawResponseJson`。
- 前端展示必须对 API 文本做 HTML escape。
- 不展示 API key / provider 信息。

## 17. R21 符号计算测试（dev/admin）
- Python 版本检查：
```bash
python3 --version
```
- SymPy 检查：
```bash
python3 -c "import sympy; print(sympy.__version__)"
```
- worker 命令行测试：
```bash
echo '{"operation":"limit","expression":"sin(x)/x","variable":"x","point":"0"}' | python3 Tools/Symbolic/symbolic_worker.py
```
- API curl 测试（`POST /api/symbolic/compute`）：
```bash
curl -X POST http://localhost:5131/api/symbolic/compute \
  -H "Content-Type: application/json" \
  -d '{
    "operation": "limit",
    "expression": "sin(x)/x",
    "variable": "x",
    "point": "0"
  }'
```
- 其他示例：
  - `integrate`: `1/x^2` from `1` to `oo`
  - `diff`: `sin(x^2)`
  - `solve`: `x^2 - 1 = 0`

权限说明：
- `/api/symbolic/compute` 默认仅 `admin` 或 Development override 可访问。
- `Auth:EnableDevelopmentSymbolicAccessOverride` 默认 `false`。
- 本地调试可临时打开，测试后应关闭。

错误排查：
- SymPy 未安装：
```bash
python3 -m pip install sympy
```
- `worker_unavailable`：检查 `PythonExecutable` / `WorkerPath`。
- `worker_invalid_response`：检查 worker stdout 是否只输出 JSON。
- `timeout`：降低表达式复杂度或调整 `Symbolic:TimeoutMs`。

安全说明：
- 不使用 `eval`。
- `operation` 白名单。
- 表达式长度限制。
- 超时 kill 子进程。
- 不返回 traceback/stderr。
- 不接学生主流程。

## 18. R22 AnalysisContext（retrieval）回归测试
R22 AnalysisContext 测试配置：
```json
"AnalysisContext": {
  "EnableKnowledgeRetrieval": false,
  "KnowledgeTopK": 3,
  "MaxKnowledgeContextChars": 1200,
  "MaxChunkPreviewChars": 400,
  "EnableSymbolicEvidence": false,
  "MaxSymbolicTasks": 2,
  "MaxSymbolicContextChars": 1000
}
```

1. 默认关闭：
  - 设置 `AnalysisContext:EnableKnowledgeRetrieval=false`
  - 调用 `/api/learning-analysis/analyze`
  - 预期：analyze 成功，prompt 不追加课程资料片段
2. 开启但无 chunk：
  - 设置 `AnalysisContext:EnableKnowledgeRetrieval=true`
  - 若 `CourseMaterials` 仅有 `ocr_pending` / `chunkCount=0`
  - 预期：analyze 成功，retrieval 返回空，prompt 不追加资料片段，不报 500
3. 开启且有 success chunk：
  - 前提：存在 `parseStatus=success` 且 `chunkCount>0` 的 CourseMaterial
  - 预期 prompt 追加 `[课程资料参考片段]`
  - 仅注入 `ContentPreview`
  - 不注入全文、`StoragePath`、`FileHash`
  - 本地可用 fake chunk 脚本补齐测试数据（见下方“附：开发期 fake chunk 数据”）
4. 权限与归属：
  - `userId mismatch` 仍应 `403`
  - retrieval context 不影响 `StudentSolution` / `UserCourseStats` / `UserKnowledgeState` 用户归属
5. 注意：
  - 当前 `LLMRequestLog` 不存完整 prompt，不能直接通过数据库查看 prompt 全文
  - 不建议将完整 prompt 入库
  - 若需观测，建议仅记录摘要：`contextInjected` / `knowledgeChunkCount` / `contextCharCount`

附：开发期 fake chunk 数据（用于“有 success chunk”场景）
- SQL 文件：`Docs/DevSeedSql/R22FakeChunk.sql`
- 执行（SQL Server Docker）：
```bash
docker exec -i mathanalysis-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -C -S localhost -U sa -P 'YourStrongPassword@123' -d MathAnalysisAI \
  < Docs/DevSeedSql/R22FakeChunk.sql
```
- 该脚本会：
  - 先清理同名测试资料（`开发测试资料-反常积分`）
  - 再插入 `CourseMaterial(parseStatus=success)` + 1 条 `MaterialChunk`
  - 若存在 `ma.improper_integral.comparison_test`，自动绑定到 `MaterialChunkKnowledgePoints`
- 清理：
  - 可直接重复执行同一脚本（脚本内已包含“先删后插”逻辑）
  - 或使用脚本底部提供的 cleanup SQL 手动删除

R22 fake chunk 验证命令（示例）：
```bash
curl "http://localhost:5133/api/course-materials/search?courseId=200&chapterId=307&q=%E5%8F%8D%E5%B8%B8%E7%A7%AF%E5%88%86%20%E6%AF%94%E8%BE%83%E5%88%A4%E5%88%AB%E6%B3%95%20%E6%94%B6%E6%95%9B&topK=3"
```

预期：
- 返回至少 1 条；
- `title=开发测试资料-反常积分`；
- `contentPreview` 包含“反常积分/比较判别法/p积分”；
- 仅返回 preview，不返回全文、`StoragePath`、`FileHash`。

在 `AnalysisContext__EnableKnowledgeRetrieval=true` 环境下验证 analyze（示例）：
```bash
curl -X POST http://localhost:5133/api/learning-analysis/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "courseId": 200,
    "chapterId": 307,
    "problemText": "判断反常积分 ∫_1^∞ 1/x^2 dx 是否收敛。",
    "studentSolutionText": "因为 1/x^2 趋于 0，所以积分收敛。",
    "analysisMode": "review_solution",
    "userId": 1
  }'
```

预期：
- analyze 不报 500；
- 若未配置 LLM key，可进入 `LLM failed / missing_api_key` 分支；
- retrieval 有数据场景不应破坏主流程。

## 19. 常见错误与排查
- `LocalDB is not supported on this platform`
  - 原因：macOS/Linux 不支持 LocalDB。
  - 处理：改用 SQL Server Docker 连接串。
- `localhost:4000 connect failed`
  - 原因：LiteLLM 未启动或端口未监听。
  - 处理：检查 LiteLLM 进程/容器与端口占用。
- `missing_litellm_api_key`
  - 原因：未设置 `LiteLLM__ApiKey`。
  - 处理：导出环境变量后重启后端。
- OCR 调用失败（photo-solution-ocr）
  - 原因：LiteLLM 未配置 vision alias，或未设置国内 provider key（`DASHSCOPE_API_KEY` / `SILICONFLOW_API_KEY`）；备用路线为 `OPENROUTER_API_KEY`。
  - 处理：检查 `infra/litellm/config.yaml` 与环境变量。
- API key header 非 ASCII 报错
  - 原因：key 含中文占位文本或非法字符。
  - 处理：确保 key 为 ASCII 合法字符串。
- SQL Server Docker 未启动
  - 原因：容器未运行或端口未映射。
  - 处理：`docker ps` / `docker logs` 检查容器状态。

## 20. 安全注意事项
- 不要将真实 `DeepSeek` / `OpenRouter` / `LiteLLM` key 写入仓库。
- 不要提交 `.env.local` / `*.env` 文件。
- `.env` 只本地使用。
- `dev.html` 属于开发工具页，正式环境应隐藏或加权限。
- 不要打印图片 base64。
- 不要将 provider raw response 原文直接返回前端。
- 课程资料接口不提供 PDF 下载。
- 课程资料检索不返回 `StoragePath` / `FileHash`。
- 课程资料检索不返回 `MaterialChunk.Content` 全文。
- 资料管理权限后续需接 teacher/admin。
- 生产环境必须关闭 Development fallback/override。

## 21. R23 MathLive Dev 验证
- 入口：`http://localhost:5131/dev.html`（admin 开发工具页）。
- 默认使用本地 vendor：`/vendor/mathlive/mathlive.min.js`。
- 验证项：
  - 看到“MathLive 公式编辑器试验”卡片；
  - 默认公式为 `\int_1^\infty \frac{1}{x^2}\,dx`；
  - 编辑时“当前 LaTeX”实时更新；
  - 示例按钮（极限/矩阵/反常积分）可切换；
  - 清空、复制 LaTeX 可用。
- 若显示“MathLive 未加载，请检查 /vendor/mathlive 文件是否存在。”：
  - 检查 `/wwwroot/vendor/mathlive/` 是否包含 `mathlive.min.js` 与 `fonts/` 资源；
  - 检查浏览器 Network 是否有 `/vendor/mathlive/*` 404。
  - CDN 仅作为临时开发备用，不作为默认加载源。

## 22. R23 OCR + MathLive 公式校对回归（index.html）
- 入口：`http://localhost:5131/index.html`
- 目标：验证拍照 OCR 回填 + `formulas[]` 公式校对 + 手动分析流程。

浏览器测试步骤：
1. 上传题目图片（jpg/jpeg/png/webp）。
2. 点击“识别题目与解答”。
3. 检查 `problemText` / `studentSolutionText` 是否回填。
4. 检查 OCR 校验提示是否出现。
5. 检查“识别出的公式”列表（`formulas[]`）。
6. 点击公式本身，直接在公式卡片内编辑。
7. 修改公式并观察“当前 LaTeX”实时变化。
8. 点击“复制 LaTeX”。
9. 点击“插入到题目末尾”与“插入到我的解答末尾”。
10. 确认 textarea 末尾追加 `$...$`。
11. 手动点击“开始分析”（OCR 成功后不会自动分析）。

成功判断：
- `problemText` 非空。
- `formulas[]` 可显示（若 OCR 未抽出独立公式，允许为空）。
- 每条公式默认以渲染后的 MathLive 公式卡片显示，而不是源码主视图。
- 点击公式可直接编辑，`当前 LaTeX` 会实时更新。
- 分析请求仅在手动点击“开始分析”后触发。

失败排查：
- MathLive 未加载：优先检查 `/vendor/mathlive/` 文件是否存在与资源路径是否 404。
- MathLive 未加载时，应回退显示 LaTeX 源码；复制/插入按钮仍应可用。
- `formulas=[]`：OCR 可能未提取独立公式，但题目/解答回填仍可用。
- `section_split_uncertain`：分区存在不确定性，需人工复核题干/解答。
- `studentSolutionText=[unclear]`：请手动补充“我的解答”后再分析。

## 23. R27 本地 MVP 演示总回归检查清单
- 基础服务：
  - `docker compose -f docker-compose.prod.yml ps`
  - `curl -i http://localhost:5131/api/health`
  - 预期：`sqlserver/litellm/server` Running，`/api/health` 返回 `200` + `status=ok`
- 登录：
  - `POST /api/auth/login`
  - `GET /api/auth/me`
  - 预期：`userId=1`、`role=student`
- OCR：
  - 使用本地测试图调用 `/api/photo-solutions/ocr`
  - 预期：`problemText` / `studentSolutionText` 非空；`formulas[]` 可选但当前样例已成功返回；`warnings` 允许 `section_split_uncertain`
- 公式校对：
  - 公式卡片主视图应为 inline MathLive，而不是纯源码主展示
  - 点击公式本身即可编辑
  - “当前 LaTeX”随输入实时变化
  - 插入规则仍为：空行 + `$<latex>$`
- 分析：
  - `POST /api/learning-analysis/analyze`
  - 预期不再出现：
    - `Not logged in`
    - `DeepseekException Authentication Fails`
    - `Method Not Allowed`
    - `missing_api_key`
- 学习统计：
  - `/stats.html` 或 `/api/leaderboard/public?courseId=200&take=10`
  - 预期：student 可访问，attempt/ranking 数据可见
- 权限：
  - student 调 `GET /api/course-materials?courseId=200` 返回 `403`
  - student 调 `GET /api/Question/list` 返回 `403`
  - `userId=999` 调 analyze 返回 `403 Forbidden userId mismatch.`
- 检索：
  - 若当前数据库未执行 fake chunk seed，则跳过“search 命中”验证，并记录为非阻断项
