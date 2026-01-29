using System.Collections.Generic;
using UnityEngine;
using WanWanKan.Character;

namespace WanWanKan.Map
{
    /// <summary>
    /// 房间内容生成器 - 根据房间类型和楼层生成房间内容
    /// </summary>
    public static class RoomContentGenerator
    {
        /// <summary>
        /// 生成房间内容
        /// </summary>
        public static void GenerateRoomContent(Room room, int floor, EnemyDatabase enemyDatabase = null)
        {
            if (room == null) return;

            switch (room.Type)
            {
                case RoomType.Start:
                    // 起点房间不需要内容
                    break;

                case RoomType.Battle:
                    GenerateBattleRoomContent(room, floor, enemyDatabase);
                    break;

                case RoomType.Treasure:
                    GenerateTreasureRoomContent(room, floor);
                    break;

                case RoomType.Event:
                    GenerateEventRoomContent(room, floor);
                    break;

                case RoomType.Shop:
                    GenerateShopRoomContent(room, floor);
                    break;

                case RoomType.Rest:
                    GenerateRestRoomContent(room, floor);
                    break;

                case RoomType.Boss:
                    GenerateBossRoomContent(room, floor, enemyDatabase);
                    break;

                case RoomType.Sacrifice:
                    GenerateSacrificeRoomContent(room, floor);
                    break;

                case RoomType.Secret:
                    GenerateSecretRoomContent(room, floor);
                    break;
            }
        }

        /// <summary>
        /// 生成战斗房间内容
        /// </summary>
        private static void GenerateBattleRoomContent(Room room, int floor, EnemyDatabase enemyDatabase)
        {
            BattleRoomData battleData = new BattleRoomData();

            // 根据楼层生成敌人
            int enemyCount = GetEnemyCountForFloor(floor);
            List<string> enemyIds = new List<string>();

            if (enemyDatabase != null)
            {
                // 使用数据库随机选择敌人
                for (int i = 0; i < enemyCount; i++)
                {
                    EnemyData enemy = enemyDatabase.GetRandomEnemyForFloor(floor);
                    if (enemy != null)
                    {
                        enemyIds.Add(enemy.Id);
                    }
                }
            }
            else
            {
                // 如果没有数据库，使用默认敌人配置
                for (int i = 0; i < enemyCount; i++)
                {
                    enemyIds.Add($"enemy_floor{floor}_{i}");
                }
            }

            battleData.EnemyIds = enemyIds;

            // 计算战斗奖励（基于楼层）
            battleData.Reward = new BattleReward
            {
                Experience = 10 + floor * 5,
                Gold = 5 + floor * 3,
                ItemIds = new List<string>()
            };

            // 小概率掉落物品
            if (Random.value < 0.2f)
            {
                battleData.Reward.ItemIds.Add($"item_floor{floor}");
            }

            room.Data = battleData;
        }

        /// <summary>
        /// 生成BOSS房间内容
        /// </summary>
        private static void GenerateBossRoomContent(Room room, int floor, EnemyDatabase enemyDatabase)
        {
            BattleRoomData bossData = new BattleRoomData();

            // BOSS房间通常只有1个BOSS敌人
            string bossId = $"boss_floor{floor}";
            bossData.EnemyIds = new List<string> { bossId };

            // BOSS奖励更丰厚
            bossData.Reward = new BattleReward
            {
                Experience = 50 + floor * 20,
                Gold = 50 + floor * 15,
                ItemIds = new List<string> { $"boss_reward_floor{floor}" }
            };

            room.Data = bossData;
        }

        /// <summary>
        /// 生成宝藏房间内容
        /// </summary>
        private static void GenerateTreasureRoomContent(Room room, int floor)
        {
            TreasureRoomData treasureData = new TreasureRoomData();
            
            // 根据楼层生成金币和物品
            treasureData.Gold = 20 + floor * 10 + Random.Range(0, 20);
            treasureData.ItemIds = new List<string>();

            // 随机生成1-3个物品
            int itemCount = Random.Range(1, 4);
            for (int i = 0; i < itemCount; i++)
            {
                treasureData.ItemIds.Add($"treasure_item_floor{floor}_{i}");
            }

            room.Data = treasureData;
        }

        /// <summary>
        /// 生成事件房间内容
        /// </summary>
        private static void GenerateEventRoomContent(Room room, int floor)
        {
            EventRoomData eventData = new EventRoomData();
            
            // 随机选择事件ID（后续可以从事件数据库加载）
            eventData.EventId = $"event_floor{floor}_{Random.Range(0, 5)}";
            eventData.ChoiceResult = -1; // -1 表示未选择

            room.Data = eventData;
        }

        /// <summary>
        /// 生成商店房间内容
        /// </summary>
        private static void GenerateShopRoomContent(Room room, int floor)
        {
            ShopRoomData shopData = new ShopRoomData();
            
            // 根据楼层生成商品列表
            shopData.ItemIds = new List<string>();
            
            int itemCount = Random.Range(3, 6);
            for (int i = 0; i < itemCount; i++)
            {
                shopData.ItemIds.Add($"shop_item_floor{floor}_{i}");
            }

            shopData.Discount = 1f; // 默认无折扣（1.0表示原价）

            room.Data = shopData;
        }

        /// <summary>
        /// 生成休息房间内容
        /// </summary>
        private static void GenerateRestRoomContent(Room room, int floor)
        {
            RestRoomData restData = new RestRoomData();
            
            // 休息房间恢复50%-100%HP
            restData.HpRestorePercent = Random.Range(0.5f, 1.0f);
            restData.RestCost = 0; // 默认免费休息

            room.Data = restData;
        }

        /// <summary>
        /// 生成献祭房间内容
        /// </summary>
        private static void GenerateSacrificeRoomContent(Room room, int floor)
        {
            // 献祭房间可以消耗HP/物品换取奖励
            EventRoomData sacrificeData = new EventRoomData();
            sacrificeData.EventId = $"sacrifice_floor{floor}";
            room.Data = sacrificeData;
        }

        /// <summary>
        /// 生成隐藏房间内容
        /// </summary>
        private static void GenerateSecretRoomContent(Room room, int floor)
        {
            // 隐藏房间通常有特殊奖励
            TreasureRoomData secretData = new TreasureRoomData();
            secretData.Gold = 50 + floor * 20;
            secretData.ItemIds = new List<string> { $"secret_item_floor{floor}" };
            room.Data = secretData;
        }

        /// <summary>
        /// 根据楼层获取敌人数量
        /// </summary>
        private static int GetEnemyCountForFloor(int floor)
        {
            // 楼层越高，敌人越多
            if (floor <= 1) return Random.Range(1, 3);
            if (floor <= 3) return Random.Range(2, 4);
            if (floor <= 5) return Random.Range(2, 5);
            return Random.Range(3, 6);
        }
    }

}
