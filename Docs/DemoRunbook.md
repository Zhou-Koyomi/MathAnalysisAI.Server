# 本地 MVP 演示手册（Demo Runbook）

配套冻结规则见：[DemoFreeze.md](/Users/night_creek/开发/数学分析智能体/MathAnalysisAI.Server/Docs/DemoFreeze.md)。

## 1. 演示目标
- 当前本地 MVP 演示链路：
  - 登录 `test_student`
  - 上传图片做拍照 OCR
  - 检查题目 / 解答回填
  - 在公式卡片内用 inline MathLive 校对公式
  - 手动点击“开始分析”
  - 展示 DeepSeek 返回的结构化学习反馈
  - 打开统计页查看学习统计 / 排行榜
  - 以 student 身份演示权限拦截

## 2. 演示前环境检查
进入项目目录：
```bash
cd "/Users/night_creek/开发/数学分析智能体/MathAnalysisAI.Server"
```

检查 compose 服务状态：
```bash
docker compose -f docker-compose.prod.yml ps
curl -i http://localhost:5131/api/health
docker inspect --format='{{json .State.Health}}' mathanalysis-server
docker exec mathanalysis-server printenv LiteLLM__BaseUrl
```

预期：
- `mathanalysis-server` / `mathanalysis-litellm` / `mathanalysis-sqlserver` 均为 `Running`
- `mathanalysis-server` health 为 `healthy`
- `/api/health` 返回 `200`，且 `status=ok`
- `LiteLLM__BaseUrl=http://litellm:4000/v1/chat/completions`

## 3. API Key 与 env 检查
不泄露 key 的检查方式：
```bash
awk -F= '
/DEEPSEEK_API_KEY/ {
  if ($2 == "placeholder" || $2 == "" || $2 ~ /your_/) print "DEEPSEEK_API_KEY_INVALID_OR_PLACEHOLDER";
  else print "DEEPSEEK_API_KEY_SET";
}
/DASHSCOPE_API_KEY/ {
  if ($2 == "placeholder" || $2 == "" || $2 ~ /your_/) print "DASHSCOPE_API_KEY_INVALID_OR_PLACEHOLDER";
  else print "DASHSCOPE_API_KEY_SET";
}
' /etc/mathanalysis-ai/litellm.env
```

预期：
- 输出 `DEEPSEEK_API_KEY_SET`
- 输出 `DASHSCOPE_API_KEY_SET`

## 4. LiteLLM 文本模型直测
```bash
curl -i http://localhost:4000/v1/chat/completions \
  -H "Authorization: Bearer sk-local-litellm-master-key" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "math-reviewer",
    "messages": [
      { "role": "user", "content": "用一句话回答：1+1等于几？" }
    ]
  }'
```

预期：
- HTTP `200`
- 返回模型文本回答

## 5. 固定登录与 analyze 样例
登录：
```bash
curl -i -c /tmp/mathauth.cookie \
  -X POST http://localhost:5131/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"test_student"}'
```

验证当前用户：
```bash
curl -i -b /tmp/mathauth.cookie http://localhost:5131/api/auth/me
```

