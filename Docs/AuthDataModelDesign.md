# 认证数据模型设计

## 1. 背景
- 当前项目已经完成本地演示版 MVP，并进入演示冻结状态。
- 当前认证仍为开发期 `username-only` 登录：
  - `/api/auth/login` 仅按 `username` 登录；
  - 无密码；
  - 无 OIDC / OAuth；
  - 无生产级 claims / scopes；
  - session 仅保存 `AppUserId`。
- `R30-b` 已完成 `AuthMode` 配置抽象与 Production fail-fast：
  - 支持 `DevelopmentUsername / LocalPassword / Oidc / Disabled`
  - Production 下禁止 `DevelopmentUsername`
  - Production 下禁止 `Development fallback / override`
- 当前仍未实现：
  - `LocalPassword`
  - `Oidc`
  - `AuthAccount / ExternalLogin` 数据表
  - teacher/admin scope 完整模型

本设计的目标是：在不破坏现有 `AppUser.Id` 业务主键体系的前提下，为后续生产认证与授权边界提供一条可实施的数据模型路线。

## 2. 当前 AppUsers 审计
### 2.1 当前字段
当前 `AppUsers` 主表字段包括：
- `Id`
- `Username`
- `RealName`
- `StudentNumber`
- `Role`
- `SchoolName`
- `DepartmentName`
- `ClassName`
- `CreatedAt`

### 2.2 当前用途
`AppUsers` 当前承担的是“业务用户主表”职责，而不是“认证凭据表”职责：
- 登录后 session 只保存 `auth_user_id`，即 `AppUser.Id`
- `CurrentUserService` / `IUserContext` 都以 `AppUser.Id` 为核心
- `LearningAnalysisController` 的用户归属收敛也以当前 `AppUser.Id` 为准
- 统计、分析结果、学生解答等业务数据都通过 `AppUser.Id` 关联

现有业务依赖包括但不限于：
- `StudentSolution.UserId`
- `UserCourseStats.UserId`
- `UserKnowledgeState.UserId`
- `Problem.CreatedByUserId`
- `LLMRequestLog.UserId`

### 2.3 当前 Role 用法
当前 `Role` 取值已包括：
- `student`
- `teacher`
- `school_leader`
- `admin`

它目前主要用于：
- 学生端 / 教师端 / 管理员端权限门
- `materials` / `dev` / legacy question / symbolic 的访问控制

### 2.4 当前 seed 与开发用户
当前开发用户 `test_student` 通过 `AppUserSeeder` 写入 `AppUsers`：
- `Username = test_student`
- `StudentNumber = 20260001`
- `Role = student`
- 带有演示用学校、院系、班级信息

### 2.5 审计结论
- `AppUsers` **适合作为业务用户主表继续保留**。
- `AppUsers` 中的 `RealName / StudentNumber / SchoolName / DepartmentName / ClassName / Role` 更接近“业务画像 + 权限主体”。
- `AppUsers` **不适合直接承载**：
  - 密码哈希
  - 外部身份绑定
  - OIDC Subject
  - Provider token / claims

## 3. 设计原则
1. `AppUsers` 继续作为业务用户主表。
2. 认证凭据与外部身份不直接塞进 `AppUsers`。
3. 一个 `AppUser` 可以绑定多个登录身份。
4. `LocalPassword` 与 `Oidc` 共享同一套“身份绑定到 AppUser”的总模型。
5. `Role` 继续保留在 `AppUsers`（或未来再演化为角色表），但 teacher scope 不进入认证表。
6. session 仍只保存 `AppUserId`，后续认证方式变化不应破坏当前业务外键体系。
7. teacher / class / course scope 由单独授权模型解决，不把 `CourseId / ClassId` 塞进认证表。

## 4. 候选模型比较
### 4.1 方案 A：单表 `AuthAccounts`
单表示意字段：
- `Id`
- `AppUserId`
- `Provider`
- `ProviderUserId`
- `Email`
- `DisplayName`
- `IsEmailVerified`
- `PasswordHash`
- `PasswordAlgorithm`
- `CreatedAt`
- `LastLoginAt`
- `IsDisabled`
- `MetadataJson`

优点：
- 表少，第一眼简单
- 初期 migration 数量少

