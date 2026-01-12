using System.Collections.Generic;
using UnityEngine;

namespace WanWanKan.Map
{
    /// <summary>
    /// 房间数据类
    /// </summary>
    [System.Serializable]
    public class Room
    {
        /// <summary>
        /// 房间ID（唯一标识）
        /// </summary>
        public int Id { get; private set; }
        
        /// <summary>
        /// 房间类型
        /// </summary>
        public RoomType Type { get; set; }
        
        /// <summary>
        /// 房间在楼层中的位置（用于布局）
        /// </summary>
        public Vector2Int Position { get; set; }
        
        /// <summary>
        /// 是否已访问
        /// </summary>
        public bool IsVisited { get; set; }
        
        /// <summary>
        /// 是否已完成（战斗/事件等）
        /// </summary>
        public bool IsCompleted { get; set; }
        
        /// <summary>
        /// 连接的房间ID列表（邻接关系）
        /// </summary>
        public List<int> ConnectedRoomIds { get; private set; }
        
        /// <summary>
        /// 房间深度（距离起点的距离）
        /// </summary>
        public int Depth { get; set; }
        
        /// <summary>
        /// 房间数据（战斗敌人、事件ID、商店物品等）
        /// </summary>
        public RoomData Data { get; set; }

        public Room(int id, RoomType type, Vector2Int position)
        {
            Id = id;
            Type = type;
            Position = position;
            IsVisited = false;
            IsCompleted = false;
            ConnectedRoomIds = new List<int>();
            Depth = 0;
            Data = null;
        }

        /// <summary>
        /// 添加连接的房间
        /// </summary>
        public void AddConnection(int roomId)
        {
            if (!ConnectedRoomIds.Contains(roomId))
            {
                ConnectedRoomIds.Add(roomId);
            }
        }

        /// <summary>
        /// 移除连接的房间
        /// </summary>
        public void RemoveConnection(int roomId)
        {
            ConnectedRoomIds.Remove(roomId);
        }

        /// <summary>
        /// 检查是否连接到指定房间
        /// </summary>
        public bool IsConnectedTo(int roomId)
        {
            return ConnectedRoomIds.Contains(roomId);
        }

        public override string ToString()
        {
            return $"Room[{Id}] {Type} @ {Position} (Depth:{Depth})";
        }
    }

    /// <summary>
    /// 房间数据基类（用于存储不同类型房间的具体数据）
    /// </summary>
    [System.Serializable]
    public class RoomData
    {
        // 子类可以继承此类来存储特定类型房间的数据
        // 例如：BattleRoomData, EventRoomData, ShopRoomData 等
    }
}

