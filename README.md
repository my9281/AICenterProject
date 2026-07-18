# AICenterProject 用户服务

基于 ASP.NET Core 8 的 Web API，提供用户注册、登录、当前用户查询和超级用户查看用户列表。

## 运行

生产环境不要使用仓库中的默认超级用户密码。推荐通过环境变量传入：

```powershell
$env:Superuser__Username = "admin"
$env:Superuser__Password = "替换为足够长的随机密码"
dotnet run --project AICenterProject
```

默认监听 `http://0.0.0.0:5080`。首次启动时会在 `App_Data/users.json` 创建用户数据库和配置的超级用户。

## API

- `POST /api/auth/register`：注册普通用户
- `POST /api/auth/login`：登录并返回有效期 12 小时的 Bearer Token
- `GET /api/users/me`：获取当前用户，需请求头 `Authorization: Bearer <token>`
- `GET /api/admin/users`：列出用户，仅超级用户可访问

注册示例：

```json
{ "username": "alice", "password": "StrongPassword123!", "note": "测试用户" }
```

用户返回字段为 `userId`、`username`、`note`、`superuser`、`createdAt`；密码只保存 PBKDF2 哈希且永不返回。

> 当前 Token 保存在进程内，服务重启后需重新登录。单机部署可直接使用；多实例或需要撤销/审计时应替换为数据库及集中式 Token/会话存储。
