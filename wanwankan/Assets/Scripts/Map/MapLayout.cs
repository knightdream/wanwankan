using System.Collections.Generic;
using UnityEngine;

namespace WanWanKan.Map
{
    /// <summary>
    /// 地图布局类 - 表示一层的地图结构
    /// </summary>
    [System.Serializable]
    public class MapLayout
    {
        /// <summary>
        /// 楼层编号
        /// </summary>
        public int FloorNumber { get; private set; }
        
        /// <summary>
        /// 所有房间的字典（Key: 房间ID）
        /// </summary>
        public Dictionary<int, Room> Rooms { get; private set; }
        
        /// <summary>
        /// 起点房间ID
        /// </summary>
        public int StartRoomId { get; private set; }
        
        /// <summary>
        /// BOSS房间ID
        /// </summary>
        public int BossRoomId { get; private set; }
        
        /// <summary>
        /// 当前玩家所在的房间ID
        /// </summary>
        public int CurrentRoomId { get; set; }
        
        /// <summary>
        /// 地图生成种子（用于存档恢复）
        /// </summary>
        public int Seed { get; set; }
        
        /// <summary>
        /// 房间数量
        /// </summary>
        public int RoomCount => Rooms.Count;

        public MapLayout(int floorNumber)
        {
            FloorNumber = floorNumber;
            Rooms = new Dictionary<int, Room>();
            StartRoomId = -1;
            BossRoomId = -1;
            CurrentRoomId = -1;
        }

        /// <summary>
        /// 添加房间
        /// </summary>
        public void AddRoom(Room room)
        {
            if (Rooms.ContainsKey(room.Id))
            {
                Debug.LogWarning($"[MapLayout] 房间 {room.Id} 已存在，将被覆盖");
            }
            
            Rooms[room.Id] = room;
            
            // 自动设置起点和BOSS房间
            if (room.Type == RoomType.Start)
            {
                StartRoomId = room.Id;
                if (CurrentRoomId == -1)
                {
                    CurrentRoomId = room.Id;
                }
            }
            else if (room.Type == RoomType.Boss)
            {
                BossRoomId = room.Id;
            }
        }

        /// <summary>
        /// 获取房间
        /// </summary>
        public Room GetRoom(int roomId)
        {
            return Rooms.TryGetValue(roomId, out Room room) ? room : null;
        }

        /// <summary>
        /// 获取当前房间
        /// </summary>
        public Room GetCurrentRoom()
        {
            return GetRoom(CurrentRoomId);
        }

        /// <summary>
        /// 获取起点房间
        /// </summary>
        public Room GetStartRoom()
        {
            return GetRoom(StartRoomId);
        }

        /// <summary>
        /// 获取BOSS房间
        /// </summary>
        public Room GetBossRoom()
        {
            return GetRoom(BossRoomId);
        }

        /// <summary>
        /// 获取可访问的相邻房间列表
        /// </summary>
        public List<Room> GetAccessibleRooms(int roomId)
        {
            Room room = GetRoom(roomId);
            if (room == null)
            {
                return new List<Room>();
            }

            List<Room> accessibleRooms = new List<Room>();
            foreach (int connectedId in room.ConnectedRoomIds)
            {
                Room connectedRoom = GetRoom(connectedId);
                if (connectedRoom != null)
                {
                    accessibleRooms.Add(connectedRoom);
                }
            }

            return accessibleRooms;
        }

        /// <summary>
        /// 检查两个房间是否相邻
        /// </summary>
        public bool AreRoomsConnected(int roomId1, int roomId2)
        {
            Room room1 = GetRoom(roomId1);
            if (room1 == null) return false;
            
            return room1.IsConnectedTo(roomId2);
        }

        /// <summary>
        /// 获取所有房间的列表
        /// </summary>
        public List<Room> GetAllRooms()
        {
            return new List<Room>(Rooms.Values);
        }
    }
}

