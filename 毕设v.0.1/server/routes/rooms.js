const express = require('express');
const net = require('net');
const db = require('../db');
const authMiddleware = require('../middleware/auth');

const router = express.Router();

// 所有房间路由都需要认证
router.use(authMiddleware);

function touchRoom(roomId) {
  db.prepare(`UPDATE rooms SET last_activity = CURRENT_TIMESTAMP WHERE id = ?`).run(roomId);
}

// GET /api/rooms — 获取房间列表
router.get('/', (req, res) => {
  const rooms = db.prepare(`
    SELECT id, name, host_username, host_ip, current_players, max_players, status, created_at
    FROM rooms
    WHERE status != 'closed'
    ORDER BY created_at DESC
  `).all();

  res.json({ success: true, rooms });
});

// POST /api/rooms — 创建房间
router.post('/', (req, res) => {
  const { name, hostIp } = req.body;

  if (!name || !hostIp) {
    return res.status(400).json({ success: false, error: '房间名和主机IP不能为空' });
  }
  if (String(name).length > 32) {
    return res.status(400).json({ success: false, error: '房间名长度不能超过 32 个字符' });
  }
  if (!net.isIP(String(hostIp))) {
    return res.status(400).json({ success: false, error: '主机IP格式不正确' });
  }

  // 只清理该用户之前处于等待状态的房间，不影响进行中的房间
  db.prepare(`DELETE FROM rooms WHERE host_user_id = ? AND status = 'waiting'`).run(req.user.userId);

  const result = db.prepare(`
    INSERT INTO rooms (name, host_user_id, host_ip, host_username, max_players, current_players, status)
    VALUES (?, ?, ?, ?, 2, 1, 'waiting')
  `).run(String(name), req.user.userId, String(hostIp), req.user.username);

  // 房主自己计入成员
  db.prepare(`INSERT INTO room_members (room_id, user_id) VALUES (?, ?)`)
    .run(result.lastInsertRowid, req.user.userId);

  // 房主作为玩家1写入对局记录（仅作持久记录，写入失败不影响建房流程）
  try {
    db.prepare(`INSERT INTO players (room_id, user_id, position, rotation, is_moving, move_x, move_y)
                VALUES (?, ?, '', 0, 0, 0, 0)`)
      .run(result.lastInsertRowid, req.user.userId);
  } catch (e) { /* 对局记录失败时静默跳过 */ }

  res.json({ success: true, roomId: result.lastInsertRowid });
});

// POST /api/rooms/:id/join — 加入房间
router.post('/:id/join', (req, res) => {
  const roomId = req.params.id;

  const room = db.prepare('SELECT * FROM rooms WHERE id = ? AND status != ?').get(roomId, 'closed');
  if (!room) {
    return res.status(404).json({ success: false, error: '房间不存在或已关闭' });
  }

  if (room.current_players >= room.max_players) {
    return res.status(403).json({ success: false, error: '房间已满' });
  }

  db.prepare(`INSERT OR IGNORE INTO room_members (room_id, user_id) VALUES (?, ?)`)
    .run(roomId, req.user.userId);

  // 更新房间人数
  db.prepare('UPDATE rooms SET current_players = current_players + 1, status = ? WHERE id = ?')
    .run('playing', roomId);
  touchRoom(roomId);

  // 队友作为玩家2写入对局记录（仅作持久记录，写入失败不影响加入流程）
  try {
    db.prepare(`INSERT INTO players (room_id, user_id, position, rotation, is_moving, move_x, move_y)
                VALUES (?, ?, '', 0, 0, 0, 0)`)
      .run(roomId, req.user.userId);
  } catch (e) { /* 对局记录失败时静默跳过 */ }

  res.json({ success: true, hostIp: room.host_ip, host_username: room.host_username });
});

// POST /api/rooms/:id/leave — 离开房间
router.post('/:id/leave', (req, res) => {
  const roomId = req.params.id;

  const room = db.prepare('SELECT * FROM rooms WHERE id = ?').get(roomId);
  if (!room) {
    return res.status(404).json({ success: false, error: '房间不存在' });
  }

  // 只有房间成员才能离开（防止任意用户修改他人房间）
  const membership = db.prepare('SELECT * FROM room_members WHERE room_id = ? AND user_id = ?')
    .get(roomId, req.user.userId);
  if (!membership) {
    return res.status(404).json({ success: false, error: '你不在该房间中' });
  }

  if (room.host_user_id === req.user.userId) {
    // 房主离开 → 删除房间（级联删除成员记录）
    db.prepare('DELETE FROM rooms WHERE id = ?').run(roomId);
  } else {
    // 成员离开 → 减少人数，若变为0则删除
    db.prepare('DELETE FROM room_members WHERE room_id = ? AND user_id = ?').run(roomId, req.user.userId);
    db.prepare(`UPDATE rooms SET current_players = MAX(current_players - 1, 0), status = ? WHERE id = ?`)
      .run('waiting', roomId);
    touchRoom(roomId);
    const after = db.prepare('SELECT current_players FROM rooms WHERE id = ?').get(roomId);
    if (after && after.current_players <= 0) {
      db.prepare('DELETE FROM rooms WHERE id = ?').run(roomId);
    }
  }

  res.json({ success: true });
});

// DELETE /api/rooms/:id — 关闭房间 (仅房主)
router.delete('/:id', (req, res) => {
  const roomId = req.params.id;

  const room = db.prepare('SELECT * FROM rooms WHERE id = ?').get(roomId);
  if (!room) {
    return res.status(404).json({ success: false, error: '房间不存在' });
  }

  if (room.host_user_id !== req.user.userId) {
    return res.status(403).json({ success: false, error: '只有房主可以关闭房间' });
  }

  db.prepare('DELETE FROM rooms WHERE id = ?').run(roomId);

  res.json({ success: true });
});

module.exports = router;
