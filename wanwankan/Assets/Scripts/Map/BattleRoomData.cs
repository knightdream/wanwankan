using System.Collections.Generic;

namespace WanWanKan.Map
{
    /// <summary>
    /// 战斗房间数据
    /// </summary>
    [System.Serializable]
    public class BattleRoomData : RoomData
    {
        /// <summary>
        /// 敌人ID列表（对应怪物系统中的怪物ID）
        /// </summary>
        public List<string> EnemyIds { get; set; }
        
        /// <summary>
        /// 是否已战斗
        /// </summary>
        public bool HasBattled { get; set; }
        
        /// <summary>
        /// 战斗奖励（经验值、金币等）
        /// </summary>
        public BattleReward Reward { get; set; }

        public BattleRoomData()
        {
            EnemyIds = new List<string>();
            HasBattled = false;
            Reward = new BattleReward();
        }
    }

    /// <summary>
    /// 战斗奖励
    /// </summary>
    [System.Serializable]
    public class BattleReward
    {
        public int Experience { get; set; }
        public int Gold { get; set; }
        public List<string> ItemIds { get; set; }

        public BattleReward()
        {
            Experience = 0;
            Gold = 0;
            ItemIds = new List<string>();
        }
    }
}

