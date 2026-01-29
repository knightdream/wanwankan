using System.Collections.Generic;
using UnityEngine;
using WanWanKan.Combat;
using WanWanKan.Character;
using WanWanKan.Core;

namespace WanWanKan.Map
{
    /// <summary>
    /// 房间处理器 - 处理进入房间时的逻辑（战斗、事件、商店等）
    /// </summary>
    public class RoomHandler : MonoBehaviour
    {
        public static RoomHandler Instance { get; private set; }

        [Header("配置")]
        [SerializeField] private EnemyDatabase enemyDatabase;
        [SerializeField] private bool autoStartBattle = true; // 进入战斗房间时自动开始战斗

        [Header("玩家配置")]
        [SerializeField] private string playerName = "勇者";
        [SerializeField] private int playerSTR = 8;
        [SerializeField] private int playerAGI = 10;
        [SerializeField] private int playerWIS = 6;

        [Header("调试")]
        [SerializeField] private bool debugMode = false;

        // 当前玩家单位（战斗用）
        private CombatUnit playerUnit;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 订阅地图管理器事件
            if (MapManager.Instance != null)
            {
                MapManager.Instance.OnRoomEntered += HandleRoomEntered;
            }

            // 创建玩家单位（用于战斗）
            CreatePlayerUnit();
        }

        private void OnDestroy()
        {
            if (MapManager.Instance != null)
            {
                MapManager.Instance.OnRoomEntered -= HandleRoomEntered;
            }
        }

        /// <summary>
        /// 创建玩家单位
        /// </summary>
        private void CreatePlayerUnit()
        {
            if (playerUnit == null)
            {
                GameObject playerGO = new GameObject("PlayerUnit");
                playerGO.transform.SetParent(transform);
                
                // 使用角色选择系统创建玩家单位
                if (CharacterSelection.Instance != null)
                {
                    playerUnit = CharacterSelection.Instance.CreatePlayerCombatUnit(playerGO);
                }
                else
                {
                    // 如果没有角色选择系统，使用默认配置
                    playerUnit = CombatUnit.CreatePlayer(
                        playerGO,
                        playerName,
                        playerSTR,
                        playerAGI,
                        playerWIS
                    );
                }
            }
        }

        /// <summary>
        /// 处理房间进入事件
        /// </summary>
        private void HandleRoomEntered(Room room)
        {
            if (room == null) return;

            if (debugMode)
            {
                Debug.Log($"[RoomHandler] 进入房间: {room.Type}");
            }

            // 如果房间还没有内容，先生成内容
            if (room.Data == null)
            {
                int floor = MapManager.Instance != null ? MapManager.Instance.CurrentFloor : 1;
                RoomContentGenerator.GenerateRoomContent(room, floor, enemyDatabase);
            }

            // 根据房间类型处理
            switch (room.Type)
            {
                case RoomType.Start:
                    HandleStartRoom(room);
                    break;

                case RoomType.Battle:
                    HandleBattleRoom(room);
                    break;

                case RoomType.Boss:
                    HandleBossRoom(room);
                    break;

                case RoomType.Treasure:
                    HandleTreasureRoom(room);
                    break;

                case RoomType.Event:
                    HandleEventRoom(room);
                    break;

                case RoomType.Shop:
                    HandleShopRoom(room);
                    break;

                case RoomType.Rest:
                    HandleRestRoom(room);
                    break;

                case RoomType.Sacrifice:
                    HandleSacrificeRoom(room);
                    break;

                case RoomType.Secret:
                    HandleSecretRoom(room);
                    break;
            }
        }

        /// <summary>
        /// 处理起点房间
        /// </summary>
        private void HandleStartRoom(Room room)
        {
            if (debugMode)
            {
                Debug.Log("[RoomHandler] 起点房间 - 可以开始探索");
            }
            // 起点房间通常不需要特殊处理，可以显示提示信息
        }

        /// <summary>
        /// 处理战斗房间
        /// </summary>
        private void HandleBattleRoom(Room room)
        {
            BattleRoomData battleData = room.Data as BattleRoomData;
            if (battleData == null)
            {
                Debug.LogWarning($"[RoomHandler] 战斗房间 {room.Id} 没有战斗数据");
                return;
            }

            // 如果已经战斗过，不再触发战斗
            if (battleData.HasBattled)
            {
                if (debugMode)
                {
                    Debug.Log("[RoomHandler] 战斗房间已完成，跳过战斗");
                }
                return;
            }

            if (autoStartBattle)
            {
                StartBattle(room, battleData);
            }
        }

