# 数学分析智能体项目审计报告

## 1. 审计结论
### 1.1 总体判断
- 当前项目**适合继续做本地演示**。
- 当前项目**不适合直接公网部署**。
- 当前项目**不适合直接面向真实多用户内测**，除非先补齐认证、安全、备份与部署边界。

### 1.2 结论摘要
- 本地 MVP 主链路已经形成闭环：
  - 登录 / session
  - OCR
  - MathLive 公式校对
  - DeepSeek 文本分析
  - 统计 / 排行榜
  - student 权限拦截
- `dotnet build` 当前通过。
- `docker compose -f docker-compose.prod.yml config` 当前通过。
- 项目已进入演示冻结阶段，适合在当前状态下继续做稳定演示。

### 1.3 下一阶段最优先的 3 件事
1. 生产级认证与权限模型补齐（`R30-production-auth / OAuth`）
2. 部署可靠性补齐（SQL backup、Nginx/HTTPS、deep health）
3. teacher/admin scope 数据模型与资料管理边界补齐（`R32-teacher-scope`）

说明：
- 认证模式设计已见 `Docs/AuthDesign.md`
- 认证数据模型设计已见 `Docs/AuthDataModelDesign.md`
- SQL 备份与恢复设计已见 `Docs/SqlBackupDesign.md`
- Nginx / HTTPS 部署设计已见 `Docs/NginxHttpsDesign.md`

## 2. 当前系统概览
### 2.1 当前架构文字版
- 前端静态页面：
  - `index.html`：学习分析、OCR、公式校对、结果展示
  - `stats.html`：学习统计与公开排行榜
  - `materials.html`：课程资料管理与检索调试
  - `dev.html`：开发工具与 symbolic / legacy 调试
  - `login.html`：开发期用户名登录
- 后端 ASP.NET Core：
  - Controllers 层承载 HTTP API
  - `AnalysisService` 作为主分析 orchestrator
  - 拆分子服务负责解析、fallback、持久化、错题绑定、统计更新、LLM 请求构造
  - `LLMGateway` 统一负责 DeepSeek / LiteLLM 模型调用与 `LLMRequestLog`
  - OCR 通过 `LiteLLMPhotoSolutionOcrProvider`
  - 符号计算通过 `SymPySymbolicMathService` 调 Python worker
- 数据存储：
  - SQL Server 存业务主数据、分析结果、统计、课程资料、日志
- 模型代理层：
  - LiteLLM 承担 alias 路由
  - 文本分析当前走 DeepSeek
  - 拍照 OCR 当前走 DashScope / Qwen-VL
- 运维层：
  - `docker-compose.prod.yml` 管理 `sqlserver` / `litellm` / `server`
  - `/api/health` + Docker healthcheck 构成浅健康检查

### 2.2 主要请求链路
#### 登录 / session 链路
1. 前端调用 `/api/auth/login`
2. `AuthController` 仅按 `username` 查 `AppUsers`
3. 写入 session `auth_user_id`
4. `/api/auth/me` 通过 `CurrentUserService` 读取当前用户

#### analyze 链路
1. 前端或 curl 调 `/api/learning-analysis/analyze`
2. `LearningAnalysisController` 用 `IUserContext` 收敛 `request.UserId`
3. `AnalysisService`
4. `AnalysisPersistenceService` 先创建 `Problem` / `StudentSolution` / pending `AnalysisResult`
5. `AnalysisContextBuilder` 可选拼 retrieval context
6. `LlmRequestFactory` 组装 prompt / alias / requestType
7. `LLMGateway` 调 LiteLLM 或直连 DeepSeek
8. parser / fallback / normalize / schema validate
9. 保存 `AnalysisResult`、`MistakeRecord`、`LLMRequestLog`
10. 更新 `UserCourseStats` / `UserKnowledgeState`

#### OCR 链路
1. 前端上传图片到 `/api/photo-solutions/ocr`
2. `PhotoSolutionsController` 校验大小、扩展名、content-type
3. `LiteLLMPhotoSolutionOcrProvider` 调 LiteLLM 视觉 alias
4. 清洗模型输出 JSON
5. 返回 `problemText` / `studentSolutionText` / `formulas[]`
6. 前端回填文本并渲染 inline MathLive

