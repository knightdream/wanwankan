using System.Collections.Generic;

namespace WanWanKan.Map
{
    /// <summary>
    /// 商店房间数据
    /// </summary>
    [System.Serializable]
    public class ShopRoomData : RoomData
    {
        /// <summary>
        /// 商店物品ID列表（可购买的物品）
        /// </summary>
        public List<string> ItemIds { get; set; }
        
        /// <summary>
        /// 商店折扣（0-1，1表示原价）
        /// </summary>
        public float Discount { get; set; }
        
        /// <summary>
        /// 是否已访问过
        /// </summary>
        public bool HasVisited { get; set; }

        public ShopRoomData()
        {
            ItemIds = new List<string>();
            Discount = 1f;
            HasVisited = false;
        }
    }
}