缺点：
- 本地密码字段与外部身份字段混在一起，空值较多
- 容易形成“一个表既是账号表又是密码表又是外部身份表”的膨胀结构
- 安全字段和普通映射字段混杂，不利于审计和后续演进
- 多身份绑定时语义不够清晰

### 4.2 方案 B：分表方案
推荐拆分为：
- `AuthAccounts`
- `LocalCredentials`
- 可选 `AuthLoginAudit`

其中：
- `AuthAccounts` 负责“某个外部或本地登录身份绑定到哪个 AppUser”
- `LocalCredentials` 只负责本地密码凭据
- OIDC / OAuth 只使用 `AuthAccounts`

优点：
- 本地密码与外部身份字段清晰隔离
- 安全字段边界更清楚
- 支持一个 `AppUser` 绑定多个身份来源
- 更适合后续增加锁定、禁用、审计、邮箱验证等能力

缺点：
- 表更多
- 第一阶段实现复杂度略高

### 4.3 推荐结论
**推荐方案 B：`AppUsers + AuthAccounts + 可选 LocalCredentials`。**

原因：
- 当前项目已经明确会同时考虑 `LocalPassword` 和 `Oidc`
- 当前项目后续还要引入 teacher/admin scope，认证模型需要保持干净
- 分表带来的复杂度是可控的，但换来的是长期演进清晰度

## 5. 推荐模型
### 5.1 核心表关系
推荐关系：
- `AppUsers`
  - 业务用户主表
- `AuthAccounts`
  - 登录身份绑定表
  - 一个 `AppUser` 对应 1..N 条 `AuthAccounts`
- `LocalCredentials`
  - 本地密码表
  - 仅对 `Provider=local_password` 的账号生效
- `AuthLoginAudit`（可选，后续）
  - 登录审计表

### 5.2 语义划分
#### `AppUsers`
负责：
- 业务身份
- 用户画像
- 平台角色
- 统计 / 分析 / 学习记录归属

#### `AuthAccounts`
负责：
- 登录来源映射
- Provider + ProviderUserId 到 `AppUserId` 的绑定
- 邮箱、显示名等登录域信息
- 禁用 / 最近登录等认证域状态

#### `LocalCredentials`
负责：
- 本地密码哈希
- 密码算法版本
- 锁定 / 登录失败计数

## 6. 字段草案
### 6.1 `AuthAccounts`
建议字段：
- `Id`：主键
- `AppUserId`：外键，指向 `AppUsers.Id`
- `Provider`：如 `local_password` / `oidc_school` / `oidc_microsoft` / `github`
- `ProviderUserId`：该 provider 下的稳定唯一身份，如 OIDC `sub`
- `Email`：可选
- `NormalizedEmail`：可选，用于查找和唯一性约束
- `DisplayName`：可选
- `IsEmailVerified`：是否已验证邮箱
- `IsDisabled`：是否禁用该登录身份
- `CreatedAt`
- `UpdatedAt`
- `LastLoginAt`
- `LastLoginIpHash`：可选，若后续做审计可考虑，仅存脱敏值
- `MetadataJson`：可选，小范围扩展字段，不建议承载核心结构
- `ConcurrencyStamp` 或 `RowVersion`：可选，用于并发控制

### 6.2 `LocalCredentials`
建议字段：
- `Id`：主键
- `AuthAccountId`：外键，指向 `AuthAccounts.Id`
- `PasswordHash`
- `PasswordAlgorithm`
- `PasswordUpdatedAt`
- `PasswordLoginEnabled`
- `FailedLoginCount`
- `LockoutUntil`
- `MustResetPassword`：可选
- `CreatedAt`
- `UpdatedAt`

### 6.3 `AuthLoginAudit`（可选）
建议字段：
- `Id`
- `AuthAccountId`
- `AppUserId`
- `Provider`
- `Success`
- `FailureReason`
- `OccurredAt`
- `RemoteIpHash`
- `UserAgentHash`

说明：
- `AuthLoginAudit` 不是第一阶段必须表
- 如实现，建议只记录脱敏信息，不记录 token / 密码 / 原始 claims

## 7. 唯一约束与索引
### 7.1 `AuthAccounts`
建议唯一约束：
- `Provider + ProviderUserId` 唯一

建议索引：
- `AppUserId`
- `NormalizedEmail`
- `Provider`