#### retrieval 链路
1. `IKnowledgeRetrievalService` 从 `MaterialChunks` 做 SQL / 关键词检索
2. `AnalysisContextBuilder` 在开关打开时将 `ContentPreview` 注入 prompt context
3. 默认 `AnalysisContext:EnableKnowledgeRetrieval=false`

#### symbolic 链路
1. `/api/symbolic/compute`
2. `SymbolicController` 做 admin / Development override 权限检查
3. `SymPySymbolicMathService`
4. 启动 `Tools/Symbolic/symbolic_worker.py`
5. stdin/stdout JSON 协议返回 CAS 结果

#### deployment 启动链路
1. Docker daemon 启动
2. compose 拉起 `sqlserver` / `litellm` / `server`
3. `/etc/mathanalysis-ai/*.env` 注入 secrets
4. `server` 容器健康检查访问 `/api/health`

## 3. 已完成能力
### 3.1 已完成且已验证
- Docker compose 本地闭环
- SQL migration 执行流程
- `/api/health` shallow healthcheck
- `test_student` 登录 / session
- `LearningAnalysisController` userId 收敛
- DeepSeek 文本分析链路
- DashScope OCR 链路
- OCR JSON 反斜杠修复
- OCR `formulas[]` inline MathLive 编辑
- 分析结果学习反馈展示
- stats / leaderboard 更新
- student 对 `materials` / `dev` / legacy question 的权限拦截

### 3.2 已完成，但主要是代码路径核对或局部验证
- `materials.html` / `dev.html` 页面 guard 的浏览器点击级验证仍偏轻
- MathLive inline 编辑的浏览器截图级验证仍不足
- retrieval context 注入已有服务链路，但“有数据命中”的 compose 新库验证依赖 fake chunk

### 3.3 已完成但未接入主流程
- SymPy symbolic 工具链
- `/api/symbolic/compute`
- `dev.html` symbolic 调试入口
- retrieval symbolic evidence 注入主分析链路仍未开启

### 3.4 已冻结，不建议演示前动
- `docker-compose.prod.yml`
- `Dockerfile`
- `/api/health`
- LiteLLM alias 路线
- `AnalysisService` 主链路
- OCR + MathLive 主体验
- session / userId 收敛
- 公开排行榜与 student 权限边界

## 4. 主链路审计
### 4.1 analyze 主链路
优点：
- 控制器层已接管用户归属，避免 student 伪造 `userId`
- `AnalysisService` 已降为 orchestrator，内部职责清晰
- LLM 失败、parse 失败、schema invalid 均能落回同一条 pending `AnalysisResult`
- `LLMRequestLog` 记录 requestType / provider / model / tokens / latency，但不存 prompt 全文

风险：
- 结构化输出仍高度依赖 LLM 返回 JSON
- schema invalid 与 parse failed 仍会让本次分析失去结果
- 当前 `LLMGateway` 把 `LiteLLM__BaseUrl` 当最终完整 URL，配置易错

### 4.2 OCR 主链路
优点：
- 已对图片大小、类型做基础限制
- 已对 JSON code fence、非法反斜杠做容错
- 已明确“只 OCR 不解题”
- 前端要求人工复核，不自动 analyze

风险：
- OCR 分区仍可能出现 `section_split_uncertain`
- 真实版式复杂时 `studentSolutionText` 仍可能 `[unclear]`
- 当前未对 OCR 结果做持久化审计

### 4.3 登录 / session 主链路
优点：
- 当前用户上下文统一由 `CurrentUserService` 管理
- session 与 Development fallback 逻辑已集中
- analyze 已以后端用户为准

风险：
- 当前仍是开发期 `username-only` 登录
- 无密码、无 claims、无标准 authorization pipeline

### 4.4 retrieval 主链路
优点：
- SQL 关键词检索已成型
- 只注入 `ContentPreview`
- 默认关闭，避免误伤主链路稳定性

