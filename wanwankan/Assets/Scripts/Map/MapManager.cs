using UnityEngine;
using WanWanKan.Core;

namespace WanWanKan.Map
{
    /// <summary>
    /// 地图管理器 - 管理当前地图状态和房间导航
    /// </summary>
    public class MapManager : MonoBehaviour
    {
        public static MapManager Instance { get; private set; }

        /// <summary>
        /// 当前地图布局
        /// </summary>
        public MapLayout CurrentMap { get; private set; }

        /// <summary>
        /// 当前楼层编号
        /// </summary>
        public int CurrentFloor { get; private set; }

        [Header("地图生成配置")]
        [SerializeField] private int minRoomsPerFloor = 5;
        [SerializeField] private int maxRoomsPerFloor = 7;

        [Header("调试")]
        [SerializeField] private bool debugMode = false;

        // 事件
        public System.Action<Room> OnRoomEntered;
        public System.Action<Room> OnRoomCompleted;
        public System.Action<MapLayout> OnMapGenerated;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 如果还没有生成地图，生成第一层
            if (CurrentMap == null)
            {
                GenerateNewFloor(1);
            }
        }

        /// <summary>
        /// 生成新楼层
        /// </summary>
        public void GenerateNewFloor(int floorNumber)
        {
            CurrentFloor = floorNumber;
            CurrentMap = MapGenerator.GenerateMap(floorNumber, minRoomsPerFloor, maxRoomsPerFloor);
            
            if (debugMode)
            {
                Debug.Log($"[MapManager] 生成第 {floorNumber} 层地图，共 {CurrentMap.RoomCount} 个房间");
                PrintMapLayout();
            }
            
            OnMapGenerated?.Invoke(CurrentMap);
        }

        /// <summary>
        /// 进入房间
        /// </summary>
        public bool EnterRoom(int roomId)
        {
            Room room = CurrentMap?.GetRoom(roomId);
            if (room == null)
            {
                Debug.LogWarning($"[MapManager] 房间 {roomId} 不存在");
                return false;
            }

            // 检查是否可以进入（必须是相邻房间或已访问的房间）
            Room currentRoom = CurrentMap.GetCurrentRoom();
            if (currentRoom != null && !currentRoom.IsConnectedTo(roomId) && !room.IsVisited)
            {
                Debug.LogWarning($"[MapManager] 无法进入房间 {roomId}，不是相邻房间");
                return false;
            }

            // 更新当前房间
            CurrentMap.CurrentRoomId = roomId;
            room.IsVisited = true;

            if (debugMode)
            {
                Debug.Log($"[MapManager] 进入房间: {room}");
            }

            OnRoomEntered?.Invoke(room);
            return true;
        }

        /// <summary>
        /// 完成当前房间
        /// </summary>
        public void CompleteCurrentRoom()
        {
            Room currentRoom = CurrentMap?.GetCurrentRoom();
            if (currentRoom == null) return;

            currentRoom.IsCompleted = true;

            if (debugMode)
            {
                Debug.Log($"[MapManager] 完成房间: {currentRoom}");
            }

            OnRoomCompleted?.Invoke(currentRoom);
        }

        /// <summary>
        /// 移动到相邻房间
        /// </summary>
        public bool MoveToRoom(int roomId)
        {
            return EnterRoom(roomId);
        }

        /// <summary>
        /// 获取当前房间
        /// </summary>
        public Room GetCurrentRoom()
        {
            return CurrentMap?.GetCurrentRoom();
        }

        /// <summary>
        /// 获取可访问的房间列表
        /// </summary>
        public System.Collections.Generic.List<Room> GetAccessibleRooms()
        {
            if (CurrentMap == null) return new System.Collections.Generic.List<Room>();
            
            Room currentRoom = CurrentMap.GetCurrentRoom();
            if (currentRoom == null) return new System.Collections.Generic.List<Room>();

            return CurrentMap.GetAccessibleRooms(currentRoom.Id);
        }

        /// <summary>
        /// 检查是否可以进入下一层
        /// </summary>
        public bool CanProceedToNextFloor()
        {
            if (CurrentMap == null) return false;
            
            Room bossRoom = CurrentMap.GetBossRoom();
            return bossRoom != null && bossRoom.IsCompleted;
        }

        /// <summary>
        /// 进入下一层
        /// </summary>
        public void ProceedToNextFloor()
        {
            if (!CanProceedToNextFloor())
            {
                Debug.LogWarning("[MapManager] 无法进入下一层，BOSS房间未完成");
                return;
            }

            GenerateNewFloor(CurrentFloor + 1);
        }

        /// <summary>
        /// 打印地图布局（调试用）
        /// </summary>
        private void PrintMapLayout()
        {
            if (CurrentMap == null) return;

            Debug.Log("=== 地图布局 ===");
            foreach (Room room in CurrentMap.GetAllRooms())
            {
                string connections = string.Join(", ", room.ConnectedRoomIds);
                Debug.Log($"{room} -> 连接: [{connections}]");
            }
            Debug.Log("================");
        }

        /// <summary>
        /// 获取地图信息字符串（用于UI显示）
        /// </summary>
        public string GetMapInfoString()
        {
            if (CurrentMap == null) return "无地图";

            Room currentRoom = CurrentMap.GetCurrentRoom();
            string currentRoomInfo = currentRoom != null ? $"{currentRoom.Type}" : "未知";
            
            return $"第 {CurrentFloor} 层 | 当前房间: {currentRoomInfo} | 总房间数: {CurrentMap.RoomCount}";
        }
    }
}

