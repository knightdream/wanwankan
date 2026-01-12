using System.Collections.Generic;

namespace WanWanKan.Map
{
    /// <summary>
    /// 宝藏房间数据
    /// </summary>
    [System.Serializable]
    public class TreasureRoomData : RoomData
    {
        /// <summary>
        /// 宝藏物品ID列表
        /// </summary>
        public List<string> ItemIds { get; set; }
        
        /// <summary>
        /// 金币数量
        /// </summary>
        public int Gold { get; set; }
        
        /// <summary>
        /// 是否已开启
        /// </summary>
        public bool IsOpened { get; set; }

        public TreasureRoomData()
        {
            ItemIds = new List<string>();
            Gold = 0;
            IsOpened = false;
        }
    }
}

