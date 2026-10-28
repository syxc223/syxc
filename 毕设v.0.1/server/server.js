require('dotenv').config();
const express = require('express');
const cors = require('cors');
const rateLimit = require('express-rate-limit');

// 启动前校验 JWT 密钥强度，防止使用示例密钥导致令牌可被伪造
if (!process.env.JWT_SECRET || process.env.JWT_SECRET.startsWith('change_this') || process.env.JWT_SECRET.length < 32) {
  console.error('[FATAL] JWT_SECRET 缺失或过弱：请在 .env 中设置一个至少 32 字符的随机密钥（参考 .env.example）');
  process.exit(1);
}

const authRoutes = require('./routes/auth');
const roomRoutes = require('./routes/rooms');
const db = require('./db');

const app = express();
const PORT = process.env.PORT || 3000;

// 中间件
app.use(cors());
app.use(express.json());

// 限流：登录/注册 10 次/分钟/IP；房间接口 60 次/分钟/IP
const authLimiter = rateLimit({
  windowMs: 60 * 1000,
  limit: 10,
  standardHeaders: true,
  legacyHeaders: false,
  message: { success: false, error: '请求过于频繁，请稍后再试' },
});
const roomsLimiter = rateLimit({
  windowMs: 60 * 1000,
  limit: 60,
  standardHeaders: true,
  legacyHeaders: false,
  message: { success: false, error: '请求过于频繁，请稍后再试' },
});

// 路由
app.use('/api/auth', authLimiter, authRoutes);
app.use('/api/rooms', roomsLimiter, roomRoutes);

// 健康检查
app.get('/api/health', (req, res) => {
  res.json({ success: true, message: 'Server is running' });
});

// ============================================================
// TCP Echo 测试服务器（端口 3001）
// 用于验证 Unity 客户端到服务器的 TCP 连通性
// 流程：连接 → 发送消息 → 服务器原样回传 → 发送 "exit" 断开
// ============================================================
const ECHO_PORT = process.env.ECHO_PORT || 3001;
const echoServer = net.createServer((socket) => {
  const addr = `${socket.remoteAddress}:${socket.remotePort}`;
  console.log(`[Echo] 客户端已连接: ${addr}`);

  socket.write(`[Echo] 连接成功！请发送消息（发送 "exit" 断开连接）\n`);

  socket.on('data', (data) => {
    const msg = data.toString().trim();
    console.log(`[Echo] 收到来自 ${addr} 的消息: "${msg}"`);

    if (msg.toLowerCase() === 'exit') {
      socket.write(`[Echo] 收到 "exit"，即将断开连接。再见！\n`);
      console.log(`[Echo] 客户端 ${addr} 请求断开`);
      socket.end();
    } else {
      socket.write(`[Echo] 收到: "${msg}"，已原样返回\n`);
    }
  });

  socket.on('end', () => {
    console.log(`[Echo] 客户端断开: ${addr}`);
  });

  socket.on('error', (err) => {
    console.error(`[Echo] 连接错误 ${addr}:`, err.message);
  });
});

echoServer.listen(ECHO_PORT, '0.0.0.0', () => {
  console.log(`Echo test server running on port ${ECHO_PORT}`);
  console.log(`  Unity 测试流程: 连接 → 发送 "Test1" → 收到回显 → 发送 "Unity Echo" → 收到回显 → 发送 "exit" → 断开`);
});

// 房间过期清扫：删除超过 10 分钟无活动的房间（含成员记录）
const ROOM_IDLE_TIMEOUT_MS = 10 * 60 * 1000;
const SWEEP_INTERVAL_MS = 5 * 60 * 1000;

function sweepStaleRooms() {
  const cutoff = new Date(Date.now() - ROOM_IDLE_TIMEOUT_MS).toISOString();
  const result = db.prepare(
    `DELETE FROM rooms WHERE status != 'closed' AND last_activity < ?`
  ).run(cutoff);
  if (result.changes > 0) {
    console.log(`[Sweep] 清理了 ${result.changes} 个过期房间`);
  }
}

setInterval(sweepStaleRooms, SWEEP_INTERVAL_MS);

// 未匹配路由也返回 JSON（避免客户端拿到 HTML）
app.use((req, res) => {
  res.status(404).json({ success: false, error: '接口不存在' });
});

// 统一 JSON 错误中间件（不泄露堆栈，客户端只收到 JSON）
app.use((err, req, res, next) => {
  console.error('[Error]', err);
  res.status(500).json({ success: false, error: '服务器内部错误' });
});

// 启动
app.listen(PORT, '0.0.0.0', () => {
  console.log(`Game API Server running on port ${PORT}`);
  console.log(`Health check: http://localhost:${PORT}/api/health`);
});