风险：
- 默认关闭意味着当前大多数分析不会自动受益于课程资料
- fake chunk 之外的真实命中质量仍待更多数据验证
- 当前缺少 context 注入观测字段

### 4.5 symbolic 主链路
优点：
- worker 通过白名单操作 + timeout 控制
- 与主分析链路隔离

风险：
- 生产容器当前默认不保证 Python/SymPy 可用
- 当前不适合直接纳入学生主流程

## 5. 安全审计
### 5.1 当前安全边界
- `LearningAnalysisController` 已拒绝 `userId mismatch`
- `CourseMaterialsController` 已阻止 student 访问资料管理 API
- `QuestionController` 已限制为 admin / Development override
- `SymbolicController` 已限制为 admin / Development override
- 前端分析结果与 OCR 文本展示已做 escape
- 前端不展示 `RawResponseJson`
- LiteLLM / DeepSeek / DashScope key 设计上只放 env，不进前端
- OCR 不返回图片 base64、provider raw body、Authorization

### 5.2 已知不足
- 当前认证仍是开发期方案，不是生产认证
- Development fallback / override 如果带着上线会有越权风险
- teacher/admin 的真实课程 / 班级 scope 尚未落地
- legacy `QuestionController` 仍直接调 DeepSeek 官方接口，且仍属旧链路（详见 `Docs/LegacyQuestionControllerAudit.md`，R35-a 审计）
- `AllowedHosts` 仍为 `*`
- `/api/health` 无鉴权，虽然问题不大，但只适合公开浅检查

### 5.3 上线前必须补的项
- 生产级认证与会话 / claims 体系
- 关闭全部 Development fallback / override
- teacher/admin scope 数据模型
- HTTPS 入口
- 数据备份与恢复流程

## 6. 数据库与迁移审计
### 6.1 当前数据库状态概览
已存在迁移：
- `Init`
- `InitialCreate`
- `AddLearningPlatformSchema`
- `AddMathAnalysisSeedData`
- `AddCourseMaterialKnowledgeBase`

当前数据库职责大致包括：
- 用户与权限：`AppUsers`
- 课程与知识点：`Course` / `Chapter` / `KnowledgePoint`
- 分析主链路：`Problem` / `StudentSolution` / `AnalysisResult` / `MistakeRecord`
- 统计：`UserCourseStats` / `UserKnowledgeState`
- 模型日志：`LLMRequestLog`
- 课程资料：`CourseMaterial` / `MaterialChunk` / `MaterialChunkKnowledgePoint`

### 6.2 seed 依赖
- `PromptProfile` 通过 runtime seeder
- `test_student` 通过 runtime seeder
- 课程与知识点依赖迁移 / seed 数据

### 6.3 compose 新 volume 初始化要求
- 新 SQL volume 默认没有 `MathAnalysisAI`
- 首次必须执行 `dotnet ef database update --connection ...`
- 这是当前部署闭环中的必要步骤之一

### 6.4 上线前数据库待办
- SQL backup 策略
- 恢复演练
- 生产 seed 边界说明
- 风险操作提示（特别是 `down -v`）

### 6.5 备份优先级建议
- 优先级：高
- 原因：当前 compose volume 有状态，但尚未建立自动备份闭环

## 7. LLM / Provider 审计
### 7.1 当前 provider 状态
- 文本分析 alias：
  - `math-reviewer`
  - `math-solver`
  - `math-hint`
  - `math-explainer`
  - 当前走 DeepSeek
- OCR alias：
  - `photo-solution-ocr`
  - 当前走 DashScope / Qwen-VL

### 7.2 当前易错点
- `LiteLLM__BaseUrl` 必须是完整 endpoint：
  - 本地：`http://localhost:4000/v1/chat/completions`
  - compose：`http://litellm:4000/v1/chat/completions`
- `server.env` / `litellm.env` 改后需要 `--force-recreate`
- `docker compose config` 会展开 env，不适合贴公开输出