### 7.2 `LocalCredentials`
建议唯一约束：
- `AuthAccountId` 唯一

### 7.3 本地用户名 / 邮箱约束
当前 `AppUsers.Username` 已是业务用户名。

建议：
- 若 `LocalPassword` 继续沿用当前用户名登录，则：
  - `AppUsers.Username` 继续唯一
- 若未来允许邮箱登录，则：
  - `AuthAccounts.NormalizedEmail` 可在特定 Provider 范围内唯一
  - 不建议一开始就做全局邮箱强唯一，避免与历史导入数据冲突

### 7.4 删除策略
- 不建议物理删除 `AppUsers`
- `AuthAccounts` 优先使用 `IsDisabled=true`
- `LocalCredentials` 可禁用 `PasswordLoginEnabled=false`
- 保留历史 `AppUser.Id`，避免破坏统计、分析、日志外键

## 8. LocalPassword 设计
### 8.1 哈希方案
若采用本地密码：
- 必须使用 ASP.NET Core `PasswordHasher<TUser>` 或同等级方案
- 不允许：
  - 明文密码
  - MD5
  - SHA1
  - 裸 `SHA256`

### 8.2 凭据字段
`LocalCredentials` 至少应包括：
- `PasswordHash`
- `PasswordAlgorithm`
- `PasswordUpdatedAt`
- `PasswordLoginEnabled`

可选增强字段：
- `FailedLoginCount`
- `LockoutUntil`
- `MustResetPassword`

### 8.3 注册策略
当前阶段**不建议开放公开自注册**。

推荐策略：
- 由 admin 预创建 `AppUser`
- 再为其分配本地密码身份
- 或由 teacher/admin 邀请、导入、绑定

### 8.4 密码重置
第一阶段可不实现完整密码重置流，但应列为上线前待办：
- 管理员重置
- 首次登录改密
- 密码轮换与失效策略

## 9. OIDC / OAuth 设计
### 9.1 外部身份字段
对于 OIDC / OAuth，`AuthAccounts` 建议保存：
- `Provider`
- `ProviderUserId`（通常为 OIDC `sub`）
- `Email`
- `NormalizedEmail`
- `DisplayName`
- `IsEmailVerified`
- `LastLoginAt`

### 9.2 不建议入库的内容
默认不建议长期存：
- `AccessToken`
- `RefreshToken`
- `IdToken`
- 完整原始 `claims` JSON

除非后续确实需要调用外部 API，否则不应把这些 token 作为第一阶段设计的一部分。

### 9.3 ClientSecret 边界
OIDC `ClientSecret` 只应存在于：
- env
- secrets 文件
- 安全配置系统

不应存入数据库。

### 9.4 首次登录绑定策略
当前项目阶段推荐：
- **优先“预创建用户 + 首次 OIDC 绑定”**
- 不建议开放“任何外部身份首次登录自动创建平台用户”

更稳妥的做法：
1. admin 预创建 `AppUser`
2. 首次 OIDC 登录时，按白名单邮箱 / 指定标识匹配
3. 绑定到已有 `AppUser`
4. 若找不到匹配，则拒绝或进入人工绑定流程

原因：
- 避免权限失控
- 避免 teacher/admin 角色被错误自动分配
- 更适合校内或私有部署

## 10. Role 与 teacher scope 边界
认证与授权边界应明确分离：
- Auth 负责：**“这个人是谁”**
- Authorization / scope 负责：**“这个人能看什么、管什么”**

### 10.1 Role
`Role` 继续只表达平台级身份：
- `student`
- `teacher`
- `school_leader`
- `admin`

### 10.2 teacher scope
teacher scope 应在后续 `R32` 单独建模，不进入 `AuthAccounts`：
- `CourseTeacher`
- `Class`
- `ClassEnrollment`
- `CourseEnrollment`（可选）

明确结论：
- 不要把 `CourseId`
- 不要把 `ClassId`
- 不要把课程/班级访问范围

直接塞进 `AuthAccounts` 或 `LocalCredentials`。

