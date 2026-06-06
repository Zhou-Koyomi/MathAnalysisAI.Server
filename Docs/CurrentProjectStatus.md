# MathAnalysisAI.Server 当前项目状态

## 1. 项目目标
- 当前目标：构建“数学分析智能体”后端 MVP，支持题目文本 + 学生解答文本输入，输出结构化分析结果，并沉淀可用于统计分析的数据。

## 2. 已完成模块概览
- R1 安全配置
  - DeepSeek API Key 从明文改为环境变量注入。
  - BaseUrl 统一配置化。
- R2 平台实体与迁移
  - 已完成平台化实体建模与迁移基础。
  - 保留 legacy `WrongQuestion` 兼容表，不破坏旧链路。
- R3 数学分析 Seed
  - 已完成数学分析课程体系静态 seed（Subject/Course/Chapter/KnowledgePoint/Dependency）。
  - PromptProfile 采用运行时 seeder。
- R4 骨架层
  - DTO：分析请求/响应、可视化、LLM DTO。
  - LLMGateway：统一大模型网关调用与日志写入。
  - VisualizationService + GeoGebraCommandValidator：可视化命令校验与落库。
  - LeaderboardService + PermissionService：排行榜与权限骨架。
- R5 主分析接口
  - LearningAnalysisController 接入主分析流程入口。
- R8 前端最小入口
  - 已有最小文本分析入口与学生向界面改造。
- R10 LiteLLM 接入
  - 后端可通过配置切换 `direct` / `litellm` 模式。
  - LiteLLM alias（如 `math-reviewer`）已接入调用链路。
- R11 知识点归一化与错因绑定
  - KnowledgePointNormalizer 将模型标签归一化为数据库真实 `KnowledgePoint.Code`。
  - MistakeRecord 可绑定真实 `KnowledgePointId`。
- R12 测试用户与统计闭环
  - 测试用户 runtime seeder 完成。
  - 前端分析请求默认携带 `test_student`（`userId=1`）。
  - UserCourseStats / UserKnowledgeState 更新可用。
  - LeaderboardService 数据源验证完成。
- R13-a 前端知识点中文化
  - 分析结果中的知识点 code 已映射中文展示。
  - 未知 code fallback 为原 code。
- R13-b 学生端分析结果展示优化
  - 分析结果区已升级为学习反馈报告式展示。
  - 展示结构包括：顶部判定卡片、我的解答问题、标准解答、关联知识点、下一步建议、可视化建议。
  - API 文本已做 HTML escape，不展示 `RawResponseJson` / provider / key。
- R23 网页端公式校对（MathLive）
  - R23-a：`dev.html` MathLive 技术验证完成。
  - R23-b：前端已接入 OCR `formulas[]` 的 MathLive 编辑流程。
  - R23-c：拍照 OCR + MathLive 回归已完成接口实测与前端代码路径核对。
  - R23-d：OCR `formulas[]` 已从底部统一编辑思路升级为 inline MathLive 编辑，用户可在公式卡片内直接修改。
  - R23-vendor：MathLive 已 vendor 到 `wwwroot/vendor/mathlive`，当前 `ocr/dev` 默认使用本地静态文件。
- R14-a 课程资料知识库建模
  - `CourseMaterial` / `MaterialChunk` / `MaterialChunkKnowledgePoint` 三表已建模并完成迁移。
- R14-b PDF 入库骨架
  - 支持 PDF 上传与文本型 PDF 提取（PdfPig）。
  - 上传大小限制 100MB。
  - 扫描型 PDF 标记为 `ocr_pending`。
- R14-c 课程资料列表与状态查看
  - 课程资料页已支持“已上传资料”列表与解析状态查看（含 `chunkCount`）。
- R14-d Chunk 检索服务设计
  - 已完成 SQL/关键词检索服务设计方案。
- R14-e 检索服务实现
  - `IKnowledgeRetrievalService` SQL/关键词检索已实现（返回 preview，不返回全文）。
- R14-f 检索调试 API
  - `GET /api/course-materials/search` 已完成，用于检索效果调试。
- R14-h materials 检索调试区
  - `materials.html` 已新增“资料检索调试”区域，可按关键词检索 chunk preview。
- R15-a/b/c/d 拍照解答 OCR
  - 方案、Provider 抽象、`/api/photo-solutions/ocr` endpoint、前端入口已完成。
