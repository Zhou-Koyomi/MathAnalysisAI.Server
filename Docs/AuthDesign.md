# 生产级认证方案设计（Auth Design）

## 1. 设计结论
### 1.1 总结
- 当前项目**不适合继续沿用 username-only 登录进入公网环境**。
- 推荐路线不是“直接把现在的 `/api/auth/login` 硬化”，而是分阶段演进：
  1. **短期**：保留演示版不动，只完成生产认证设计与模式抽象
  2. **中期**：引入 `AuthMode`，支持 `DevelopmentUsername / LocalPassword / Oidc / Disabled`
  3. **长期**：优先落地 **OIDC / OAuth**，本地账号密码仅作为过渡或校内私有部署备选

### 1.2 推荐路线
- **短期推荐**：设计先行，不改演示版代码
- **中期推荐**：先做 `AuthMode + AuthAccount / ExternalLogin` 预留
- **长期推荐**：生产环境优先 OIDC / OAuth，Development 保留本地开发登录

### 1.3 为什么不是直接做账号密码
- 本地账号密码实现快，但要自己承担：
  - 密码哈希
  - 重置流程
  - 锁定 / 风控
  - 多端登录与审计
  - 忘记密码 / 管理员重置
- 对当前项目阶段而言，**OIDC 更适合真实公网或学校内测场景**。
- 但 OIDC 配置复杂，因此推荐先完成模式分层，再决定最终 IdP。

## 2. 当前 Auth 现状审计
### 2.1 当前登录方式
- `AuthController`
  - `POST /api/auth/login`
  - 输入：`LoginRequestDto { Username }`
  - 行为：仅按 `AppUsers.Username` 查用户
  - 不校验密码
- `GET /api/auth/me`
  - 从当前 session 读取用户
- `POST /api/auth/logout`
  - 清理 session `auth_user_id`

### 2.2 DTO 现状
- `LoginRequestDto`
  - 仅有 `Username`
- `CurrentUserDto`
  - `UserId`
  - `Username`
  - `RealName`
  - `Role`
  - `SchoolName`
  - `DepartmentName`
  - `ClassName`

### 2.3 当前用户上下文
- `IUserContext`
  - `GetCurrentUserAsync`
  - `GetCurrentUserIdAsync`
  - `GetCurrentRoleAsync`
  - `IsInRoleAsync`
  - `IsInAnyRoleAsync`
  - `IsAuthenticatedAsync`
- `CurrentUserService`
  - session key：`auth_user_id`
  - 若无 session 且为 Development 且 `Auth:EnableDevelopmentFallback=true`
  - 使用 `Auth:DevelopmentFallbackUser`
  - 不强制写回 session

### 2.4 Development 相关配置
- `Auth:EnableDevelopmentFallback`
- `Auth:DevelopmentFallbackUser`
- `Auth:EnableDevelopmentMaterialAccessOverride`
- `Auth:EnableDevelopmentLegacyAccessOverride`
- `Auth:EnableDevelopmentSymbolicAccessOverride`

### 2.5 当前权限边界
- analyze：
  - `LearningAnalysisController` 已以后端 `IUserContext` 当前用户为准
  - `request.UserId mismatch` 返回 `403`
- `CourseMaterialsController`
  - 仅 `teacher/admin`
  - Development override 可选
- `QuestionController`
  - 仅 `admin` 或 Development override
- `SymbolicController`
  - 仅 `admin` 或 Development override
- 前端 `auth.js` / `nav.js`
  - 已有 `hasRole / hasAnyRole / requireAnyRole`
  - 只是 UX，不是安全边界

### 2.6 当前不可公网部署原因
- username-only 登录无密码
- 无标准 claims / authorization
- 无 OIDC / OAuth
- Development fallback / override 存在误开风险
- teacher/admin scope 尚未落地
- 当前“谁是 teacher 能看哪些学生”还没有数据库级边界

## 3. 当前认证链路图（文字版）
1. 前端调用 `/api/auth/login`
2. `AuthController` 按用户名查 `AppUsers`
3. 将 `auth_user_id` 写入 session
4. 前端调用 `/api/auth/me`
5. `CurrentUserService` 从 session 或 Development fallback 返回用户
6. 页面 `auth.js` 缓存当前用户
7. 控制器通过 `IUserContext` 做角色 / 登录态判断
8. analyze 再次以后端 `currentUser.Id` 收敛 `request.UserId`

## 4. 生产认证候选路线比较
## 4.1 A：本地账号密码登录
### 做法
- `AppUsers` 增加：
  - `PasswordHash`
  - `PasswordSalt` 或改为单字段 modern hash
  - `PasswordUpdatedAt`
  - 可选 `PasswordResetRequired`
  - 可选 `FailedLoginCount / LockedUntil`

### 优点
- 实现路径最直接
- 不依赖外部 IdP
- 私有化部署可控

