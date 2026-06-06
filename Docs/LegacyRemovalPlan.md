# Legacy 代码删除计划

> **R35-cleanup-prep** | 日期：2026-06-03 | 状态：删除前准备（未执行删除）

---

## 1. 可安全删除候选

### 1.1 文件级删除

| 文件 | 行数 | 理由 | 验证状态 |
|------|------|------|---------|
| `Controllers/QuestionController.cs` | 317 | Production 已禁用（R35-c）；OCR 被 `/api/photo-solutions/ocr` 替代；analyze 被 `/api/learning-analysis/analyze` 替代 | R35-b 已验证零主链路依赖 |
| `Services/LLMService.cs` | ~50 | 零引用（无任何类注入使用）；`QuestionController` 也不使用它（直接用 `new HttpClient()`） | R35-b 已验证零引用 |
| `Services/MathpixService.cs` | 6 | 空类，零引用，非标准命名空间 `MathAnalysisAI.Services` | R35-b 新发现 |
| `wwwroot/js/legacy-ocr.js` | 73 | `index.html` 已移除引用（R35-d/e）；`dev.html` 仍加载 | 需同步清理 dev.html |

### 1.2 代码片段级删除

| 文件 | 行 | 内容 | 理由 |
|------|-----|------|------|
| `wwwroot/dev.html:103-112` | legacy 区域 | `#legacyArea` DOM + 警告文案 | Controller 删除后无意义 |
| `wwwroot/dev.html:122` | `<script src="/js/legacy-ocr.js">` | Legacy JS 引用 | JS 文件删除后需同步移除 |
| `wwwroot/js/nav.js:99-101` | `loadQuestions()` 调用 | 仅在 `#list` 存在时执行 | DOM 删除后此调用为 no-op |
| `Program.cs:65` | `builder.Services.AddScoped<LLMService>()` | 零消费者 DI 注册 | R35-b 已验证 |
| `Program.cs:181-184` | `EnableDevelopmentLegacyAccessOverride` fail-fast | 3 行 | 配置属性删除后需移除 |
| `Options/AuthOptions.cs:16` | `EnableDevelopmentLegacyAccessOverride` 属性 | 仅被 QuestionController 和 Program.cs 使用 | 同步删除 |
| `appsettings.json:49` | `"EnableDevelopmentLegacyAccessOverride": false` | 废弃配置项 | 同步删除 |
| `appsettings.Development.json:7` | 同上 | 废弃配置项 | 同步删除 |

---

## 2. 需要谨慎处理

### 2.1 WrongQuestion 模型与表

| 项 | 文件 | 现状 | 策略 |
|----|------|------|------|
| 模型 | `Models/WrongQuestion.cs` | 被 `ApplicationDbContext` 注册 | **保留模型** |
| DbSet | `Data/ApplicationDbContext.cs:14` | `DbSet<WrongQuestion>` | **保留 DbSet**（已有 migration 依赖） |
| 表 | `WrongQuestions`（数据库） | 存在于已有 migration 中 | **不删表** |
| Migration 引用 | `Migrations/*Snapshot.cs` | `WrongQuestion` 在 EF snapshot 中 | **不生成新 migration** |

**原因**：
- 删除 EF 实体需要生成 migration，违反演示冻结
- 保留空壳模型不引入运行时成本（无消费者 = 无查询）
- 后续可在 `R40-db-cleanup` 独立处理

### 2.2 wwwroot/uploads 目录

可能存在历史遗留的上传文件。删除目录 vs 仅清理文件需要评估。**当前不建议操作**。

---

## 3. 删除前检查命令

执行以下命令确认无遗漏引用：

```bash
# 检查 /api/Question 路由引用
rg "/api/Question" --type-add 'code:*.{cs,js,html}' -t code

# 检查 legacy-ocr 引用
rg "legacy-ocr" --type-add 'code:*.{cs,js,html}' -t code

# 检查 LLMService 引用（排除自身和文档）
rg "LLMService" --type-add 'code:*.{cs,js,html}' -t code | grep -v "LLMService.cs" | grep -v "Docs/"

# 检查 WrongQuestion 引用（排除模型自身和 DbContext 注册）
rg "WrongQuestion" --type cs | grep -v "Models/WrongQuestion.cs" | grep -v "Migrations/"

# 检查 MathpixService 引用
rg "MathpixService"

# 检查 EnableDevelopmentLegacyAccessOverride 引用
rg "EnableDevelopmentLegacyAccessOverride"
```

---

## 4. 删除后验证清单

- [ ] `dotnet build` 通过（0 errors）
- [ ] `docker compose -f docker-compose.prod.yml config` 通过
- [ ] 首页 `index.html` 正常加载（OCR + MathLive + analyze 入口可见）
- [ ] `dev.html` 正常加载（symbolic + MathLive dev 区域可见，legacy 区域已移除）
- [ ] `POST /api/photo-solutions/ocr` 正常（如服务运行）
- [ ] `POST /api/learning-analysis/analyze` 正常
- [ ] `GET /api/leaderboard/public` 正常
- [ ] `GET /api/auth/me` 正常
- [ ] `login.html` 登录正常
- [ ] `stats.html` 排行榜正常
- [ ] student 访问 `materials.html` 仍被拦截
- [ ] `/api/health` 正常

---

## 5. 推荐执行顺序

```
R35-d+e 合并提交，按以下顺序执行：
1. 删除 Services/LLMService.cs
2. 删除 Services/MathpixService.cs
3. 删除 Controllers/QuestionController.cs
4. 删除 wwwroot/js/legacy-ocr.js
5. 修改 wwwroot/index.html（如仍有残留引用）
6. 修改 wwwroot/dev.html（移除 legacy 区域 DOM + script 标签）
7. 修改 wwwroot/js/nav.js（移除 loadQuestions() 调用）
8. 修改 Program.cs（移除 LLMService DI 注册 + EnableDevelopmentLegacyAccessOverride fail-fast 条目）
9. 修改 Options/AuthOptions.cs（移除 EnableDevelopmentLegacyAccessOverride 属性）
10. 修改 appsettings.json（移除 EnableDevelopmentLegacyAccessOverride 配置项）
11. 修改 appsettings.Development.json（同上）
12. dotnet build 确认通过
13. docker compose 重建确认
```

---

## 6. 回滚方案

删除操作通过 git 管理，回滚只需：

```bash
git checkout -- <deleted-files>
git checkout -- <modified-files>
dotnet build
```

如已提交：
```bash
git revert <commit-hash>
```

---

## 7. 决策结论

- ❌ 当前不执行删除（演示冻结）
- ✅ 删除候选清单已就绪
- ✅ 删除前检查命令已就绪
- ✅ 删除后验证清单已就绪
- ✅ 推荐演示冻结解除后一次性执行（R35-d+e 合并提交）
- ⚠️ WrongQuestion 模型/表不删除（独立阶段处理）