### 7.3 LLMGateway 状态
- 优点：
  - 统一错误码
  - 记录 tokens / latency / model / requestType
  - 不存 prompt 全文
- 风险：
  - 结构化输出仍靠模型配合
  - 没有 provider 级 deep health
  - 没有自动 fallback / retry / queue

### 7.4 成本 / 并发 / 多 key 状态
- 当前是单 key / 单链路 MVP
- 还没有真正的限流、配额、并发治理
- “双 DeepSeek 多轮裁决”适合后续作为 `R36` 独立质量增强链路，不建议现在混入主路径

## 8. OCR 与公式校对审计
### 8.1 当前能力
- OCR 能回填题目与我的解答
- `formulas[]` 可渲染为 inline MathLive
- 用户可直接编辑、复制、插入到题目 / 解答末尾
- OCR 不会自动触发 analyze

### 8.2 当前体验边界
- OCR 结果需要人工复核
- 分区不确定是可接受现象，不是当前阻断
- MathLive fallback 已有，但用户仍可能看到源码模式

### 8.3 下一阶段建议
- 增加更多真实图片样本回归
- 增强公式渲染一致性（LaTeX / Markdown / MathLive）
- 若走公网部署，再考虑浏览器级 E2E

## 9. 前端体验审计
### 9.1 当前可演示体验
- `index.html` 主路径清晰
- OCR -> 校对 -> 手动分析 逻辑明确
- 学习反馈展示结构化程度较好
- `stats.html` 能承接演示闭环
- `materials.html` / `dev.html` 对 student 有访问限制

### 9.2 体验问题列表
- OCR 分区偶发不准
- MathLive 浏览器点击级回归仍偏轻
- 移动端 / 窄屏表现未见系统级验收结论
- retrieval 结果未进入默认演示路径
- 公式渲染与分析结果中的 LaTeX 展示仍有进一步统一空间

### 9.3 优先级建议
- 先做浏览器 E2E / 小范围人工验收
- 再做公式展示统一增强
- 再考虑更多教师视角页面体验

## 10. 部署审计
### 10.1 当前部署可用边界
- 本地 compose 已可启动
- healthcheck 可用
- env_file secrets 方案已成型
- `server` 镜像本地可构建

### 10.2 腾讯云部署前必须补项
- 生产级认证
- Nginx / HTTPS
- SQL backup
- deep health（DB / LiteLLM / provider）
- 运维文档中的恢复步骤与故障演练

### 10.3 可延后项
- symbolic worker 容器化
- provider 深层指标
- 自动化日志治理 / log rotate

## 11. 测试覆盖审计
### 11.1 已验证矩阵
- `R20/R26`：compose analyze 成功
- `R23`：OCR / MathLive 接口与代码路径验证
- `R27`：MVP 总回归
- 权限回归：student / materials / dev / question / userId mismatch
- `R22FakeChunk`：retrieval fake chunk 命中验证
- `R21`：symbolic worker / API 链路验证

### 11.2 未完全验证矩阵
- 浏览器点击级 OCR + MathLive 全自动回归
- 移动端 / 窄屏 UI 回归
- 真实 teacher/admin 角色链路
- retrieval context 在真实教材 success chunk 上的持续效果
- symbolic 在生产容器内可用性

### 11.3 建议新增的最小测试清单
- 浏览器 E2E：
  - 登录
  - OCR 上传
  - analyze
  - stats 打开
  - student 权限拦截
- compose 环境 smoke test：
  - `/api/health`
  - LiteLLM 直测
  - fixed analyze 样例

## 12. 风险清单
### 12.1 P0
1. 当前没有备份与恢复闭环，若误操作 volume 或主机故障，存在数据不可恢复风险

### 12.2 P1
1. 当前认证仍为开发期 `username-only` 登录，不适合公网
2. Development fallback / override 若误开到生产，会造成权限边界失效
3. `LiteLLM__BaseUrl` 必须写完整 endpoint，配置很容易误填
4. OCR 结果仍依赖模型输出合法 JSON，虽然已有修复链，但仍有脆弱性
5. 文本分析仍依赖结构化 LLM 输出，schema invalid / parse failed 会导致结果缺失
6. `/api/health` 是 shallow health，不检查 DB / LiteLLM / provider
7. teacher/admin scope 未真正落地，后续多用户场景存在越权设计空缺
8. symbolic worker 在生产容器中当前不保证可用