固定 analyze 样例：
```bash
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

预期：
- HTTP `200`
- `studentSolutionReview.isCorrect=false`
- `mainIssue` 指出“函数趋于 0 不是反常积分收敛充分条件”
- 不应出现：
  - `Not logged in`
  - `DeepseekException Authentication Fails`
  - `Method Not Allowed`
  - `missing_api_key`

## 6. OCR 演示样例
推荐测试图片：
- `/Users/night_creek/Downloads/图像.jpeg`

说明：
- 当前已验证该样例可返回 `problemText` / `studentSolutionText` / `formulas[]`
- 允许出现 `section_split_uncertain`
- 演示时应明确说明：OCR 结果需要用户人工复核，不会自动进入分析

## 7. MathLive 演示步骤
建议现场这样演示：
1. 在首页上传图片并点击“识别题目与解答”
2. 检查 OCR 回填的题目与我的解答
3. 查看公式卡片
4. 直接点击公式本身进行编辑
5. 观察“当前 LaTeX”小字实时变化
6. 点击“插入到题目末尾”或“插入到我的解答末尾”
7. 确认插入格式为：空行 + `$<latex>$`
8. 最后手动点击“开始分析”

强调：
- OCR 成功后不会自动调用 analyze
- MathLive 默认从本地 `/vendor/mathlive/*` 加载，不依赖 CDN

## 8. Stats 与权限演示
统计页：
- 打开 [http://localhost:5131/stats.html](http://localhost:5131/stats.html)
- 展示公开排行榜与个人统计变化

权限页：
- 打开 [http://localhost:5131/materials.html](http://localhost:5131/materials.html)
- 打开 [http://localhost:5131/dev.html](http://localhost:5131/dev.html)

预期：
- student 无权访问资料管理区
- student 无权访问开发工具区
- 不应继续触发受限管理接口

`userId mismatch` 固定验证：
```bash
curl -i -b /tmp/mathauth.cookie \
  -X POST http://localhost:5131/api/learning-analysis/analyze \
  -H "Content-Type: application/json" \
  -d '{
    "courseId": 200,
    "chapterId": 307,
    "problemText": "测试",
    "studentSolutionText": "测试",
    "analysisMode": "review_solution",
    "userId": 999
  }'
```

预期：
- HTTP `403`
- `Forbidden userId mismatch.`

## 9. Retrieval 演示准备
说明：
- 当前 compose 新库若未执行 fake chunk seed，则课程资料检索“命中演示”不可用
- 如需演示，先执行：
  - [R22FakeChunk.sql](/Users/night_creek/开发/数学分析智能体/MathAnalysisAI.Server/Docs/DevSeedSql/R22FakeChunk.sql)

执行方式：
```bash
docker exec -i mathanalysis-sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -C -S localhost -U sa -P 'YourStrongPassword@123' -d MathAnalysisAI \
  < "/Users/night_creek/开发/数学分析智能体/MathAnalysisAI.Server/Docs/DevSeedSql/R22FakeChunk.sql"
```

种子后测试：
```bash
curl "http://localhost:5131/api/course-materials/search?courseId=200&chapterId=307&q=反常积分%20比较判别法%20收敛&topK=3"
```

预期：
- 返回 `开发测试资料-反常积分`
- 不返回全文 / `StoragePath` / `FileHash`

注意：
- student 对部分材料管理 API 返回 `403` 是正确行为

## 10. 常见故障速查
| 现象 | 可能原因 | 快速处理 |
|---|---|---|
| `Cannot open database "MathAnalysisAI"` | 未执行 migration | 先执行 `dotnet ef database update --connection ...` |
| `Not logged in` | 未登录或请求未带 cookie | 重新登录并检查 `-b /tmp/mathauth.cookie` |
| `Forbidden userId mismatch` | cookie 用户和请求 `userId` 不一致 | 改回当前用户 id，或不传 `userId` |
| `DeepseekException Authentication Fails` | `DEEPSEEK_API_KEY` 无效或未注入 LiteLLM | 检查 `/etc/mathanalysis-ai/litellm.env`，修改后 `--force-recreate litellm` |
| `Method Not Allowed` | `LiteLLM__BaseUrl` 不是完整 endpoint | 改为 `http://litellm:4000/v1/chat/completions` 并 `--force-recreate server` |
| OCR parse failed | 模型返回 JSON / LaTeX 转义异常 | 检查 OCR Provider 容错清洗链是否仍在 |
| MathLive 未加载 | 本地 vendor 资源不存在或路径 404 | 检查 `/vendor/mathlive/*` 是否可访问 |
| `docker compose config` 输出包含 env | config 会展开 env_file | 不要把真实 key 输出贴到聊天/日志/公开 issue |

## 11. 演示话术简稿
这是一个面向数学分析学习场景的本地 MVP。它支持拍照 OCR，把题目、学生解答和公式先提取出来，再让用户用可视化公式编辑器做人工校对，确认后手动发起分析。后端通过 DeepSeek 生成结构化学习反馈，同时把统计结果更新到排行榜和个人学习状态里。当前版本重点验证主链路闭环，后续还会继续补生产级认证、PDF OCR、teacher scope、备份和 HTTPS。

## 12. 推荐演示顺序
1. 打开 `/login.html`，登录 `test_student`
2. 打开 `/index.html`
3. 上传 OCR 样例图片
4. 检查题目 / 解答回填
5. 在公式卡片中直接编辑 MathLive 公式
6. 插入一条公式到题目或解答末尾
7. 手动点击“开始分析”
8. 展示结构化学习反馈
9. 打开 `/stats.html` 展示统计
10. 打开 `/materials.html` / `/dev.html` 展示 student 权限拦截

## 13. 演示前冻结规则
- 当前版本已进入“本地演示版 MVP 冻结状态”。
- 演示前不再主动加新功能。
- 仅允许处理 `P0` / `P1` 阻断问题。
- `P2` / `P3` 问题记录到后续待办，不在演示前大改。
- 详细冻结范围、禁止操作和回滚策略见 [DemoFreeze.md](/Users/night_creek/开发/数学分析智能体/MathAnalysisAI.Server/Docs/DemoFreeze.md)。

## 14. 演示前最后 10 分钟检查
- `docker compose -f docker-compose.prod.yml ps`
- `curl -i http://localhost:5131/api/health`
- `docker inspect --format='{{json .State.Health}}' mathanalysis-server`
- `docker exec mathanalysis-server printenv LiteLLM__BaseUrl`
- `curl -i http://localhost:4000/v1/chat/completions ...` 文本直测成功
- `/etc/mathanalysis-ai/litellm.env` 中两类 key 不是 `placeholder`
- `/login.html` 可登录
- 固定 analyze 样例能返回 `200`
- OCR 样例图片路径可用
- OCR 成功后公式卡片可点一次
- `/stats.html` 能打开一次
- 首页公式卡片使用本地 MathLive vendor，而不是 CDN
