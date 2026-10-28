-- ============================================================
-- 关卡配置种子数据 seed.sql（SQLite）
-- 用途：向关卡配置表（missions/mechanisms/items）写入关卡内容数据
-- 使用：sqlite3 data/game.db < seed.sql（需先执行 schema.sql 或启动过服务端）
-- 说明：INSERT OR IGNORE 幂等，可重复执行，不会产生重复数据
-- 注：服务端 db.js 启动时会自动写入相同种子数据，本脚本供独立部署/核对
-- ============================================================

-- 关卡任务配置：两个关卡的名称、目标与双方出生点
INSERT OR IGNORE INTO missions (mission_id, mission_name, objective, spawn_p1, spawn_p2) VALUES
  (1, '第一关：协作之门', '目标：一人踩压力板开门，另一人通过，交替前进到达出口', '(-3, 1, 0)', '(3, 1, 0)'),
  (2, '第二关：机关重重', '目标：协作搬运方块压住机关，引导激光解开谜题', '(-4, 1, -2)', '(4, 1, -2)');

-- 机关配置：
-- 第一关：两组"压力板—解谜门"联动，构成交替前进协作谜题
-- 第二关：压力板联动门 + 激光发射器/接收器（接收器命中后联动开门）
INSERT OR IGNORE INTO mechanisms (mech_id, mission_id, mech_type, mech_state, linked_doors) VALUES
  (1, 1, '压力板', 0, '[2]'),
  (2, 1, '解谜门', 0, '[]'),
  (3, 1, '压力板', 0, '[4]'),
  (4, 1, '解谜门', 0, '[]'),
  (5, 2, '压力板', 0, '[6]'),
  (6, 2, '解谜门', 0, '[]'),
  (7, 2, '激光发射器', 0, '[]'),
  (8, 2, '激光接收器', 0, '[6]');

-- 道具配置：两个关卡各提供一个可拾取方块，初始未被持有（holder_id 为空）
INSERT OR IGNORE INTO items (item_id, mission_id, item_type, position, holder_id) VALUES
  (1, 1, '可拾取方块', '(0, 0.5, 5)', NULL),
  (2, 2, '可拾取方块', '(-1, 0.5, 4)', NULL);