- R16 AnalysisService 拆分
  - R16-a：拆分设计完成。
  - R16-b：`LlmResponseParser` 抽出。
  - R16-c：`AnalysisFallbackService` 抽出。
  - R16-d：`AnalysisPersistenceService` 抽出。
  - R16-e：`MistakeRecordService` 抽出。
  - R16-f：`UserStatsUpdateService` 抽出。
  - R16-g：`LlmRequestFactory` 抽出。
- R18 前端多页面拆分
  - R18-a：多页面规划完成。
  - R18-b：统一导航 `nav.js` 完成。
  - R18-c：`materials.html` 课程资料页完成。
  - R18-d：`stats.html` 学习统计页完成。
  - R18-e：`dev.html` 开发工具页完成。
  - R18-f：首页 / 资料 / 统计 / 开发页基础拆分完成。
  - UI-split-audit：`index.html` 已收敛为入口页，新增 `analysis.html` 与 `ocr.html` 承载手动分析和拍照识别流程。
- R19 登录与用户体系（开发期）
  - R19-a：登录与用户体系设计完成。
  - R19-b：`AuthController` + `/api/auth/me` `/api/auth/login` `/api/auth/logout` 完成。
  - R19-c：前端 `auth.js` 接入完成（当前用户来源切换为 `/api/auth/me`）。
  - R19-d：`login.html` 开发期登录页完成。
  - R19-e：teacher/admin 权限设计完成。
  - R19-f：`IUserContext` / `CurrentUserService` 完成。
  - R19-g：`CourseMaterialsController` teacher/admin 权限检查完成。
  - R19-h：前端 `nav` + page guard 角色控制完成。
  - R19-i：legacy `QuestionController` admin/development 权限限制完成（R35-a 已审计，详见 `Docs/LegacyQuestionControllerAudit.md`）。
  - R19-j：`/api/learning-analysis/analyze` 的 `userId` 改为以后端 session 用户为准。
- R21 符号计算模块
  - R21-a：SymPy 符号计算模块设计完成。
  - R21-b：`Tools/Symbolic/symbolic_worker.py` 原型完成。
  - R21-c：`ISymbolicMathService` + `SymPySymbolicMathService` 完成。
  - R21-d：`POST /api/symbolic/compute` 完成。
  - R21-e：`dev.html` 符号计算调试入口完成。
  - R21-test：端到端验证完成。
- R22 AnalysisContext 上下文层
  - R22-a：AnalysisContextBuilder 设计完成。
  - R22-b：AnalysisContext DTO（含 SymbolicEvidence/Task）+ 空壳 Builder 完成。
  - R22-c：`LlmRequestFactory` 接收 `AnalysisContextDto` 参数完成。
  - R22-d：retrieval context 可选接入完成（`EnableKnowledgeRetrieval=true` 时调用 `IKnowledgeRetrievalService`）。
  - R22-e：本地回归测试完成（flag=false 稳定；flag=true+无 chunk 正常降级；userId mismatch 仍 403）。
  - R22-fake-chunk：开发期 `success chunk` 测试数据已验证（search 命中 + analyze 不阻断）。
- R26 生产容器化部署（进行中）
  - `docker-compose.prod.yml` 已完成（`sqlserver/litellm/server` + `restart: unless-stopped`）。
  - `docker compose -f docker-compose.prod.yml config` 已通过（在 `/etc/mathanalysis-ai/*.env` 就绪后）。
  - `docker compose -f docker-compose.prod.yml build server` 已通过。
  - 已产出镜像：`mathanalysis-server:latest`。
  - `/api/health` 轻量健康检查 endpoint 已完成。
  - `server` 服务 compose healthcheck 已完成（基于 `/api/health`）。
  - `docker compose -f docker-compose.prod.yml up -d` 已通过。
  - `sqlserver` / `litellm` / `server` 已实测均为 `Running`。
  - `/api/health` 已实测返回 `status=ok`。
  - `mathanalysis-server` Docker health 已实测为 `healthy`。
  - 启动初期前两次 healthcheck 连接失败、随后转为 `healthy`，属于正常启动窗口现象。
  - compose SQL Server 新 volume 下已完成数据库迁移初始化。
  - compose 环境下 `test_student` 登录 / session 已验证通过。
  - LiteLLM 直连 `math-reviewer` 已返回 `200`。
  - compose 环境下 `/api/learning-analysis/analyze` 已成功返回结构化分析结果。
