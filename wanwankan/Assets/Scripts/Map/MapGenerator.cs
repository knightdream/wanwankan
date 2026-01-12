using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WanWanKan.Map
{
    /// <summary>
    /// 房间数量配置
    /// </summary>
    [System.Serializable]
    public class RoomCountConfig
    {
        public int Min;  // 最小数量
        public int Max;  // 最大数量
        
        public RoomCountConfig(int min, int max)
        {
            Min = min;
            Max = max;
        }
        
        /// <summary>
        /// 获取随机数量
        /// </summary>
        public int GetRandomCount()
        {
            return Random.Range(Min, Max + 1);
        }
    }

    /// <summary>
    /// 楼层房间配置
    /// </summary>
    [System.Serializable]
    public class FloorRoomConfig
    {
        public int TotalRoomsMin = 15;        // 总房间数最小值（确保地图足够大）
        public int TotalRoomsMax = 22;        // 总房间数最大值
        
        // 各类型房间数量配置
        public RoomCountConfig Boss = new RoomCountConfig(1, 1);        // BOSS房（固定1个）
        public RoomCountConfig Shop = new RoomCountConfig(1, 2);        // 商店（1-2个）
        public RoomCountConfig Treasure = new RoomCountConfig(1, 2);    // 宝藏（1-2个）
        public RoomCountConfig Rest = new RoomCountConfig(1, 2);        // 休息（1-2个）
        public RoomCountConfig Event = new RoomCountConfig(2, 4);       // 事件（2-4个）
        public RoomCountConfig Sacrifice = new RoomCountConfig(0, 1);   // 献祭（0-1个）
        public RoomCountConfig Secret = new RoomCountConfig(0, 1);      // 隐藏（0-1个）
        // 战斗房填充剩余空间
    }

    /// <summary>
    /// 地图生成器 - 生成可自由探索的网格地图（类似以撒的结合/霓虹深渊）
    /// </summary>
    public static class MapGenerator
    {
        /// <summary>
        /// 房间类型权重配置（用于随机填充）
        /// </summary>
        private static readonly Dictionary<RoomType, float> RoomTypeWeights = new Dictionary<RoomType, float>
        {
            { RoomType.Battle, 70f },    // 70% 战斗（主要填充）
            { RoomType.Event, 20f },     // 20% 事件
            { RoomType.Treasure, 5f },   // 5% 宝藏
            { RoomType.Rest, 5f }        // 5% 休息
        };

        // 默认楼层配置
        private static FloorRoomConfig _defaultConfig = new FloorRoomConfig();
        
        // 当前使用的配置
        private static FloorRoomConfig _currentConfig;

        // 地图配置
        private const int GRID_SIZE = 7;      // 网格大小 7x7（确保有足够空间放置远距离BOSS）
        private const int MIN_BOSS_DISTANCE = 4;  // BOSS距离起点的最小深度

        /// <summary>
        /// 设置楼层配置
        /// </summary>
        public static void SetFloorConfig(FloorRoomConfig config)
        {
            _currentConfig = config;
        }

        /// <summary>
        /// 获取当前配置
        /// </summary>
        private static FloorRoomConfig GetConfig()
        {
            return _currentConfig ?? _defaultConfig;
        }

        /// <summary>
        /// 生成地图布局
        /// </summary>
        public static MapLayout GenerateMap(int floorNumber, int minRooms = 12, int maxRooms = 18)
        {
            MapLayout layout = new MapLayout(floorNumber);
            
            FloorRoomConfig config = GetConfig();
            int targetRooms = Random.Range(config.TotalRoomsMin, config.TotalRoomsMax + 1);
            
            // 生成可探索的网格地图
            GenerateExplorationMap(layout, targetRooms, targetRooms);
            
            // 根据配置表分配房间类型
            AssignRoomTypesByConfig(layout, config);
            
            // 计算房间深度
            CalculateRoomDepths(layout);
            
            return layout;
        }

        /// <summary>
        /// 生成可探索的网格地图（以撒风格）
        /// 结构示例：
        ///          [宝藏]
        ///            │
        /// [商店]─[战斗]─[战斗]─[隐藏]
        ///            │     │
        ///      [起点]─[战斗]─[事件]
        ///        │           │
        ///     [战斗]─[战斗]─[BOSS]
        /// </summary>
        private static void GenerateExplorationMap(MapLayout layout, int minRooms, int maxRooms)
        {
            int roomIdCounter = 0;
            int targetRooms = Random.Range(minRooms, maxRooms + 1);
            
            // 网格：存储每个位置的房间（null表示空）
            Room[,] grid = new Room[GRID_SIZE, GRID_SIZE];
            
            // 起点放在中心
            int centerX = GRID_SIZE / 2;
            int centerY = GRID_SIZE / 2;
            
            Room startRoom = new Room(roomIdCounter++, RoomType.Start, new Vector2Int(centerX, centerY));
            layout.AddRoom(startRoom);
            grid[centerX, centerY] = startRoom;
            
            // 从起点开始扩展房间
            List<Vector2Int> frontier = new List<Vector2Int>(); // 可扩展的位置
            AddAdjacentToFrontier(frontier, grid, centerX, centerY);
            
            int roomsCreated = 1;
            
            // 持续扩展直到达到目标房间数
            while (roomsCreated < targetRooms && frontier.Count > 0)
            {
                // 随机选择一个扩展位置
                int index = Random.Range(0, frontier.Count);
                Vector2Int pos = frontier[index];
                frontier.RemoveAt(index);
                
                // 检查这个位置是否仍然有效（旁边有房间）
                if (grid[pos.x, pos.y] != null) continue;
                if (!HasAdjacentRoom(grid, pos.x, pos.y)) continue;
                
                // 创建新房间
                Room newRoom = new Room(roomIdCounter++, RoomType.Battle, pos);
                layout.AddRoom(newRoom);
                grid[pos.x, pos.y] = newRoom;
                roomsCreated++;
                
                // 连接到相邻的房间
                ConnectToAdjacentRooms(newRoom, grid, pos.x, pos.y);
                
                // 添加新的扩展位置
                AddAdjacentToFrontier(frontier, grid, pos.x, pos.y);
            }
            
            // 找到所有末端房间（只有1个连接的房间）
            List<Room> deadEnds = new List<Room>();
            foreach (Room room in layout.GetAllRooms())
            {
                if (room.Type != RoomType.Start && room.ConnectedRoomIds.Count == 1)
                {
                    deadEnds.Add(room);
                }
            }
            
            // 确保有BOSS房间
            // 规则1: 100%保证距离起点至少4步（3步内没有BOSS）
            // 规则2: 90%概率放在最远位置
            if (deadEnds.Count > 0)
            {
                // 计算深度
                Dictionary<int, int> tempDepths = CalculateTempDepths(layout, startRoom);
                
                // 筛选出符合条件的候选房间（深度 >= 4）
                List<Room> validBossCandidates = new List<Room>();
                Room farthestRoom = null;
                int maxDepth = -1;
                
                foreach (Room room in deadEnds)
                {
                    if (tempDepths.ContainsKey(room.Id))
                    {
                        int depth = tempDepths[room.Id];
                        
                        // 记录最远的房间
                        if (depth > maxDepth)
                        {
                            maxDepth = depth;
                            farthestRoom = room;
                        }
                        
                        // 只有深度 >= MIN_BOSS_DISTANCE 的才能作为BOSS房
                        if (depth >= MIN_BOSS_DISTANCE)
                        {
                            validBossCandidates.Add(room);
                        }
                    }
                }
                
                Room bossRoom = null;
                
                if (validBossCandidates.Count > 0)
                {
                    // 90%概率选择最远的房间
                    if (Random.value < 0.9f)
                    {
                        // 从有效候选中找最远的
                        bossRoom = null;
                        maxDepth = -1;
                        foreach (Room room in validBossCandidates)
                        {
                            if (tempDepths[room.Id] > maxDepth)
                            {
                                maxDepth = tempDepths[room.Id];
                                bossRoom = room;
                            }
                        }
                    }
                    else
                    {
                        // 10%概率随机选择一个有效候选
                        bossRoom = validBossCandidates[Random.Range(0, validBossCandidates.Count)];
                    }
                }
                else if (farthestRoom != null)
                {
                    // 如果没有深度>=4的房间，选择最远的（地图太小的情况）
                    bossRoom = farthestRoom;
                    Debug.LogWarning("[MapGenerator] 地图太小，BOSS房深度不足4");
                }
                
                if (bossRoom != null)
                {
                    bossRoom.Type = RoomType.Boss;
                    deadEnds.Remove(bossRoom);
                }
            }
        }

        /// <summary>
        /// 计算临时深度
        /// </summary>
        private static Dictionary<int, int> CalculateTempDepths(MapLayout layout, Room startRoom)
        {
            Dictionary<int, int> depths = new Dictionary<int, int>();
            Queue<Room> queue = new Queue<Room>();
            
            depths[startRoom.Id] = 0;
            queue.Enqueue(startRoom);
            
            while (queue.Count > 0)
            {
                Room current = queue.Dequeue();
                int currentDepth = depths[current.Id];
                
                foreach (int connectedId in current.ConnectedRoomIds)
                {
                    if (!depths.ContainsKey(connectedId))
                    {
                        Room connected = layout.GetRoom(connectedId);
                        if (connected != null)
                        {
                            depths[connectedId] = currentDepth + 1;
                            queue.Enqueue(connected);
                        }
                    }
                }
            }
            
            return depths;
        }

        /// <summary>
        /// 添加相邻位置到扩展列表
        /// </summary>
        private static void AddAdjacentToFrontier(List<Vector2Int> frontier, Room[,] grid, int x, int y)
        {
            // 四个方向：上下左右
            int[,] directions = { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };
            
            for (int i = 0; i < 4; i++)
            {
                int nx = x + directions[i, 0];
                int ny = y + directions[i, 1];
                
                // 检查边界
                if (nx >= 0 && nx < GRID_SIZE && ny >= 0 && ny < GRID_SIZE)
                {
                    // 如果这个位置是空的且不在列表中
                    if (grid[nx, ny] == null)
                    {
                        Vector2Int pos = new Vector2Int(nx, ny);
                        if (!frontier.Contains(pos))
                        {
                            frontier.Add(pos);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查位置是否有相邻房间
        /// </summary>
        private static bool HasAdjacentRoom(Room[,] grid, int x, int y)
        {
            int[,] directions = { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };
            
            for (int i = 0; i < 4; i++)
            {
                int nx = x + directions[i, 0];
                int ny = y + directions[i, 1];
                
                if (nx >= 0 && nx < GRID_SIZE && ny >= 0 && ny < GRID_SIZE)
                {
                    if (grid[nx, ny] != null)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// 连接到相邻的房间
        /// </summary>
        private static void ConnectToAdjacentRooms(Room room, Room[,] grid, int x, int y)
        {
            int[,] directions = { { 0, -1 }, { 0, 1 }, { -1, 0 }, { 1, 0 } };
            
            for (int i = 0; i < 4; i++)
            {
                int nx = x + directions[i, 0];
                int ny = y + directions[i, 1];
                
                if (nx >= 0 && nx < GRID_SIZE && ny >= 0 && ny < GRID_SIZE)
                {
                    Room adjacent = grid[nx, ny];
                    if (adjacent != null && !room.IsConnectedTo(adjacent.Id))
                    {
                        room.AddConnection(adjacent.Id);
                        adjacent.AddConnection(room.Id);
                    }
                }
            }
        }

        /// <summary>
        /// 根据配置表分配房间类型
        /// </summary>
        private static void AssignRoomTypesByConfig(MapLayout layout, FloorRoomConfig config)
        {
            List<Room> deadEnds = new List<Room>();   // 末端房间（只有1个连接）
            List<Room> normalRooms = new List<Room>(); // 普通房间（多个连接）
            
            // 临时计算深度
            Room startRoom = layout.GetStartRoom();
            Dictionary<int, int> depths = new Dictionary<int, int>();
            if (startRoom != null)
            {
                depths = CalculateTempDepths(layout, startRoom);
            }
            
            // 检查是否已经有BOSS房（在GenerateExplorationMap中设置的）
            bool hasBoss = false;
            foreach (Room room in layout.GetAllRooms())
            {
                if (room.Type == RoomType.Boss)
                {
                    hasBoss = true;
                    break;
                }
            }
            
            foreach (Room room in layout.GetAllRooms())
            {
                // 跳过起点
                if (room.Type == RoomType.Start)
                {
                    continue;
                }
                
                // 跳过已标记为BOSS的房间
                if (room.Type == RoomType.Boss)
                {
                    continue;
                }
                
                if (room.ConnectedRoomIds.Count == 1)
                {
                    deadEnds.Add(room);
                }
                else
                {
                    normalRooms.Add(room);
                }
            }
            
            // ========== 按配置分配特殊房间 ==========
            
            // 1. BOSS房（如果还没有的话，使用有距离限制的选择）
            if (!hasBoss)
            {
                int bossCount = config.Boss.GetRandomCount();
                if (bossCount > 0 && deadEnds.Count > 0)
                {
                    // 筛选深度 >= MIN_BOSS_DISTANCE 的末端房间
                    List<Room> validBossCandidates = new List<Room>();
                    Room farthestRoom = null;
                    int maxDepth = -1;
                    
                    foreach (Room room in deadEnds)
                    {
                        if (depths.ContainsKey(room.Id))
                        {
                            int depth = depths[room.Id];
                            if (depth > maxDepth)
                            {
                                maxDepth = depth;
                                farthestRoom = room;
                            }
                            if (depth >= MIN_BOSS_DISTANCE)
                            {
                                validBossCandidates.Add(room);
                            }
                        }
                    }
                    
                    Room bossRoom = null;
                    if (validBossCandidates.Count > 0)
                    {
                        // 90%选最远，10%随机
                        if (Random.value < 0.9f)
                        {
                            maxDepth = -1;
                            foreach (Room room in validBossCandidates)
                            {
                                if (depths[room.Id] > maxDepth)
                                {
                                    maxDepth = depths[room.Id];
                                    bossRoom = room;
                                }
                            }
                        }
                        else
                        {
                            bossRoom = validBossCandidates[Random.Range(0, validBossCandidates.Count)];
                        }
                    }
                    else if (farthestRoom != null)
                    {
                        bossRoom = farthestRoom;
                    }
                    
                    if (bossRoom != null)
                    {
                        bossRoom.Type = RoomType.Boss;
                        deadEnds.Remove(bossRoom);
                    }
                }
            }
            
            // 打乱末端房间顺序
            ShuffleList(deadEnds);
            
            // 创建待分配的房间类型队列
            List<RoomType> typesToAssign = new List<RoomType>();
            
            // 2. 商店
            int shopCount = config.Shop.GetRandomCount();
            for (int i = 0; i < shopCount; i++) typesToAssign.Add(RoomType.Shop);
            
            // 3. 宝藏
            int treasureCount = config.Treasure.GetRandomCount();
            for (int i = 0; i < treasureCount; i++) typesToAssign.Add(RoomType.Treasure);
            
            // 4. 休息
            int restCount = config.Rest.GetRandomCount();
            for (int i = 0; i < restCount; i++) typesToAssign.Add(RoomType.Rest);
            
            // 5. 献祭
            int sacrificeCount = config.Sacrifice.GetRandomCount();
            for (int i = 0; i < sacrificeCount; i++) typesToAssign.Add(RoomType.Sacrifice);
            
            // 6. 隐藏房
            int secretCount = config.Secret.GetRandomCount();
            for (int i = 0; i < secretCount; i++) typesToAssign.Add(RoomType.Secret);
            
            // 7. 事件
            int eventCount = config.Event.GetRandomCount();
            for (int i = 0; i < eventCount; i++) typesToAssign.Add(RoomType.Event);
            
            // 打乱类型顺序
            ShuffleList(typesToAssign);
            
            // 优先分配给末端房间
            int typeIndex = 0;
            foreach (Room room in deadEnds)
            {
                if (typeIndex < typesToAssign.Count)
                {
                    room.Type = typesToAssign[typeIndex];
                    typeIndex++;
                }
                else
                {
                    // 末端房间用完了特殊类型，变成战斗房
                    room.Type = RoomType.Battle;
                }
            }
            
            // 剩余的特殊类型分配给普通房间
            ShuffleList(normalRooms);
            foreach (Room room in normalRooms)
            {
                if (typeIndex < typesToAssign.Count)
                {
                    room.Type = typesToAssign[typeIndex];
                    typeIndex++;
                }
                else
                {
                    // 剩余的普通房间都是战斗房
                    room.Type = RoomType.Battle;
                }
            }
        }

        /// <summary>
        /// 打乱列表顺序
        /// </summary>
        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
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



