# 演示版本冻结说明（Demo Freeze）

## 1. 冻结状态
当前版本进入“本地演示版 MVP 冻结状态”。

含义：
- 主链路已经通过 `R27` 本地总回归。
- 演示前不再主动开发新功能。
- 仅允许修复 `P0` / `P1` 阻断问题。
- `P2` / `P3` 问题只记录，不在演示前大改。

## 2. 冻结范围
当前冻结以下内容：
- `docker-compose.prod.yml`
- [Dockerfile](/Users/night_creek/开发/数学分析智能体/MathAnalysisAI.Server/Dockerfile)
- `/api/health`
- LiteLLM 文本 alias
- DashScope OCR alias
- `AnalysisService` 主链路
- Auth / session / `userId` 收敛逻辑
- OCR + MathLive 校对流程
- `index.html` 分析结果展示
- `stats.html` / leaderboard
- student 权限拦截

## 3. 演示前禁止操作
演示前禁止：
- 不改 migration
- 不改数据库 schema
- 不重置 SQL volume
- 不执行 `docker compose down -v`
- 不替换 LiteLLM alias
- 不切换 DeepSeek / Qwen provider 路线
- 不重构前端 OCR / MathLive 交互
- 不改 `AnalysisService` / `LlmRequestFactory` / `LLMGateway`
- 不提交真实 key
- 不贴 `docker compose config` 的真实展开输出

## 4. 演示前允许操作
演示前允许：
- 重启容器
- 检查 health
- 重新登录
- 重新跑固定 analyze 样例
- 重新上传固定 OCR 图片
- 手动执行演示前数据库备份
- 修正文档
- 修 `P0` / `P1` 阻断 bug
- 临时补 seed `R22FakeChunk` 用于 retrieval 演示，但需要记录执行时间和用途

## 5. 问题分级规则
- `P0`
  - 演示无法启动或主链路完全中断
  - 例如：server 起不来、`/api/health` 不通、登录彻底失败、analyze 全挂
- `P1`
  - 主流程关键环节失败，但有明确修复路径
  - 例如：LiteLLM BaseUrl 错误、key 未注入、OCR 上传按钮能点但接口固定失败
- `P2`
  - 影响体验但可绕过
  - 例如：OCR 分区不准、MathLive 某些样式不理想、排行榜文案一般
- `P3`
  - 文案、样式、说明、文档问题

处理策略：
- `P0` / `P1`：允许演示前修复
- `P2` / `P3`：只记录，不演示前大改

## 6. 演示前最后 10 分钟检查
- `docker compose -f docker-compose.prod.yml ps`
- `curl -i http://localhost:5131/api/health`
- `docker inspect --format='{{json .State.Health}}' mathanalysis-server`
- `docker exec mathanalysis-server printenv LiteLLM__BaseUrl`
- 检查 `/etc/mathanalysis-ai/litellm.env` 中 key 不是 `placeholder`
- LiteLLM `math-reviewer` 直测一次
- 登录 `test_student`
- 跑一次固定 analyze 样例
- 用固定 OCR 图片跑一次 OCR
- 在首页公式卡片手动点一次 MathLive
- 打开一次 `/stats.html`

## 7. 回滚策略
如果演示前突然坏掉，优先按这个顺序排查：
1. server 配置问题：检查 `/etc/mathanalysis-ai/server.env`
2. LiteLLM key 问题：检查 `/etc/mathanalysis-ai/litellm.env`
3. BaseUrl 问题：确认 `LiteLLM__BaseUrl` 是完整 endpoint
4. 数据库问题：确认 `MathAnalysisAI` 数据库存在且迁移已完成
5. 端口问题：检查 `5131` / `4000` / `1433`
6. 容器异常：`docker compose -f docker-compose.prod.yml restart <service>`

注意：
- 不使用 `docker compose down -v`
- 不重置当前演示数据库 volume
- 若演示前执行恢复操作，必须记录原因、时间和影响范围

## 8. 后续开发建议
演示冻结后，后续新增功能建议单独开阶段推进：
- `R30-production-auth`
- `R31-pdf-ocr`
- `R32-teacher-scope`
- `R33-backup-nginx`
- `R34-symbolic-container`
- `R35-legacy-question-cleanup`（R35-a 审计已完成，见 `Docs/LegacyQuestionControllerAudit.md`，演示后执行清理）