- R27 本地 MVP 演示总回归
  - 本地 MVP 演示总回归已完成。
  - 基础服务、登录/session、OCR、文本分析、排行榜、student 权限回归已验证通过。
  - 当前版本可作为本地演示版 MVP 使用。
- R28 演示前准备
  - `Docs/DemoRunbook.md` 已新增，收纳演示前检查命令、固定 analyze 样例、OCR 推荐样例、权限演示与故障速查。
  - 已整理本地演示固定流程，降低现场演示失败概率。
- R29 演示版本冻结
  - 当前版本已进入“本地演示版 MVP 冻结状态”。
  - 已新增 `Docs/DemoFreeze.md`，明确冻结范围、禁止操作、允许操作、问题分级与回滚策略。
- R30 全项目审计
  - 已新增 `Docs/ProjectAudit.md`，收纳架构、主链路、安全、数据库、Provider、前端体验、部署与下一阶段建议。
- R30 production auth 设计
  - 已新增 `Docs/AuthDesign.md`，明确开发期认证现状、生产候选路线、`AuthMode` 设计、`AuthAccount/ExternalLogin` 预留与 Production 关闭 fallback 策略。
- R30-b AuthMode + Production fail-fast
  - 已新增 `AuthOptions` 配置抽象。
  - 已支持 `Auth:Mode=DevelopmentUsername / LocalPassword / Oidc / Disabled` 的配置识别。
  - 已在 `Program.cs` 中加入 Production fail-fast：
    - Production 禁止 `DevelopmentUsername`
    - Production 禁止 `EnableDevelopmentFallback=true`
    - Production 禁止任一 Development override=true
    - Production 下 `Auth:Mode` 为空也会拒绝启动
  - 当前仍未实现 `LocalPassword` / `Oidc` 真正登录能力。
- R30-c 认证数据模型设计
  - 已新增 `Docs/AuthDataModelDesign.md`。
  - 已完成 `AppUsers` 职责审计。
  - 已明确推荐：`AppUsers + AuthAccounts + 可选 LocalCredentials`。
  - 已明确生产最终优先 OIDC，`LocalPassword` 作为过渡 / 私有部署方案。
  - 当前仍仅为设计阶段，尚未新增实体、DbContext 或 migration。
- R33-a SQL 备份与恢复设计
  - 已新增 `Docs/SqlBackupDesign.md`。
  - 已明确 `.bak` 手动备份、宿主机副本、恢复顺序、恢复演练与保留策略。
  - 当前尚未实现自动备份脚本、恢复脚本或云端上传。
- R33-b 手动 SQL Server 备份脚本
  - 已新增 `scripts/backup-sqlserver.sh`。
  - 已支持手动生成 `MathAnalysisAI_YYYYMMDD_HHMMSS.bak` 并复制到宿主机备份目录。
  - 当前尚未实现 restore 脚本、定时备份或云端上传。
- R33-c SQL Server 恢复脚本设计
  - 已新增 `Docs/SqlRestoreScriptDesign.md`。
  - 已明确默认恢复到临时数据库名。
  - 已明确覆盖主库必须显式启用、显式确认、显式停服。
  - 当前 restore 脚本仍未实现，也未执行真实恢复。
- R33-c-impl SQL Server restore script phase 1
  - 已新增 `scripts/restore-sqlserver.sh`。
  - 已实现从 `.bak` 恢复到临时数据库。
  - 当前不支持覆盖 `MathAnalysisAI`。
  - 当前不会停止 `server`。
  - 已完成一次真实临时库恢复验证。
  - 覆盖主库恢复仍待后续阶段。
- R33-d 手动备份与临时恢复 runbook
  - 已新增 `Docs/BackupRestoreRunbook.md`。
  - 已整理手动备份、临时恢复、只读验证与临时库清理流程。
  - 当前最小备份恢复闭环已形成。
  - 定时备份 / 云端备份仍未实现。
- R33-e SQL Server 定时备份设计
  - 已新增 `Docs/ScheduledBackupDesign.md`。
  - 已明确本地以手动备份为主，Linux/腾讯云优先 `systemd timer`。
  - 已明确定时任务仅调用 `backup-sqlserver.sh`，不纳入自动恢复。
  - 当前定时任务仍未实现。
