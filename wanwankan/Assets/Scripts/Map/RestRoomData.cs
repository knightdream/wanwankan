namespace WanWanKan.Map
{
    /// <summary>
    /// 休息房间数据
    /// </summary>
    [System.Serializable]
    public class RestRoomData : RoomData
    {
        /// <summary>
        /// HP恢复百分比（0-1）
        /// </summary>
        public float HpRestorePercent { get; set; }
        
        /// <summary>
        /// 是否已休息
        /// </summary>
        public bool HasRested { get; set; }
        
        /// <summary>
        /// 休息的代价（金币、物品等，可选）
        /// </summary>
        public int RestCost { get; set; }

        public RestRoomData()
        {
            HpRestorePercent = 1f; // 默认完全恢复
            HasRested = false;
            RestCost = 0;
        }
    }
}

