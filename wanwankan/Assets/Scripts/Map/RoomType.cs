namespace WanWanKan.Map
{
    /// <summary>
    /// 房间类型枚举
    /// </summary>
    public enum RoomType
    {
        /// <summary>
        /// 起点房间
        /// </summary>
        Start = 0,
        
        /// <summary>
        /// 战斗房间 - 40%概率
        /// </summary>
        Battle = 1,
        
        /// <summary>
        /// 宝藏房间 - 15%概率
        /// </summary>
        Treasure = 2,
        
        /// <summary>
        /// 事件房间 - 25%概率
        /// </summary>
        Event = 3,
        
        /// <summary>
        /// 商店房间 - 10%概率
        /// </summary>
        Shop = 4,
        
        /// <summary>
        /// 休息房间 - 10%概率
        /// </summary>
        Rest = 5,
        
        /// <summary>
        /// BOSS房间
        /// </summary>
        Boss = 6
    }
}