        /// <summary>
        /// 处理BOSS房间
        /// </summary>
        private void HandleBossRoom(Room room)
        {
            BattleRoomData bossData = room.Data as BattleRoomData;
            if (bossData == null)
            {
                Debug.LogWarning($"[RoomHandler] BOSS房间 {room.Id} 没有战斗数据");
                return;
            }

            if (bossData.HasBattled)
            {
                if (debugMode)
                {
                    Debug.Log("[RoomHandler] BOSS房间已完成");
                }
                return;
            }

            if (autoStartBattle)
            {
                StartBattle(room, bossData, isBoss: true);
            }
        }

        /// <summary>
        /// 开始战斗
        /// </summary>
        private void StartBattle(Room room, BattleRoomData battleData, bool isBoss = false)
        {
            if (CombatManager.Instance == null)
            {
                Debug.LogError("[RoomHandler] CombatManager不存在，无法开始战斗");
                return;
            }

            if (playerUnit == null)
            {
                CreatePlayerUnit();
            }

            // 创建敌人单位
            List<CombatUnit> enemies = new List<CombatUnit>();
            
            if (enemyDatabase != null)
            {
                // 使用数据库创建敌人
                foreach (string enemyId in battleData.EnemyIds)
                {
                    EnemyData enemyData = enemyDatabase.GetEnemyById(enemyId);
                    if (enemyData != null)
                    {
                        GameObject enemyGO = new GameObject($"Enemy_{enemyData.Name}");
                        enemyGO.transform.SetParent(transform);
                        
                        CombatUnit enemyUnit = CombatUnit.CreateEnemy(
                            enemyGO,
                            enemyData.Name,
                            enemyData.Strength,
                            enemyData.Agility,
                            enemyData.Wisdom
                        );
                        enemies.Add(enemyUnit);
                    }
                }
            }
            else
            {
                // 如果没有数据库，使用默认配置
                int floor = MapManager.Instance != null ? MapManager.Instance.CurrentFloor : 1;
                int enemyCount = battleData.EnemyIds.Count;
                
                for (int i = 0; i < enemyCount; i++)
                {
                    // 根据楼层生成敌人属性
                    int baseStr = 5 + floor;
                    int baseAgi = 5 + floor;
                    int baseWis = 5 + floor;
                    
                    string enemyName = isBoss ? $"BOSS-{floor}层" : $"敌人-{floor}层-{i + 1}";
                    
                    GameObject enemyGO = new GameObject($"Enemy_{enemyName}");
                    enemyGO.transform.SetParent(transform);
                    
                    CombatUnit enemyUnit = CombatUnit.CreateEnemy(
                        enemyGO,
                        enemyName,
                        baseStr,
                        baseAgi,
                        baseWis
                    );
                    enemies.Add(enemyUnit);
                }
            }

            if (enemies.Count == 0)
            {
                Debug.LogWarning("[RoomHandler] 没有敌人可以战斗");
                return;
            }

            // 创建玩家单位列表
            List<CombatUnit> players = new List<CombatUnit> { playerUnit };

            // 订阅战斗结束事件
            CombatManager.Instance.OnCombatEnd += (victory) => OnBattleEnd(room, battleData, victory);

            // 开始战斗
            CombatManager.Instance.StartCombat(players, enemies);

            if (debugMode)
            {
                Debug.Log($"[RoomHandler] 开始战斗 - 玩家: {players.Count}, 敌人: {enemies.Count}");
            }
        }

        /// <summary>
        /// 战斗结束回调
        /// </summary>
        private void OnBattleEnd(Room room, BattleRoomData battleData, bool victory)
        {
            if (debugMode)
            {
                Debug.Log($"[RoomHandler] 战斗结束 - 胜利: {victory}");
            }

            if (victory)
            {
                // 标记战斗完成
                battleData.HasBattled = true;
                room.IsCompleted = true;

                // 给予奖励
                if (battleData.Reward != null)
                {
                    // TODO: 实现奖励系统（经验值、金币、物品）
                    Debug.Log($"[RoomHandler] 战斗奖励 - 经验: {battleData.Reward.Experience}, 金币: {battleData.Reward.Gold}");
                }

                // 通知地图管理器房间完成
                if (MapManager.Instance != null)
                {
                    MapManager.Instance.CompleteCurrentRoom();
                }
            }
            else
            {
                // 战斗失败 - 游戏结束或返回起点
                Debug.Log("[RoomHandler] 战斗失败！");
                // TODO: 实现失败处理（返回起点、游戏结束等）
            }
        }