- R34-a Nginx + HTTPS 反向代理设计
  - 已新增 `Docs/NginxHttpsDesign.md`。
  - 已明确当前阶段推荐宿主机 Nginx，外部只开放 `80/443`。
  - 已明确 SQL Server / LiteLLM / ASP.NET server 不应作为公网直连入口。
  - `deploy/nginx/mathanalysis-ai.conf.example` 已存在，但服务器尚未按该方案真实接入。
- Linux 服务器部署试验
  - 已新增 `Docs/LinuxDeploymentTrialReport.md`。
  - Linux compose 首轮部署已验证通过：
    - SQL migration 完成
    - `MathAnalysisAI` 数据库存在
    - 20 张表存在
    - `/api/health` 返回 `200`
    - `mathanalysis-server` 为 `healthy`
    - `test_student` 登录成功
    - LiteLLM `math-reviewer` 直测成功
    - `/api/learning-analysis/analyze` 返回 `200`
    - leaderboard 更新正常
  - 已记录一次 `server.env` 与 `sqlserver.env` 密码不一致导致的登录/SQL 失败，并已通过修正 `server.env` + `--force-recreate server` 解决。
  - 仓库侧 `docker-compose.prod.yml` 已完成 compose 生产端口收敛修改：
    - `127.0.0.1:1433:1433`
    - `127.0.0.1:4000:4000`
    - `127.0.0.1:5131:5131`
  - 服务器侧仍需手动 `pull` 新代码并 `docker compose up -d --force-recreate` 才会生效。

## 3. 当前页面结构
- `index.html`：首页入口
  - 项目说明
  - 手动分析入口
  - 拍照识别入口
  - 学习统计入口
  - 课程资料入口
- `analysis.html`：手动分析页
  - 章节与模式选择
  - 题目输入
  - 学生解答输入
  - 分析结果展示
- `ocr.html`：拍照识别页
  - OCR 图片上传
  - OCR 回填
  - MathLive inline 公式校对
  - 手动开始分析
  - 分析结果展示
- `materials.html`：课程资料管理
  - PDF 上传
  - 解析状态展示
  - `ocr_pending` 提示
- `stats.html`：学习统计与排行榜
  - 公开排行榜
  - 个人统计预留
- `dev.html`：开发工具
  - 符号计算调试
  - MathLive 编辑器试验
  - legacy OCR / legacy analyze 区域已删除
  - 旧 `WrongQuestion` 数据表仍保留，仅作为历史兼容数据，不再作为页面功能入口

## 4. 当前主链路（MVP）
1. 前端页面或 `curl` 调用 `/api/learning-analysis/analyze`
2. `LearningAnalysisController`
3. `AnalysisService`（orchestrator）
4. 子服务协作：
   - Parsing：`LlmResponseParser`
   - Fallback：`AnalysisFallbackService`
   - Persistence：`AnalysisPersistenceService`
   - Mistakes：`MistakeRecordService`
   - Stats：`UserStatsUpdateService`
   - LLM Request Factory：`LlmRequestFactory`
5. `LLMGateway`
6. LiteLLM Proxy（litellm 模式）或直连 DeepSeek（direct 模式）
7. 回写：
   - `AnalysisResult`
   - `MistakeRecord`
   - `LLMRequestLog`
   - `UserCourseStats`
   - `UserKnowledgeState`
8. 排行榜链路：
   - `LeaderboardController`
   - `LeaderboardService`
   - `stats.html` 排行榜区域
9. 拍照解答 OCR 链路：
   - `/api/photo-solutions/ocr`
   - `IPhotoSolutionOcrProvider`
   - `LiteLLMPhotoSolutionOcrProvider`

## 5. 当前系统状态
- 文本分析主链路可用。
- LiteLLM + DeepSeek 文本分析可用。
- 文本分析 alias（`math-reviewer/math-solver/math-hint/math-explainer`）当前保持 DeepSeek 路线（不切到 Qwen）。
- 文本分析若出现 `DeepseekException - Authentication Fails` / `code=401`，当前优先排查 LiteLLM 进程环境中的 `DEEPSEEK_API_KEY`，而不是分析链路代码。
- compose 环境下 DeepSeek 文本分析链路已跑通：
  - SQL 迁移完成
  - 登录与 session 正常
  - LiteLLM 直测 `math-reviewer` 正常
  - `/api/learning-analysis/analyze` 返回 `200`