## 11. 与现有代码兼容性
推荐模型与当前代码兼容性良好，原因如下：
- `CurrentUserService` 仍可继续以 `AppUserId` 为核心
- session 仍只存 `AppUserId`
- analyze 的 `userId` 收敛逻辑不需要改
- `StudentSolution / UserCourseStats / UserKnowledgeState / AnalysisResult / LLMRequestLog` 等外键体系不需要改
- 后续 `AuthController` 只需从“登录凭据 -> `AuthAccounts` -> `AppUserId`”完成映射

换言之：
- 现有业务主表和历史数据可以保持稳定
- 认证实现可以在控制器和认证服务层演进
- 不必为了生产认证重写统计或分析主链路

## 12. Migration 规划
本轮不生成 migration，但建议后续拆分如下：

### 12.1 `R30-c1`
新增：
- `AuthAccounts`
- `LocalCredentials`

影响：
- 对现有业务数据非破坏性
- 不需要修改既有 `AppUsers` 业务主键

### 12.2 `R30-c2`
为 `test_student` 生成 development local identity seed

影响：
- 只新增开发身份映射
- 不破坏现有 `AppUsers`

### 12.3 `R30-c3`
补充唯一索引：
- `Provider + ProviderUserId`
- `AuthAccountId`
- 视策略补充 `NormalizedEmail`

### 12.4 `R30-c4`
可选新增 `AuthLoginAudit`

### 12.5 回滚策略
- 若仅新增认证表，回滚相对简单：
  - 停用新认证代码路径
  - 删除新增表 migration
- 不建议在同一轮中同时重构 `AppUsers`
- 不建议把 `AppUsers` 认证字段和新认证表混合大改，避免回滚困难

### 12.6 数据迁移脚本
第一阶段通常不需要复杂迁移脚本，因为：
- 旧系统没有密码数据
- 也没有外部身份绑定历史
- 主要是为现有 `AppUsers` 建立新的认证映射

## 13. 安全与隐私
### 13.1 密码与 token
- 不存明文密码
- 不存裸哈希
- 默认不存 provider `access_token / refresh_token`
- 不在日志打印：
  - token
  - password hash
  - 原始 claims

### 13.2 个人信息最小化
- `Email` 属于个人信息，应最小化保存
- `DisplayName` 只在确有需要时保存
- `RawClaimsJson` 默认不建议长期存整包

### 13.3 禁用与封禁
- 使用 `IsDisabled`
- 使用 `PasswordLoginEnabled`
- 可选 `LockoutUntil`

而不是物理删除账号

### 13.4 删除策略
对业务用户建议：
- 优先 disable / soft delete
- 避免直接删除 `AppUser`

原因：
- 历史分析结果、统计、日志仍需保持可追溯

## 14. 后续实施路线
- `R30-c`：当前数据模型设计
- `R30-c-impl`：新增实体与 migration
- `R30-d`：`LocalPassword` MVP 或 `Oidc` MVP 二选一
- `R30-e`：`AuthController` 按 `AuthMode` 分流
- `R30-f`：前端 `login.html` 适配新认证入口
- `R30-g`：标准 Authorization / policy 初步接入
- `R32`：teacher / class / course scope

推荐顺序：
1. 先做 `AuthAccounts + LocalCredentials` 数据模型落地
2. 然后在 `LocalPassword` 与 `Oidc` 之间选择一条先实现
3. 最后再做 teacher scope 和标准 Authorization

## 15. 决策结论
### 15.1 明确建议
- **继续使用 `AppUsers` 作为业务用户主表。**
- **推荐采用 `AppUsers + AuthAccounts + 可选 LocalCredentials` 的组合。**
- **生产最终优先 OIDC。**
- **LocalPassword 可作为过渡 / 私有部署方案。**
- **当前不建议开放公开自注册。**
- **teacher scope 不应混入 `AuthAccounts`。**

### 15.2 简化决策
- 认证身份：放到 `AuthAccounts`
- 本地密码：放到 `LocalCredentials`
- 平台角色：继续放到 `AppUsers.Role`
- 课程/班级授权：后续放到 `R32` scope 模型

### 15.3 对当前项目的意义
这套模型最大的价值不是“立刻上线认证”，而是保证后续做：
- `LocalPassword`
- `Oidc`
- teacher/admin scope
- 标准 authorization

时，不需要推翻现有 `AppUser.Id` 业务主键体系，也不需要重写已跑通的 analyze / OCR / stats 主链路。