        /// <summary>
        /// 处理宝藏房间
        /// </summary>
        private void HandleTreasureRoom(Room room)
        {
            TreasureRoomData treasureData = room.Data as TreasureRoomData;
            if (treasureData == null) return;

            if (treasureData.IsOpened)
            {
                if (debugMode)
                {
                    Debug.Log("[RoomHandler] 宝藏房间已开启");
                }
                return;
            }

            // TODO: 显示宝藏UI，让玩家选择开启
            if (debugMode)
            {
                Debug.Log($"[RoomHandler] 宝藏房间 - 金币: {treasureData.Gold}, 物品: {treasureData.ItemIds.Count}");
            }

            // 临时：自动开启宝藏
            treasureData.IsOpened = true;
            room.IsCompleted = true;
            if (MapManager.Instance != null)
            {
                MapManager.Instance.CompleteCurrentRoom();
            }
        }

        /// <summary>
        /// 处理事件房间
        /// </summary>
        private void HandleEventRoom(Room room)
        {
            EventRoomData eventData = room.Data as EventRoomData;
            if (eventData == null) return;

            if (eventData.IsCompleted)
            {
                if (debugMode)
                {
                    Debug.Log("[RoomHandler] 事件房间已完成");
                }
                return;
            }

            // TODO: 显示事件UI，触发对话系统
            if (debugMode)
            {
                Debug.Log($"[RoomHandler] 事件房间 - 事件ID: {eventData.EventId}");
            }
        }

        /// <summary>
        /// 处理商店房间
        /// </summary>
        private void HandleShopRoom(Room room)
        {
            ShopRoomData shopData = room.Data as ShopRoomData;
            if (shopData == null) return;

            // TODO: 显示商店UI
            if (debugMode)
            {
                Debug.Log($"[RoomHandler] 商店房间 - 商品数: {shopData.ItemIds.Count}, 折扣: {shopData.Discount}");
            }
        }

        /// <summary>
        /// 处理休息房间
        /// </summary>
        private void HandleRestRoom(Room room)
        {
            RestRoomData restData = room.Data as RestRoomData;
            if (restData == null) return;

            if (restData.HasRested)
            {
                if (debugMode)
                {
                    Debug.Log("[RoomHandler] 休息房间已使用");
                }
                return;
            }

            // TODO: 显示休息UI，让玩家选择是否休息
            if (debugMode)
            {
                Debug.Log($"[RoomHandler] 休息房间 - 恢复: {restData.HpRestorePercent * 100}%, 代价: {restData.RestCost}");
            }

            // 临时：自动休息
            if (playerUnit != null && playerUnit.Stats != null)
            {
                int healAmount = Mathf.RoundToInt(playerUnit.Stats.MaxHP * restData.HpRestorePercent);
                playerUnit.Stats.Heal(healAmount);
                restData.HasRested = true;
                room.IsCompleted = true;
                
                if (debugMode)
                {
                    Debug.Log($"[RoomHandler] 休息完成，恢复 {healAmount} HP");
                }
            }

            if (MapManager.Instance != null)
            {
                MapManager.Instance.CompleteCurrentRoom();
            }
        }

        /// <summary>
        /// 处理献祭房间
        /// </summary>
        private void HandleSacrificeRoom(Room room)
        {
            // TODO: 实现献祭房间逻辑
            if (debugMode)
            {
                Debug.Log("[RoomHandler] 献祭房间");
            }
        }

        /// <summary>
        /// 处理隐藏房间
        /// </summary>
        private void HandleSecretRoom(Room room)
        {
            TreasureRoomData secretData = room.Data as TreasureRoomData;
            if (secretData == null) return;

            // 隐藏房间类似宝藏房间
            HandleTreasureRoom(room);
        }

        /// <summary>
        /// 获取玩家单位（供其他系统使用）
        /// </summary>
        public CombatUnit GetPlayerUnit()
        {
            return playerUnit;
        }
    }
}