### 缺点
- 需要自行承担密码安全责任
- 需要补重置 / 风控 / 审计
- 后续切换 OIDC 时可能还要再做一层迁移

### 适用场景
- 私有部署
- 校内封闭试点
- 短期过渡

## 4.2 B：OAuth / OIDC 登录
### 做法
- 接入学校统一认证或标准 OIDC Provider
- 本地只维护用户映射与角色
- 不存本地密码，或本地密码只保留应急管理员账号

### 优点
- 更符合生产安全边界
- 可复用外部 MFA / 风控 / 审计
- 更适合未来公网和校内统一体系

### 缺点
- 配置复杂
- 依赖外部 IdP
- 本地联调成本更高

### 适用场景
- 公网部署
- 学校统一认证
- 多用户内测 / 正式上线

## 4.3 C：过渡方案（生产禁用 username-only，开发保留）
### 做法
- Production 禁用 `DevelopmentUsername`
- Development 仍允许 `test_student`
- Production 只允许：
  - `LocalPassword`
  - 或 `Oidc`
  - 或 `Disabled`

### 优点
- 兼容当前演示与开发效率
- 不会破坏现有 MVP
- 为未来生产认证预留清晰开关

### 缺点
- 若只停在此阶段，生产问题并未真正解决
- 仍需继续落地正式认证方式

## 5. 推荐路线
### 5.1 短期推荐
- 保持当前演示冻结版不动
- 先完成认证模式设计与文档
- 不在演示前接入真实生产认证

### 5.2 中期推荐
- 增加 `AuthMode` 配置与统一认证入口抽象
- 预留 `AuthAccount / ExternalLogin` 数据模型
- 先实现：
  - `DevelopmentUsername`
  - `LocalPassword` 或 `Oidc` 二选一作为第一条生产路线

### 5.3 长期推荐
- **优先 OIDC / OAuth**
- 本地密码只作为：
  - 过渡方案
  - 私有部署方案
  - 应急管理员方案

## 6. 推荐实施拆分
- `R30-a`：本设计
- `R30-b`：引入 `AuthMode` 配置抽象
- `R30-c`：设计 `AuthAccount / ExternalLogin` 数据模型
- `R30-d`：实现 `LocalPassword` 或 `Oidc`
- `R30-e`：启动时安全检查，Production 禁止 fallback / override
- `R30-f`：将后端权限判断迁移到标准 Authorization / Policy
- `R30-g`：前端登录态改造成生产认证入口

### 6.1 当前实施状态
- `R30-b` 已完成：
  - 已引入 `Auth:Mode`
  - 已增加 `AuthOptions` 配置抽象
  - 已在 `Program.cs` 中加入 Production fail-fast
- `R30-c` 已完成设计文档：
  - 见 `Docs/AuthDataModelDesign.md`
  - 已明确推荐 `AppUsers + AuthAccounts + 可选 LocalCredentials`
- 当前仍**未实现**：
  - `LocalPassword`
  - `Oidc`
  - `AuthAccount / ExternalLogin` 数据表

## 7. 配置设计
建议未来统一为：

```json
"Auth": {
  "Mode": "DevelopmentUsername",
  "EnableDevelopmentFallback": false,
  "EnableDevelopmentMaterialAccessOverride": false,
  "EnableDevelopmentLegacyAccessOverride": false,
  "EnableDevelopmentSymbolicAccessOverride": false,
  "RequireHttps": true,
  "CookieSecurePolicy": "Always",
  "CookieSameSite": "Lax",
  "CookieName": ".MathAnalysisAI.Auth",
  "Oidc": {
    "Authority": "",
    "ClientId": "",
    "ClientSecret": "",
    "CallbackPath": "/signin-oidc",
    "Scopes": [ "openid", "profile", "email" ]
  }
}
```

### 7.1 `AuthMode` 建议枚举
- `DevelopmentUsername`
  - 仅 Development 用
  - 仅本地演示 / 开发
- `LocalPassword`
  - 本地账号密码
  - 过渡或私有部署
- `Oidc`
  - 生产优先推荐
- `Disabled`
  - 用于禁用登录入口或维护模式

当前实现状态：
- 已支持识别并校验：
  - `DevelopmentUsername`
  - `LocalPassword`
  - `Oidc`
  - `Disabled`
- 但当前只有 `DevelopmentUsername` 具备现有登录链路；
- `LocalPassword` / `Oidc` 目前仍属于“配置可识别、功能未实现”状态。

## 8. 数据模型设计建议
### 8.1 为什么不直接把所有认证信息塞进 `AppUsers`
- `AppUsers` 适合保留“平台用户画像与角色”
- 外部身份、密码认证、多个登录源更适合独立出去

### 8.2 推荐模型
#### 方案 A：只做本地密码
- 可直接给 `AppUsers` 增字段
- 快，但可扩展性一般

