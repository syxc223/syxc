const Database = require('better-sqlite3');
const path = require('path');
const fs = require('fs');

// 必须在打开数据库之前确保 data 目录存在（全新部署时否则会直接崩溃）
const dataDir = path.join(__dirname, 'data');
if (!fs.existsSync(dataDir)) {
  fs.mkdirSync(dataDir, { recursive: true });
}

const db = new Database(path.join(dataDir, 'game.db'));

db.pragma('journal_mode = WAL');
db.pragma('foreign_keys = ON');

db.exec(`
  CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
  );

  CREATE TABLE IF NOT EXISTS rooms (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    host_user_id INTEGER NOT NULL,
    host_ip TEXT NOT NULL,
    host_username TEXT NOT NULL,
    max_players INTEGER DEFAULT 2,
    current_players INTEGER DEFAULT 1,
    status TEXT DEFAULT 'waiting',
    last_activity DATETIME DEFAULT CURRENT_TIMESTAMP,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (host_user_id) REFERENCES users(id)
  );

  CREATE TABLE IF NOT EXISTS room_members (
    room_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    joined_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (room_id, user_id),
    FOREIGN KEY (room_id) REFERENCES rooms(id) ON DELETE CASCADE
  );

  CREATE TABLE IF NOT EXISTS players (
    player_id INTEGER PRIMARY KEY AUTOINCREMENT,
    room_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    position TEXT NOT NULL DEFAULT '',
    rotation REAL NOT NULL DEFAULT 0,
    is_moving INTEGER NOT NULL DEFAULT 0,
    move_x REAL NOT NULL DEFAULT 0,
    move_y REAL NOT NULL DEFAULT 0,
    FOREIGN KEY (room_id) REFERENCES rooms(id) ON DELETE CASCADE
  );

  CREATE TABLE IF NOT EXISTS missions (
    mission_id INTEGER PRIMARY KEY AUTOINCREMENT,
    mission_name TEXT NOT NULL,
    objective TEXT NOT NULL,
    spawn_p1 TEXT NOT NULL,
    spawn_p2 TEXT NOT NULL
  );

  CREATE TABLE IF NOT EXISTS mechanisms (
    mech_id INTEGER PRIMARY KEY AUTOINCREMENT,
    mission_id INTEGER NOT NULL,
    mech_type TEXT NOT NULL,
    mech_state INTEGER NOT NULL DEFAULT 0,
    linked_doors TEXT NOT NULL DEFAULT '[]',
    FOREIGN KEY (mission_id) REFERENCES missions(mission_id) ON DELETE CASCADE
  );

  CREATE TABLE IF NOT EXISTS items (
    item_id INTEGER PRIMARY KEY AUTOINCREMENT,
    mission_id INTEGER NOT NULL,
    item_type TEXT NOT NULL,
    position TEXT NOT NULL,
    holder_id INTEGER,
    FOREIGN KEY (mission_id) REFERENCES missions(mission_id) ON DELETE CASCADE
  );

  CREATE TABLE IF NOT EXISTS chat_messages (
    msg_id INTEGER PRIMARY KEY AUTOINCREMENT,
    sender_id INTEGER NOT NULL,
    sender TEXT NOT NULL,
    content TEXT NOT NULL,
    send_time DATETIME DEFAULT CURRENT_TIMESTAMP
  );
`);

// 迁移：为已存在的 rooms 表补充 last_activity 列
const roomCols = db.prepare('PRAGMA table_info(rooms)').all().map(c => c.name);
if (!roomCols.includes('last_activity')) {
  db.exec(`ALTER TABLE rooms ADD COLUMN last_activity DATETIME DEFAULT CURRENT_TIMESTAMP`);
}

// 关卡配置种子数据（INSERT OR IGNORE 幂等，可重复启动；与 seed.sql 保持一致）
db.exec(`
  INSERT OR IGNORE INTO missions (mission_id, mission_name, objective, spawn_p1, spawn_p2) VALUES
    (1, '第一关：协作之门', '目标：一人踩压力板开门，另一人通过，交替前进到达出口', '(-3, 1, 0)', '(3, 1, 0)'),
    (2, '第二关：机关重重', '目标：协作搬运方块压住机关，引导激光解开谜题', '(-4, 1, -2)', '(4, 1, -2)');

  INSERT OR IGNORE INTO mechanisms (mech_id, mission_id, mech_type, mech_state, linked_doors) VALUES
    (1, 1, '压力板', 0, '[2]'),
    (2, 1, '解谜门', 0, '[]'),
    (3, 1, '压力板', 0, '[4]'),
    (4, 1, '解谜门', 0, '[]'),
    (5, 2, '压力板', 0, '[6]'),
    (6, 2, '解谜门', 0, '[]'),
    (7, 2, '激光发射器', 0, '[]'),
    (8, 2, '激光接收器', 0, '[6]');

  INSERT OR IGNORE INTO items (item_id, mission_id, item_type, position, holder_id) VALUES
    (1, 1, '可拾取方块', '(0, 0.5, 5)', NULL),
    (2, 2, '可拾取方块', '(-1, 0.5, 4)', NULL);
`);

module.exports = db;
