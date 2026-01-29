using System;
using UnityEngine;

namespace WanWanKan.Map
{
    /// <summary>
    /// 敌人数据配置 - 定义敌人的属性
    /// </summary>
    [Serializable]
    public class EnemyData
    {
        /// <summary>
        /// 敌人ID（唯一标识）
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// 敌人名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 力量
        /// </summary>
        public int Strength { get; set; }
        
        /// <summary>
        /// 敏捷
        /// </summary>
        public int Agility { get; set; }
        
        /// <summary>
        /// 智慧
        /// </summary>
        public int Wisdom { get; set; }
        
        /// <summary>
        /// 经验值奖励
        /// </summary>
        public int ExperienceReward { get; set; }
        
        /// <summary>
        /// 金币奖励
        /// </summary>
        public int GoldReward { get; set; }
        
        /// <summary>
        /// 出现楼层范围（最小楼层）
        /// </summary>
        public int MinFloor { get; set; }
        
        /// <summary>
        /// 出现楼层范围（最大楼层）
        /// </summary>
        public int MaxFloor { get; set; }
        
        /// <summary>
        /// 出现权重（用于随机选择）
        /// </summary>
        public int Weight { get; set; }

        public EnemyData()
        {
            Id = "";
            Name = "未知敌人";
            Strength = 5;
            Agility = 5;
            Wisdom = 5;
            ExperienceReward = 10;
            GoldReward = 5;
            MinFloor = 1;
            MaxFloor = 10;
            Weight = 1;
        }

        public EnemyData(string id, string name, int str, int agi, int wis, int exp, int gold, int minFloor = 1, int maxFloor = 10, int weight = 1)
        {
            Id = id;
            Name = name;
            Strength = str;
            Agility = agi;
            Wisdom = wis;
            ExperienceReward = exp;
            GoldReward = gold;
            MinFloor = minFloor;
            MaxFloor = maxFloor;
            Weight = weight;
        }

        public override string ToString()
        {
            return $"{Name} (STR:{Strength} AGI:{Agility} WIS:{Wisdom})";
        }
    }

    /// <summary>
    /// 敌人数据库 - 存储所有敌人配置
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyDatabase", menuName = "WanWanKan/Enemy Database")]
    public class EnemyDatabase : ScriptableObject
    {
        [SerializeField] private EnemyData[] enemies;

        public EnemyData[] Enemies => enemies;

        /// <summary>
        /// 根据ID获取敌人数据
        /// </summary>
        public EnemyData GetEnemyById(string id)
        {
            foreach (var enemy in enemies)
            {
                if (enemy.Id == id)
                {
                    return enemy;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据楼层获取可出现的敌人列表
        /// </summary>
        public EnemyData[] GetEnemiesForFloor(int floor)
        {
            System.Collections.Generic.List<EnemyData> validEnemies = new System.Collections.Generic.List<EnemyData>();
            
            foreach (var enemy in enemies)
            {
                if (floor >= enemy.MinFloor && floor <= enemy.MaxFloor)
                {
                    validEnemies.Add(enemy);
                }
            }
            
            return validEnemies.ToArray();
        }

        /// <summary>
        /// 根据楼层和权重随机选择敌人
        /// </summary>
        public EnemyData GetRandomEnemyForFloor(int floor)
        {
            var validEnemies = GetEnemiesForFloor(floor);
            if (validEnemies.Length == 0)
            {
                Debug.LogWarning($"[EnemyDatabase] 第{floor}层没有可用的敌人配置，返回默认敌人");
                return new EnemyData("default", "哥布林", 5, 5, 5, 10, 5);
            }

            // 计算总权重
            int totalWeight = 0;
            foreach (var enemy in validEnemies)
            {
                totalWeight += enemy.Weight;
            }

            // 随机选择
            int random = UnityEngine.Random.Range(0, totalWeight);
            int currentWeight = 0;
            
            foreach (var enemy in validEnemies)
            {
                currentWeight += enemy.Weight;
                if (random < currentWeight)
                {
                    return enemy;
                }
            }

            // 如果没选到（理论上不会发生），返回第一个
            return validEnemies[0];
        }
    }
}
