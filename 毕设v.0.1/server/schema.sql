-- ============================================================
-- 数据库建表脚本 schema.sql（SQLite）
-- 用途：毕业设计"数据库对应文件"交付物，完整建立系统 8 张数据表
-- 使用：sqlite3 data/game.db < schema.sql
--       （服务端 db.js 启动时也会自动建表，本脚本供独立执行/核对）
-- 说明：全部使用 IF NOT EXISTS，脚本可重复执行，不会破坏已有数据
-- ============================================================

PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

-- 表3-1 用户表：账号业务数据
CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY AUTOINCREMENT,          -- 用户唯一标识，主键，自增
  username TEXT UNIQUE NOT NULL,                 -- 用户名，唯一，3~20字符
  password_hash TEXT NOT NULL,                   -- bcrypt加盐哈希值，不保存明文
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP  -- 注册时间
);

-- 表3-2 房间表：联机房间业务数据
CREATE TABLE IF NOT EXISTS rooms (
  id INTEGER PRIMARY KEY AUTOINCREMENT,          -- 房间唯一标识，主键，自增
  name TEXT NOT NULL,                            -- 房间名称，不超过32字符
  host_user_id INTEGER NOT NULL,                 -- 房主用户ID，外键关联 users(id)
  host_ip TEXT NOT NULL,                         -- 房主主机IP，供队友UDP直连
  host_username TEXT NOT NULL,                   -- 房主用户名
  max_players INTEGER DEFAULT 2,                 -- 房间人数上限
  current_players INTEGER DEFAULT 1,             -- 当前人数（房主计入1人）
  status TEXT DEFAULT 'waiting',                 -- 房间状态：waiting/playing/closed
  last_activity DATETIME DEFAULT CURRENT_TIMESTAMP, -- 最近活跃时间（过期清理依据）
  created_at DATETIME DEFAULT CURRENT_TIMESTAMP, -- 创建时间
  FOREIGN KEY (host_user_id) REFERENCES users(id)
);

-- 表3-3 房间成员表：用户与房间的加入关系（联合主键）
CREATE TABLE IF NOT EXISTS room_members (
  room_id INTEGER NOT NULL,                      -- 房间ID，联合主键
  user_id INTEGER NOT NULL,                      -- 用户ID，联合主键
  joined_at DATETIME DEFAULT CURRENT_TIMESTAMP,  -- 加入时间
  PRIMARY KEY (room_id, user_id),
  FOREIGN KEY (room_id) REFERENCES rooms(id) ON DELETE CASCADE
);

-- 表3-4 玩家表：对局玩家记录
CREATE TABLE IF NOT EXISTS players (
  player_id INTEGER PRIMARY KEY AUTOINCREMENT,  -- 玩家编号，对局记录标识
  room_id INTEGER NOT NULL,                      -- 所属房间ID，外键关联 rooms(id)
  user_id INTEGER NOT NULL,                      -- 对应的用户ID
  position TEXT NOT NULL DEFAULT '',             -- 位置坐标（x,y,z格式）
  rotation REAL NOT NULL DEFAULT 0,              -- 朝向角度
  is_moving INTEGER NOT NULL DEFAULT 0,          -- 是否移动中（0/1）
  move_x REAL NOT NULL DEFAULT 0,                -- 移动方向X分量
  move_y REAL NOT NULL DEFAULT 0,                -- 移动方向Y分量
  FOREIGN KEY (room_id) REFERENCES rooms(id) ON DELETE CASCADE
);

-- 表3-5 关卡任务表：关卡配置
CREATE TABLE IF NOT EXISTS missions (
  mission_id INTEGER PRIMARY KEY AUTOINCREMENT,  -- 任务ID，关卡编号
  mission_name TEXT NOT NULL,                     -- 任务名称
  objective TEXT NOT NULL,                        -- 任务目标提示文本
  spawn_p1 TEXT NOT NULL,                         -- 玩家一出生点坐标（x,y,z格式）
  spawn_p2 TEXT NOT NULL                          -- 玩家二出生点坐标（x,y,z格式）
);

-- 表3-6 机关表：关卡机关配置
CREATE TABLE IF NOT EXISTS mechanisms (
  mech_id INTEGER PRIMARY KEY AUTOINCREMENT,     -- 机关ID，场景内编号
  mission_id INTEGER NOT NULL,                   -- 所属关卡ID，外键关联 missions
  mech_type TEXT NOT NULL,                       -- 机关类型（压力板、解谜门等）
  mech_state INTEGER NOT NULL DEFAULT 0,          -- 机关状态（0/1：按压/开合）
  linked_doors TEXT NOT NULL DEFAULT '[]',       -- 联动的解谜门编号列表（JSON数组）
  FOREIGN KEY (mission_id) REFERENCES missions(mission_id) ON DELETE CASCADE
);

-- 表3-7 道具表：关卡道具配置
CREATE TABLE IF NOT EXISTS items (
  item_id INTEGER PRIMARY KEY AUTOINCREMENT,     -- 道具ID
  mission_id INTEGER NOT NULL,                   -- 所属关卡ID，外键关联 missions
  item_type TEXT NOT NULL,                        -- 道具类型（可拾取方块）
  position TEXT NOT NULL,                         -- 初始位置坐标（x,y,z格式）
  holder_id INTEGER,                             -- 持有者玩家编号，未被持有时为空
  FOREIGN KEY (mission_id) REFERENCES missions(mission_id) ON DELETE CASCADE
);

-- 表3-8 聊天消息表：聊天记录
CREATE TABLE IF NOT EXISTS chat_messages (
  msg_id INTEGER PRIMARY KEY AUTOINCREMENT,      -- 消息编号
  sender_id INTEGER NOT NULL,                    -- 发送者网络标识
  sender TEXT NOT NULL,                          -- 发送者用户名
  content TEXT NOT NULL,                         -- 消息内容
  send_time DATETIME DEFAULT CURRENT_TIMESTAMP   -- 发送时间
);