- 近期关键配置结论：
  - `LiteLLM__BaseUrl` 当前必须是完整 endpoint
  - compose 环境使用：`http://litellm:4000/v1/chat/completions`
  - 本地 `dotnet run` 使用：`http://localhost:4000/v1/chat/completions`
  - 修改 `server.env` / `litellm.env` 后，需要 `docker compose up -d --force-recreate` 相关服务，不能只做普通 restart
- 本地/服务器 secrets env 文件方案已文档化（`.env.local`、`/etc/mathanalysis-ai/*.env`、systemd `EnvironmentFile`）。
- 生产 compose 本地验证已完成：`config` 通过、`server` 镜像构建通过、`up -d` 可正常拉起。
- 本地演示操作手册已独立整理为 `Docs/DemoRunbook.md`。
- 演示冻结规则已独立整理为 `Docs/DemoFreeze.md`。
- 容器化本地联调状态已验证：
  - `mathanalysis-sqlserver` Running
  - `mathanalysis-litellm` Running
  - `mathanalysis-server` Running
  - `mathanalysis-server` health = `healthy`
- `/api/health` 已实测返回 `status=ok`。
- Linux 服务器首轮部署试验已跑通：
  - compose 启动成功
  - migration 成功
  - login / LiteLLM / analyze / leaderboard 均已验证通过
- 当前 Linux 部署主要剩余风险：
  - 服务器运行中的容器若尚未重建，`1433 / 4000 / 5131` 仍可能保持 `0.0.0.0`
  - 需要继续完成服务器侧端口收敛验证 + Nginx/HTTPS 阶段
- 拍照解答 OCR 已完成 DashScope / 阿里百炼真实联调。
- `photo-solution-ocr` 当前链路为：LiteLLM alias -> DashScope OpenAI-compatible 视觉模型。
- 已验证 OCR 返回 `problemText` / `studentSolutionText` / `formulas`。
- OCR 本地实测样例已验证：
  - `problemText` 非空
  - `studentSolutionText` 非空
  - `formulas[]` 非空
  - `warnings` 为可接受的 `section_split_uncertain`
- OCR 返回结构已覆盖：`problemText` / `studentSolutionText` / `formulas[]` / `warnings` / `confidence`。
- OCR JSON LaTeX 反斜杠解析问题已修复（非法反斜杠可容错修复）。
- OCR 题干/学生解答分区 prompt 已优化。
- 当前仍可能出现 `section_split_uncertain`，属于可接受 warning。
- `ocr.html` 已支持 OCR 后公式校对：
  - `formulas[]` 列表展示
  - MathLive inline 可视化编辑
  - 复制 LaTeX
  - 插入到题目末尾 / 我的解答末尾
- OCR 成功后不会自动调用 analyze，用户校对后手动点击“开始分析”。
- MathLive 默认使用本地 vendor（`/vendor/mathlive/*`），降低外网依赖。
- 当前本地演示版 MVP 已冻结；演示前默认只允许修复 `P0` / `P1` 阻断问题。
- 拍照解答前后端入口已就绪。
- 课程资料 PDF 上传可用。
- 扫描型 PDF 可识别为 `ocr_pending`。
- 课程资料页已支持：
  - PDF 上传
  - 资料列表
  - `parseStatus` / `chunkCount` 查看
  - `ocr_pending` 提示
  - 关键词检索 chunk preview
- 主分析链路已具备 AnalysisContextBuilder 上下文层。
- `AnalysisService` 会在创建 pending AnalysisResult 后、构建 LLM request 前调用 `AnalysisContextBuilder`。
- `LlmRequestFactory` 已支持接收 `AnalysisContextDto`。
- 当前支持可选课程资料检索上下文：
  - 开关：`AnalysisContext:EnableKnowledgeRetrieval`（默认 `false`）
  - 开启后调用 `IKnowledgeRetrievalService`
  - 仅注入 `ContentPreview`
  - 不注入 `MaterialChunk.Content` 全文
  - 不注入 `StoragePath` / `FileHash`
  - 检索为空或异常时不阻断 analyze
- 检索当前只返回 `ContentPreview`，不返回全文。
- 已通过开发期 fake chunk（`parseStatus=success && chunkCount=1`）完成“有 chunk 数据”检索场景验证。
- `GET /api/course-materials/search` 已命中测试资料：`开发测试资料-反常积分`，返回 `ContentPreview` 且不返回全文/路径/hash。
- `EnableKnowledgeRetrieval=true` 下调用 analyze 不报 500（当前在缺少 LLM key 时进入可接受失败分支）。
- compose 环境下 analyze 成功样例已验证：
  - `problemType=判断`
  - `difficulty=简单`
  - `knowledgePoints` 包含 `ma.improper_integral.convergence_criteria`
  - `studentSolutionReview.isCorrect=false`
  - `mainIssue` 指出“被积函数趋于0不是反常积分收敛充分条件”
