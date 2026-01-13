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
        [SerializeField] private int minRoomsPerFloor = 15;
        [SerializeField] private int maxRoomsPerFloor = 22;

        [Header("存档设置")]
        [SerializeField] private bool autoSave = true;  // 自动保存
        
        [Header("调试")]
        [SerializeField] private bool debugMode = false;

        // 存档键名
        private const string SAVE_KEY_FLOOR = "MapManager_CurrentFloor";
        private const string SAVE_KEY_ROOM = "MapManager_CurrentRoomId";
        private const string SAVE_KEY_SEED = "MapManager_MapSeed";
        private const string SAVE_KEY_HAS_SAVE = "MapManager_HasSave";

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
            // 尝试加载存档，如果没有则生成新地图
            if (!TryLoadGame())
            {
                StartNewGame();
            }
        }

        private void OnApplicationQuit()
        {
            // 退出时自动保存
            if (autoSave)
            {
                SaveGame();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            // 暂停时自动保存（移动端）
            if (pauseStatus && autoSave)
            {
                SaveGame();
            }
        }

        /// <summary>
        /// 开始新游戏
        /// </summary>
        public void StartNewGame()
        {
            // 清除存档
            ClearSave();
            
            // 生成新地图
            GenerateNewFloor(1);
            
            // 自动进入起点
            EnterStartRoom();
            
            if (debugMode)
            {
                Debug.Log("[MapManager] 开始新游戏，进入起点");
            }
        }

        /// <summary>
        /// 进入起点房间
        /// </summary>
        public void EnterStartRoom()
        {
            if (CurrentMap == null) return;
            
            Room startRoom = CurrentMap.GetStartRoom();
            if (startRoom != null)
            {
                CurrentMap.CurrentRoomId = startRoom.Id;
                startRoom.IsVisited = true;
                
                if (debugMode)
                {
                    Debug.Log($"[MapManager] 进入起点房间: {startRoom}");
                }
                
                OnRoomEntered?.Invoke(startRoom);
            }
        }

        /// <summary>
        /// 保存游戏
        /// </summary>
        public void SaveGame()
        {
            if (CurrentMap == null) return;
            
            PlayerPrefs.SetInt(SAVE_KEY_HAS_SAVE, 1);
            PlayerPrefs.SetInt(SAVE_KEY_FLOOR, CurrentFloor);
            PlayerPrefs.SetInt(SAVE_KEY_ROOM, CurrentMap.CurrentRoomId);
            PlayerPrefs.SetInt(SAVE_KEY_SEED, CurrentMap.Seed);
            
            // 保存已访问和已完成的房间
            SaveRoomStates();
            
            PlayerPrefs.Save();
            
            if (debugMode)
            {
                Debug.Log($"[MapManager] 游戏已保存 - 楼层:{CurrentFloor}, 房间:{CurrentMap.CurrentRoomId}");
            }
        }

        /// <summary>
        /// 尝试加载游戏
        /// </summary>
        public bool TryLoadGame()
        {
            if (PlayerPrefs.GetInt(SAVE_KEY_HAS_SAVE, 0) == 0)
            {
                return false;
            }
            
            int savedFloor = PlayerPrefs.GetInt(SAVE_KEY_FLOOR, 1);
            int savedRoomId = PlayerPrefs.GetInt(SAVE_KEY_ROOM, -1);
            int savedSeed = PlayerPrefs.GetInt(SAVE_KEY_SEED, 0);
            
            // 使用相同的种子重新生成地图
            CurrentFloor = savedFloor;
            Random.InitState(savedSeed);
            CurrentMap = MapGenerator.GenerateMap(savedFloor, minRoomsPerFloor, maxRoomsPerFloor);
            CurrentMap.Seed = savedSeed;
            
            // 恢复房间状态
            LoadRoomStates();
            
            // 恢复当前房间
            if (savedRoomId >= 0 && CurrentMap.GetRoom(savedRoomId) != null)
            {
                CurrentMap.CurrentRoomId = savedRoomId;
            }
            else
            {
                // 如果保存的房间无效，进入起点
                EnterStartRoom();
            }
            
            if (debugMode)
            {
                Debug.Log($"[MapManager] 游戏已加载 - 楼层:{CurrentFloor}, 房间:{CurrentMap.CurrentRoomId}");
            }
            
            OnMapGenerated?.Invoke(CurrentMap);
            
            Room currentRoom = CurrentMap.GetCurrentRoom();
            if (currentRoom != null)
            {
                OnRoomEntered?.Invoke(currentRoom);
            }
            
            return true;
        }

        /// <summary>
        /// 保存房间状态
        /// </summary>
        private void SaveRoomStates()
        {
            if (CurrentMap == null) return;
            
            string visitedRooms = "";
            string completedRooms = "";
            
            foreach (Room room in CurrentMap.GetAllRooms())
            {
                if (room.IsVisited)
                {
                    visitedRooms += room.Id + ",";
                }
                if (room.IsCompleted)
                {
                    completedRooms += room.Id + ",";
                }
            }
            
            PlayerPrefs.SetString($"MapManager_Visited_{CurrentFloor}", visitedRooms);
            PlayerPrefs.SetString($"MapManager_Completed_{CurrentFloor}", completedRooms);
        }

        /// <summary>
        /// 加载房间状态
        /// </summary>
        private void LoadRoomStates()
        {
            if (CurrentMap == null) return;
            
            string visitedRooms = PlayerPrefs.GetString($"MapManager_Visited_{CurrentFloor}", "");
            string completedRooms = PlayerPrefs.GetString($"MapManager_Completed_{CurrentFloor}", "");
            
            // 恢复已访问状态
            if (!string.IsNullOrEmpty(visitedRooms))
            {
                string[] visitedIds = visitedRooms.Split(',');
                foreach (string idStr in visitedIds)
                {
                    if (int.TryParse(idStr, out int roomId))
                    {
                        Room room = CurrentMap.GetRoom(roomId);
                        if (room != null)
                        {
                            room.IsVisited = true;
                        }
                    }
                }
            }
            
            // 恢复已完成状态
            if (!string.IsNullOrEmpty(completedRooms))
            {
                string[] completedIds = completedRooms.Split(',');
                foreach (string idStr in completedIds)
                {
                    if (int.TryParse(idStr, out int roomId))
                    {
                        Room room = CurrentMap.GetRoom(roomId);
                        if (room != null)
                        {
                            room.IsCompleted = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 清除存档
        /// </summary>
        public void ClearSave()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY_HAS_SAVE);
            PlayerPrefs.DeleteKey(SAVE_KEY_FLOOR);
            PlayerPrefs.DeleteKey(SAVE_KEY_ROOM);
            PlayerPrefs.DeleteKey(SAVE_KEY_SEED);
            
            // 清除所有楼层的房间状态
            for (int i = 1; i <= 10; i++)
            {
                PlayerPrefs.DeleteKey($"MapManager_Visited_{i}");
                PlayerPrefs.DeleteKey($"MapManager_Completed_{i}");
            }
            
            PlayerPrefs.Save();
            
            if (debugMode)
            {
                Debug.Log("[MapManager] 存档已清除");
            }
        }

        /// <summary>
        /// 检查是否有存档
        /// </summary>
        public bool HasSave()
        {
            return PlayerPrefs.GetInt(SAVE_KEY_HAS_SAVE, 0) == 1;
        }

        /// <summary>
        /// 生成新楼层
        /// </summary>
        public void GenerateNewFloor(int floorNumber)
        {
            CurrentFloor = floorNumber;
            
            // 生成随机种子并保存
            int seed = System.Environment.TickCount;
            Random.InitState(seed);
            
            CurrentMap = MapGenerator.GenerateMap(floorNumber, minRoomsPerFloor, maxRoomsPerFloor);
            CurrentMap.Seed = seed;
            
            if (debugMode)
            {
                Debug.Log($"[MapManager] 生成第 {floorNumber} 层地图，共 {CurrentMap.RoomCount} 个房间，种子:{seed}");
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

            // 自动保存
            if (autoSave)
            {
                SaveGame();
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