### 12.3 P2
1. OCR 分区仍可能不准
2. retrieval 默认关闭，真实资料增强价值尚未体现
3. fake chunk 依赖手动 seed，不适合长期演示
4. MathLive 浏览器点击级验证偏少
5. 移动端 / 窄屏适配风险尚未系统验收
6. prompt context 注入缺少摘要级观测字段
7. 课程资料扫描 PDF OCR 未接入
8. compose 首次迁移需要手动执行，运维门槛仍偏高

### 12.4 P3
1. 文档数量较多，入口虽已有但仍有信息分散
2. `WeatherForecastController` 等模板残留仍在仓库中
3. `wwwroot/uploads` 下存在演示/测试图片，后续需要更明确清理策略
4. legacy `QuestionController` 仍保留较旧风格实现（R35-a 已审计，详见 `Docs/LegacyQuestionControllerAudit.md`）
5. 样式与文案统一性仍可继续收束

## 13. 下一阶段路线建议
### 13.1 立即做
1. `R30-production-auth`
2. `R33-SQL backup`
3. `R34-Nginx HTTPS`（R34-b Nginx 模板已完成，R34-c-design ForwardedHeaders 设计已完成，R34-f 安全组清单已完成）
4. `R35-provider deep health`
5. `R32-teacher-scope`
6. `R35-legacy-question-cleanup`（R35-a 审计 + R35-b 引用验证 + R35-c Production 禁用已完成）
7. `R36-rate-limit`（R36-a 设计已完成）
8. `R37-csrf`（R37-a 设计已完成）
9. `R38-cdn-sri`（R38-a CDN 审计已完成）

### 13.2 演示后做
1. `R31-PDF OCR`
2. `R37-formula/Markdown rendering`
3. `R38-retrieval context` 观测字段
4. `R40-browser E2E tests`

### 13.3 上线前必须做
1. 生产认证 / OAuth 或等价方案
2. 关闭 Development fallback / override
3. SQL backup / restore
4. HTTPS
5. deep health / provider health

### 13.4 可长期规划
1. `R36-multi-pass DeepSeek`
2. `R39-symbolic worker container`
3. 更细粒度 teacher scope / class scope
4. 资料检索与 symbolic evidence 的更深度接入

补充：
- 生产认证路线设计已单独整理为 `Docs/AuthDesign.md`

## 14. 演示冻结建议
- 继续保持 `R29` 演示冻结规则
- 演示前默认只处理 `P0` / `P1`
- retrieval / symbolic / teacher scope 等新工作建议全部演示后另开阶段

## 15. 附录：关键命令与配置约定
### 15.1 构建与 compose
```bash
cd "/Users/night_creek/开发/数学分析智能体/MathAnalysisAI.Server"
dotnet build
docker compose -f docker-compose.prod.yml config
docker compose -f docker-compose.prod.yml ps
```

### 15.2 health
```bash
curl -i http://localhost:5131/api/health
docker inspect --format='{{json .State.Health}}' mathanalysis-server
```

### 15.3 LiteLLM BaseUrl 约定
- 本地：`http://localhost:4000/v1/chat/completions`
- compose：`http://litellm:4000/v1/chat/completions`

### 15.4 固定 analyze 样例
```bash
curl -i -c /tmp/mathauth.cookie \
  -X POST http://localhost:5131/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"test_student"}'

curl -i -b /tmp/mathauth.cookie \
  -X POST http://localhost:5131/api/learning-analysis/analyze \
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

### 15.5 OCR 推荐样例
- `/Users/night_creek/Downloads/图像.jpeg`

### 15.6 风险提示
- 不执行 `docker compose down -v`
- 不提交真实 key
- 不公开贴出 `docker compose config` 展开的 secrets