- student 权限回归已验证：
  - `materials` 相关 API 返回 `403`
  - legacy `Question` API 返回 `403`
  - `userId mismatch` 返回 `403 Forbidden userId mismatch.`
- 公开排行榜接口已验证可访问，且 `test_student` 统计会随 analyze 请求更新。
- R22 fake chunk 在当前 compose 新库中未重新 seed，因此本轮未做“有数据检索命中”浏览器/接口复测。
- 当前无法直接从 `LLMRequestLog` 核验完整 prompt（不存 prompt 全文）。
- 当前仍不建议落库完整 prompt；后续建议仅增加 `contextInjected / knowledgeChunkCount / contextCharCount` 摘要观测。
- 扫描型 PDF 仍需后续 OCR 才能产生可检索 chunk。
- 学生统计闭环可用。
- 排行榜已迁移到 `stats.html`。
- 前端多页面拆分完成。
- 开发期登录闭环可用。
- 当前用户上下文服务可用。
- 前端不再直接依赖硬编码 `userId=1`。
- 前端导航已按角色显示。
- `materials.html` teacher/admin 可见，student 隐藏/无权访问。
- `CourseMaterialsController` 已后端拦截 student。
- `dev.html` admin 可见，当前用于 symbolic / MathLive 等开发调试，不再作为 legacy Question 链路入口。
- 当前仍非生产级认证。
- `analyze` 不再盲目信任前端 `userId`。
- `request.UserId` 为空或等于当前用户时允许。
- `request.UserId` 与当前用户不一致时返回 `403`。
- `StudentSolution` / `UserCourseStats` / `UserKnowledgeState` 归属以后端当前用户为准。
- `analysis.html` / `ocr.html` 分析结果已改为学习反馈报告式展示（学生可读）。
- 系统已具备独立符号计算工具层。
- 支持 `simplify / expand / factor / diff / integrate / limit / solve / series`。
- 符号计算当前仅作为 dev/admin 调试工具。
- 尚未接入 `AnalysisService` 主分析链路。
- 计算通过 Python SymPy worker 执行。

