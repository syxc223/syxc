# 游戏服务器部署指南

## 架构说明

```
Unity 客户端 ──HTTP──→ 云服务器 (Node.js API) ──SQLite──→ 数据库
                ↓
            Netcode UDP 7777 (玩家直连 Host)
```

- 云服务器只负责 **账号注册/登录** 和 **房间管理**
- 游戏数据通过 Netcode UDP 直连，不经过服务器
- 创建房间的玩家做 Host，其他人直连 Host IP

---

## 一、云服务器操作步骤 (Alibaba Cloud Linux / CentOS / RHEL)

### 1. 安装 Node.js 20 LTS

```bash
curl -fsSL https://rpm.nodesource.com/setup_20.x | sudo -E bash -
sudo dnf install -y nodejs
```

验证安装：
```bash
node -v   # 应显示 v20.x.x
npm -v    # 应显示 10.x.x
```

### 2. 上传服务器代码

将项目中的 `server/` 目录上传到云服务器，例如上传到 `/home/admin/game-server/`。

可以使用 scp：
```bash
# 在本地执行（替换用户名和 IP）
scp -r server/ admin@你的服务器IP:/home/admin/game-server/
```

### 3. 安装依赖

```bash
cd /home/admin/game-server
npm install
```

### 4. 配置环境变量

```bash
cp .env.example .env
```

编辑 `.env` 文件，修改 JWT_SECRET：
```bash
nano .env
```

内容如下（密钥至少 32 个字符，否则服务器拒绝启动）：
```
JWT_SECRET=<随机生成的至少32字符密钥>
PORT=3000
```

> ⚠️ 用以下命令生成随机密钥后粘贴进去（不要把 .env.example 原样复制）：
> ```bash
> node -e "console.log(require('crypto').randomBytes(48).toString('hex'))"
> ```

### 5. 测试启动

```bash
npm start
```

看到以下输出表示成功：
```
Game API Server running on port 3000
Health check: http://localhost:3000/api/health
```

按 Ctrl+C 停止。

### 6. 使用 pm2 保持后台运行

```bash
# 安装 pm2
sudo npm install -g pm2

# 启动服务
pm2 start server.js --name "game-api"

# 保存并设置开机自启
pm2 save
pm2 startup
# 按提示执行返回的命令
```

常用 pm2 命令：
```bash
pm2 status          # 查看运行状态
pm2 logs game-api   # 查看日志
pm2 restart game-api  # 重启服务
pm2 stop game-api    # 停止服务
```

### 7. 开放防火墙端口

```bash
# 开放 API 端口 3000
sudo firewall-cmd --permanent --add-port=3000/tcp
sudo firewall-cmd --reload

# 如果 firewalld 未运行，先启动：
sudo systemctl start firewalld
sudo systemctl enable firewalld
sudo firewall-cmd --permanent --add-port=3000/tcp
sudo firewall-cmd --reload
```

> 注意：游戏端口 7777 (UDP) **不需要** 在服务器开放，因为玩家是直连 Host 的。

### 8. 验证服务可访问

在本地浏览器访问：
```
http://你的服务器IP:3000/api/health
```

应返回：
```json
{"success":true,"message":"Server is running"}
```

---

## 二、Unity 客户端配置

### 修改服务器地址

打开 `Assets/Scripts/Network/ApiClient.cs`，修改第 12 行：

```csharp
// 将 127.0.0.1 改为你的云服务器公网 IP
public const string ServerUrl = "http://你的服务器IP:3000";
```

例如：
```csharp
public const string ServerUrl = "http://123.45.67.89:3000";
```

---

## 三、测试流程

### 1. 启动后端服务
确保云服务器上的 API 正在运行。

### 2. Unity 中测试
1. 打开 Unity，进入 Play 模式
2. 看到登录界面
3. 点击"注册"，输入用户名和密码，注册新账号
4. 注册成功后自动进入房间大厅
5. 点击"创建房间"，输入房间名
6. 成为 Host，进入游戏

### 3. 双人测试（局域网）
1. 在另一台电脑上打开同一 Unity 项目
2. 登录（用不同账号）
3. 在房间列表中看到对方创建的房间
4. 点击"加入"
5. 连接到 Host，进入游戏

---

## 四、API 接口一览

| 方法 | 路径 | 说明 | 认证 |
|------|------|------|------|
| GET | `/api/health` | 健康检查 | 否 |
| POST | `/api/auth/register` | 注册 | 否 |
| POST | `/api/auth/login` | 登录 | 否 |
| GET | `/api/rooms` | 获取房间列表 | 是 |
| POST | `/api/rooms` | 创建房间 | 是 |
| POST | `/api/rooms/:id/join` | 加入房间 | 是 |
| POST | `/api/rooms/:id/leave` | 离开房间 | 是 |
| DELETE | `/api/rooms/:id` | 关闭房间 | 是 |

### 使用 curl 测试

```bash
# 健康检查
curl http://服务器IP:3000/api/health

# 注册
curl -X POST http://服务器IP:3000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"123456"}'

# 登录
curl -X POST http://服务器IP:3000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"123456"}'

# 获取房间列表（需要 token）
curl http://服务器IP:3000/api/rooms \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## 五、常见问题

### Q: 服务器启动报错 "Cannot find module"
A: 确保在 server 目录下执行了 `npm install`

### Q: Unity 连接服务器失败
A: 检查：
1. 服务器是否在运行 (`pm2 status`)
2. 防火墙是否开放 3000 端口 (`sudo ufw status`)
3. ApiClient.cs 中的 IP 地址是否正确

### Q: 房间列表为空
A: 需要先有人创建房间。创建房间后刷新列表即可看到。

### Q: 加入房间后连接失败
A: Host 的 IP 需要能被其他玩家访问：
- 局域网：确保在同一网络
- 互联网：Host 需要公网 IP 或端口转发（7777 UDP）

### Q: 数据库在哪
A: SQLite 数据库文件在 `server/data/game.db`，无需额外安装数据库软件。
