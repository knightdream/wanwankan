namespace WanWanKan.Map
{
    /// <summary>
    /// 事件房间数据
    /// </summary>
    [System.Serializable]
    public class EventRoomData : RoomData
    {
        /// <summary>
        /// 事件ID（对应事件系统中的事件ID）
        /// </summary>
        public string EventId { get; set; }
        
        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted { get; set; }
        
        /// <summary>
        /// 事件选择结果（如果有分支）
        /// </summary>
        public int ChoiceResult { get; set; }

        public EventRoomData()
        {
            EventId = "";
            IsCompleted = false;
            ChoiceResult = -1;
        }
    }
}