## 6. 后续待办
1. R35-a：legacy QuestionController 审计与 containment design（已完成，见 `Docs/LegacyQuestionControllerAudit.md`）
2. R35-b：legacy 删除前全仓库引用验证（已完成，见审计文档第 9/11 节）— 结论：可安全进入 R35-d+e 删除
3. R35-c：legacy QuestionController Production 禁用 + 错误泄露修复（已完成）— Production 环境返回 404，Development 保持 admin 访问；Upload 错误消息已通用化
4. R35-d/e：legacy 前端引用清理 + 控制器删除（部分完成：index.html legacy-ocr.js 引用已移除，dev.html 已添加 Legacy 警告；控制器和 legacy-ocr.js 本体保留待演示后删除）
5. R34-b：Nginx 配置模板（已完成）— `deploy/nginx/mathanalysis-ai.conf.example`
6. R34-c-design：Forwarded Headers 设计（已完成）— `Docs/ForwardedHeadersDesign.md`
7. R34-f：腾讯云安全组检查清单（已完成）— `Docs/TencentCloudSecurityGroupChecklist.md`
8. R36-a：Rate Limiting 设计（已完成）— `Docs/RateLimitDesign.md`
9. R36-b：Rate Limiting 实现（已完成）— `Program.cs` 配置 + 4 个 controller 标注（login/analyze/ocr/symbolic），详见 `Docs/RateLimitImplementationReport.md`
10. R36-b-fix：中间件顺序修复 + 运行时验证（已完成）— `UseRouting→UseSession→UseRateLimiter→UseAuthorization→UseEndpoints`；并行测试 3/15 通过，12/15 429；R40 审计确认 health 结论需更正
11. R36-c：前端 429 友好提示（已完成）— `api.js` 统一 enrichment + `ui.js` 格式化 + login/analyze/OCR 三个调用方
12. R34-c-impl：ASP.NET Forwarded Headers 最小实现（已完成）— `Program.cs` 添加 ForwardedHeaders middleware；信任 loopback + 可配置 Docker bridge
13. R38-b-prep：MathJax 本地 vendor 计划（已完成）— `Docs/MathJaxVendorPlan.md`（不替换，不下载）
14. R37-b-prep：CSRF 实现前准备（已完成）— `Docs/CsrfImplementationPlan.md`（切入点审计 + 4 步实现方案）
15. R35-cleanup-prep：Legacy 删除准备清单（已完成）— `Docs/LegacyRemovalPlan.md`
16. R35-d+e：Legacy 代码删除（已完成）— 删除 QuestionController.cs + legacy-ocr.js + LLMService.cs + MathpixService.cs；保留 WrongQuestion 模型/表；清理 Program.cs/AuthOptions/appsettings/dev.html/nav.js 引用
17. R40-final-audit：health 豁免审计（已完成）— 确认 `[DisableRateLimiting]` 仅用于 HealthController；health 连续 5×200；login 限流 14/15×429
18. R36-c-verify：前端 429 UI 静态验证（已完成）— api.js enrichment + ui.js + login/analyze/OCR 调用方验证通过
19. R34-c-verify：Forwarded Headers 审计 + 验证 runbook（已完成）— 配置安全确认 + `Docs/ForwardedHeadersVerificationRunbook.md`
16. R40-fix：`/api/health` 限流豁免修复（已完成）— `HealthController` 已添加 `[DisableRateLimiting]`；`/api/health` 不再受 `GlobalLimiter` 影响，login/analyze/ocr/symbolic 策略保持不变
9. R37-a：CSRF 设计（已完成）— `Docs/CsrfDesign.md`
10. R38-a：CDN/SRI/本地 vendor 审计（已完成）— `Docs/FrontendDependencyAudit.md`
3. 可选增加开发期 context 观测摘要（不落完整 prompt）：
   - `contextInjected`
   - `knowledgeChunkCount`
   - `contextCharCount`
4. R22-f：symbolic task extraction 设计。
5. R22-g：symbolic evidence 小范围接入设计/实现（默认 flag off）。
4. R14-g 可视为已部分完成：retrieval context 已进入主链路，当前已完成 fake chunk 场景验证；后续仍需更贴近真实教材 chunk 的质量回归。
7. 扫描版教材 OCR 后续实现。
8. 可选：`materials.html` 增加 chunk 摘要浏览（不展示全文）。
9. 可选：后续引入向量检索。
10. teacher scope 数据模型设计（课程/班级范围）待完善。
11. 生产级认证/OAuth 评估与落地。
12. 上线前必须关闭 Development fallback / override。
13. 可选：ASP.NET Core Authorization 标准化。
14. 若接学校统一认证/OAuth，可能需要新增 `AuthAccount` 或映射表。
15. 可选继续抽 `AnalysisValidationService`。
16. 可选抽统一 `FailureHandler`。
17. 可选为各子服务补齐单元测试。
18. 生产部署方案未完成。
19. `dev.html` 正式环境隐藏或权限限制策略待定。
20. 可选：R13-c Prompt 输出结构微调。
21. 可选：分析结果展示接入公式渲染 / Markdown-LaTeX 渲染。
22. R21-g：AnalysisService 可选接入设计。
23. 可选：FastAPI worker 常驻化。
24. 可选：SageMath 评估。
25. 可选：符号计算结果 LaTeX/Markdown 渲染增强。
26. 拍照 OCR 结果质量仍需更多样本回归（含复杂版式、弱分区样本）。
27. MathLive 浏览器真实点击截图级验证仍待补充（当前已完成接口实测与代码路径核对）。
28. 课程资料扫描 PDF OCR 仍未接入（当前仅拍照解答 OCR 已联调）。
29. 公式渲染统一增强（MathLive/LaTeX 展示一致性）待后续规划。
30. 可选后续增强：`/api/health/deep`（DB/LiteLLM/provider 连通性检查）。
31. 本地 MVP 演示虽已可用，但 OCR/MathLive 的浏览器点击级回归仍建议现场手动再走一遍。
32. `R26-nginx-https`（反向代理与证书）待完成。
33. `R26-backup`（SQL Server 备份）待完成。
34. 生产 secrets 仍需替换占位值为真实值（仅服务器本地）。
35. `R26-symbolic-container`（生产镜像内 Python/SymPy）待完成。
