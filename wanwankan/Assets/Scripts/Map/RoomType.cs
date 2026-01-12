namespace WanWanKan.Map
{
    /// <summary>
    /// 房间类型枚举
    /// </summary>
    public enum RoomType
    {
        /// <summary>
        /// 起点房间（固定1个）
        /// </summary>
        Start = 0,
        
        /// <summary>
        /// 战斗房间（填充剩余空间）
        /// </summary>
        Battle = 1,
        
        /// <summary>
        /// 宝藏房间
        /// </summary>
        Treasure = 2,
        
        /// <summary>
        /// 事件房间
        /// </summary>
        Event = 3,
        
        /// <summary>
        /// 商店房间
        /// </summary>
        Shop = 4,
        
        /// <summary>
        /// 休息房间
        /// </summary>
        Rest = 5,
        
        /// <summary>
        /// BOSS房间
        /// </summary>
        Boss = 6,
        
        /// <summary>
        /// 献祭房间（消耗HP换取奖励）
        /// </summary>
        Sacrifice = 7,
        
        /// <summary>
        /// 隐藏房间（特殊条件触发）
        /// </summary>
        Secret = 8
    }
}

