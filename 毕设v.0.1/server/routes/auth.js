const express = require('express');
const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const db = require('../db');

const router = express.Router();

// POST /api/auth/register
router.post('/register', (req, res) => {
  const username = String(req.body.username || '').trim();
  const password = req.body.password;

  if (!username || !password) {
    return res.status(400).json({ success: false, error: '用户名和密码不能为空' });
  }
  if (username.length < 3 || username.length > 20) {
    return res.status(400).json({ success: false, error: '用户名长度需为 3-20 个字符' });
  }
  if (password.length < 6 || password.length > 32) {
    return res.status(400).json({ success: false, error: '密码长度需为 6-32 个字符' });
  }

  const existing = db.prepare('SELECT id FROM users WHERE username = ?').get(username);
  if (existing) {
    return res.status(409).json({ success: false, error: '用户名已存在' });
  }

  const hash = bcrypt.hashSync(password, 10);
  const result = db.prepare('INSERT INTO users (username, password_hash) VALUES (?, ?)').run(username, hash);

  const token = jwt.sign(
    { userId: result.lastInsertRowid, username },
    process.env.JWT_SECRET,
    { expiresIn: '24h' }
  );

  res.json({ success: true, token, username });
});

// POST /api/auth/login
router.post('/login', (req, res) => {
  const username = String(req.body.username || '').trim();
  const password = req.body.password;

  if (!username || !password) {
    return res.status(400).json({ success: false, error: '用户名和密码不能为空' });
  }

  const user = db.prepare('SELECT id, username, password_hash FROM users WHERE username = ?').get(username);
  if (!user) {
    return res.status(401).json({ success: false, error: '用户名或密码错误' });
  }

  if (!bcrypt.compareSync(password, user.password_hash)) {
    return res.status(401).json({ success: false, error: '用户名或密码错误' });
  }

  const token = jwt.sign(
    { userId: user.id, username: user.username },
    process.env.JWT_SECRET,
    { expiresIn: '24h' }
  );

  res.json({ success: true, token, username: user.username });
});

module.exports = router;
