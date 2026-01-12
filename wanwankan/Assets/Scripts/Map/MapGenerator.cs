using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WanWanKan.Map
{
    /// <summary>
    /// 地图生成器 - 负责生成随机地图布局
    /// </summary>
    public static class MapGenerator
    {
        /// <summary>
        /// 房间类型权重配置（用于随机生成）
        /// </summary>
        private static readonly Dictionary<RoomType, float> RoomTypeWeights = new Dictionary<RoomType, float>
        {
            { RoomType.Battle, 40f },    // 40%
            { RoomType.Treasure, 15f },  // 15%
            { RoomType.Event, 25f },     // 25%
            { RoomType.Shop, 10f },      // 10%
            { RoomType.Rest, 10f }       // 10%
        };

        /// <summary>
        /// 生成地图布局
        /// </summary>
        /// <param name="floorNumber">楼层编号</param>
        /// <param name="minRooms">最小房间数（默认5）</param>
        /// <param name="maxRooms">最大房间数（默认7）</param>
        /// <returns>生成的地图布局</returns>
        public static MapLayout GenerateMap(int floorNumber, int minRooms = 5, int maxRooms = 7)
        {
            MapLayout layout = new MapLayout(floorNumber);
            
            // 确定房间数量
            int roomCount = Random.Range(minRooms, maxRooms + 1);
            
            // 生成房间布局（使用简单的网格布局 + 随机连接）
            GenerateRoomLayout(layout, roomCount);
            
            // 分配房间类型
            AssignRoomTypes(layout);
            
            // 计算房间深度
            CalculateRoomDepths(layout);
            
            return layout;
        }

        /// <summary>
        /// 生成房间布局（网格布局算法）
        /// </summary>
        private static void GenerateRoomLayout(MapLayout layout, int roomCount)
        {
            int roomIdCounter = 0;
            
            // 1. 创建起点房间
            Room startRoom = new Room(roomIdCounter++, RoomType.Start, Vector2Int.zero);
            layout.AddRoom(startRoom);
            
            // 2. 创建BOSS房间（最后一个）
            Room bossRoom = new Room(roomIdCounter++, RoomType.Boss, new Vector2Int(roomCount - 1, 0));
            layout.AddRoom(bossRoom);
            
            // 3. 创建中间房间（使用简单的线性布局 + 分支）
            List<Room> rooms = new List<Room> { startRoom };
            List<Vector2Int> usedPositions = new List<Vector2Int> { Vector2Int.zero, new Vector2Int(roomCount - 1, 0) };
            
            // 生成中间房间
            int middleRoomCount = roomCount - 2; // 减去起点和BOSS
            for (int i = 0; i < middleRoomCount; i++)
            {
                Vector2Int position = FindAvailablePosition(usedPositions, roomCount);
                Room room = new Room(roomIdCounter++, RoomType.Battle, position); // 临时类型，后续会重新分配
                layout.AddRoom(room);
                rooms.Add(room);
                usedPositions.Add(position);
            }
            
            rooms.Add(bossRoom);
            
            // 4. 连接房间（确保所有房间可达）
            ConnectRooms(layout, rooms);
        }

        /// <summary>
        /// 查找可用位置（简单的网格布局）
        /// </summary>
        private static Vector2Int FindAvailablePosition(List<Vector2Int> usedPositions, int totalRooms)
        {
            // 尝试在主路径上放置（x轴递增）
            for (int x = 1; x < totalRooms - 1; x++)
            {
                Vector2Int pos = new Vector2Int(x, 0);
                if (!usedPositions.Contains(pos))
                {
                    return pos;
                }
            }
            
            // 如果主路径满了，尝试分支（y轴偏移）
            for (int y = -1; y <= 1; y += 2)
            {
                for (int x = 1; x < totalRooms - 1; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (!usedPositions.Contains(pos))
                    {
                        return pos;
                    }
                }
            }
            
            // 如果都满了，返回一个随机位置
            return new Vector2Int(Random.Range(1, totalRooms - 1), Random.Range(-1, 2));
        }

        /// <summary>
        /// 连接房间（确保连通性）
        /// </summary>
        private static void ConnectRooms(MapLayout layout, List<Room> rooms)
        {
            // 确保主路径连通（起点 -> ... -> BOSS）
            for (int i = 0; i < rooms.Count - 1; i++)
            {
                Room current = rooms[i];
                Room next = rooms[i + 1];
                
                // 双向连接
                current.AddConnection(next.Id);
                next.AddConnection(current.Id);
            }
            
            // 随机添加一些分支连接（增加探索选择）
            int branchCount = Random.Range(0, rooms.Count / 2);
            for (int i = 0; i < branchCount; i++)
            {
                Room room1 = rooms[Random.Range(0, rooms.Count - 1)]; // 不包括BOSS
                Room room2 = rooms[Random.Range(0, rooms.Count - 1)];
                
                if (room1.Id != room2.Id && !room1.IsConnectedTo(room2.Id))
                {
                    // 检查深度差，避免跨太多层连接
                    int depthDiff = Mathf.Abs(room1.Position.x - room2.Position.x);
                    if (depthDiff <= 2) // 只连接距离较近的房间
                    {
                        room1.AddConnection(room2.Id);
                        room2.AddConnection(room1.Id);
                    }
                }
            }
        }

        /// <summary>
        /// 分配房间类型
        /// </summary>
        private static void AssignRoomTypes(MapLayout layout)
        {
            List<Room> assignableRooms = new List<Room>();
            
            // 收集需要分配类型的房间（排除起点和BOSS）
            foreach (Room room in layout.GetAllRooms())
            {
                if (room.Type != RoomType.Start && room.Type != RoomType.Boss)
                {
                    assignableRooms.Add(room);
                }
            }
            
            // 使用加权随机分配房间类型
            foreach (Room room in assignableRooms)
            {
                room.Type = GetRandomRoomType();
            }
        }

        /// <summary>
        /// 根据权重随机获取房间类型
        /// </summary>
        private static RoomType GetRandomRoomType()
        {
            float totalWeight = 0f;
            foreach (float weight in RoomTypeWeights.Values)
            {
                totalWeight += weight;
            }
            
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var kvp in RoomTypeWeights)
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                {
                    return kvp.Key;
                }
            }
            
            // 默认返回战斗房间
            return RoomType.Battle;
        }

        /// <summary>
        /// 计算房间深度（距离起点的最短距离）
        /// </summary>
        private static void CalculateRoomDepths(MapLayout layout)
        {
            Room startRoom = layout.GetStartRoom();
            if (startRoom == null) return;
            
            // 使用BFS计算深度
            Queue<Room> queue = new Queue<Room>();
            HashSet<int> visited = new HashSet<int>();
            
            startRoom.Depth = 0;
            queue.Enqueue(startRoom);
            visited.Add(startRoom.Id);
            
            while (queue.Count > 0)
            {
                Room current = queue.Dequeue();
                
                foreach (int connectedId in current.ConnectedRoomIds)
                {
                    if (!visited.Contains(connectedId))
                    {
                        Room connectedRoom = layout.GetRoom(connectedId);
                        if (connectedRoom != null)
                        {
                            connectedRoom.Depth = current.Depth + 1;
                            queue.Enqueue(connectedRoom);
                            visited.Add(connectedId);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 设置房间类型权重（可用于动态调整）
        /// </summary>
        public static void SetRoomTypeWeight(RoomType type, float weight)
        {
            if (RoomTypeWeights.ContainsKey(type))
            {
                RoomTypeWeights[type] = weight;
            }
        }
    }
}