#### 方案 B：推荐
新增 `AuthAccount` / `ExternalLogin` 表：
- `AuthAccount`
  - `Id`
  - `UserId`
  - `Provider`
  - `ProviderSubject`
  - `Email`
  - `PasswordHash`（仅本地账号时使用）
  - `PasswordUpdatedAt`
  - `IsActive`
  - `CreatedAt`
  - `UpdatedAt`
- 或拆为：
  - `LocalPasswordCredential`
  - `ExternalLogin`

### 8.3 推荐原因
- 更适合一人多登录源
- 更适合未来学校统一认证 / OAuth
- 不污染核心业务 `AppUsers`

## 9. 生产环境与 Development 的兼容策略
### 9.1 演示版兼容原则
- 当前本地 MVP 演示版保持不动
- `test_student` 继续作为 Development 演示用户
- `login.html` 继续作为开发期用户名登录页

### 9.2 生产环境原则
- Production 禁止：
  - `DevelopmentUsername`
  - `EnableDevelopmentFallback=true`
  - 任意 Development override=true

### 9.3 启动时安全检查建议
应用启动时增加 fail-fast 规则：
- 若 `ASPNETCORE_ENVIRONMENT=Production`
  - 且 `Auth:Mode=DevelopmentUsername`
  - 或任一 `EnableDevelopment*` 为 `true`
  - 则直接拒绝启动并输出清晰错误

当前实施状态：
- 已在 `Program.cs` 落地 fail-fast；
- 若 Production 下 `Auth:Mode` 为空，也会拒绝启动；
- 若 `Auth:Mode` 不在允许值中，也会拒绝启动。

## 10. teacher/admin scope 前置设计
### 10.1 当前缺口
- 当前只有角色，没有课程 / 班级范围
- teacher 能“看什么 / 管什么”尚无数据库边界

### 10.2 后续建议表
- `CourseTeacher`
  - `CourseId`
  - `TeacherUserId`
- `Class`
  - `Id`
  - `CourseId`
  - `Name`
- `ClassEnrollment`
  - `ClassId`
  - `StudentUserId`
- 可选 `CourseEnrollment`

### 10.3 设计原则
- teacher 默认不能跨课程看全体学生
- admin 可全局
- analyze 的“代学生提交”只有在 scope 明确后才开放

## 11. 兼容当前 analyze 主链路的要求
### 11.1 已有正确做法
- analyze 已以后端 session 用户为准
- `request.UserId mismatch` 返回 `403`

### 11.2 后续保持原则
- student 永远不能伪造别人 `userId`
- teacher/admin 未来如需代提交：
  - 必须新增 `targetUserId`
  - 必须校验 course/class scope
  - 当前阶段默认禁用

## 12. Cookie / Session / Token 设计建议
### 12.1 当前状态
- 当前使用 ASP.NET Core Session
- 对本地演示足够

### 12.2 生产建议
- Web 站点场景可继续基于 Cookie + Server-side auth session
- 如果未来需要 API / 小程序 / 多端：
  - 再评估 Token / OIDC access token

### 12.3 当前推荐
- 对现有 Web MVP：
  - 先走 Cookie + OIDC 登录会话
  - 不必急着改成纯 JWT

## 13. 关闭 Development fallback / override 的策略
### 13.1 配置策略
- Development：
  - 允许 `DevelopmentUsername`
  - fallback 可保留
- Production：
  - fallback 必须 `false`
  - `MaterialAccessOverride` 必须 `false`
  - `LegacyAccessOverride` 必须 `false`
  - `SymbolicAccessOverride` 必须 `false`

### 13.2 运维策略
- 在 `DeploymentRunbook` 保持这些值默认为 `false`
- 上线脚本或启动健康检查前增加配置核查

## 14. 如何避免破坏当前 MVP 演示链路
- 不修改现有 `AuthController`、`CurrentUserService`、`LearningAnalysisController`
- 不改前端 `login.html`
- 不改 DemoRunbook / DemoFreeze 的演示前提
- 生产认证工作必须在新阶段、独立开关下推进
- 演示版仍以当前链路为准，直到新认证方案完整落地并回归通过

## 15. 推荐决策
### 15.1 如果近期目标是“继续本地演示 + 小范围开发”
- 保持当前演示版不动
- 先实现 `AuthMode` 抽象与 fail-fast

### 15.2 如果近期目标是“校内真实内测”
- 优先做 `LocalPassword` 或直接做 OIDC
- 同时补 teacher scope

### 15.3 如果近期目标是“公网部署”
- 直接优先 OIDC / OAuth
- 同步补：
  - HTTPS
  - 备份
  - deep health
  - 关闭所有 Development 入口

## 16. 最终建议
- **短期**：不动当前演示认证，实现设计与模式抽象
- **中期**：优先落地 `AuthMode + AuthAccount`
- **长期**：生产环境采用 **OIDC / OAuth**，Development 保留独立开发登录
