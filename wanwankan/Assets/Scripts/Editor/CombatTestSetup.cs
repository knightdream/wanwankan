using UnityEngine;
using UnityEditor;
using WanWanKan.Core;
using WanWanKan.Combat;
using WanWanKan.Character;
using WanWanKan.Map;
using WanWanKan.UI;

namespace WanWanKan.Editor
{
    /// <summary>
    /// 战斗测试场景快速设置工具
    /// </summary>
    public class CombatTestSetup : EditorWindow
    {
        [MenuItem("WanWanKan/战斗测试/快速设置测试场景")]
        public static void ShowWindow()
        {
            GetWindow<CombatTestSetup>("战斗测试设置");
        }

        private void OnGUI()
        {
            GUILayout.Label("战斗系统测试场景设置", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "此工具会自动创建战斗测试所需的所有GameObject和组件。\n" +
                "运行后，按 B 键开始测试战斗。",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("创建最小测试场景（仅战斗）", GUILayout.Height(30)))
            {
                CreateMinimalTestScene();
            }

            if (GUILayout.Button("创建完整测试场景（战斗+地图）", GUILayout.Height(30)))
            {
                CreateFullTestScene();
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            if (GUILayout.Button("检查场景设置", GUILayout.Height(25)))
            {
                CheckSceneSetup();
            }

            if (GUILayout.Button("清理测试对象", GUILayout.Height(25)))
            {
                CleanupTestObjects();
            }
        }

        /// <summary>
        /// 创建最小测试场景（仅战斗系统）
        /// </summary>
        private static void CreateMinimalTestScene()
        {
            Debug.Log("[CombatTestSetup] 开始创建最小测试场景...");

            // 1. GameManager
            CreateOrGetComponent<GameManager>("GameManager");

            // 2. CombatManager
            CreateOrGetComponent<CombatManager>("CombatManager");

            // 3. CombatVisualEffects
            CreateOrGetComponent<CombatVisualEffects>("CombatVisualEffects");

            // 4. CharacterSelection
            CreateOrGetComponent<CharacterSelection>("CharacterSelection");

            // 5. DiceSystem
            CreateOrGetComponent<DiceSystem>("DiceSystem");

            // 6. Canvas + CombatUI
            CreateCombatUI();

            Debug.Log("[CombatTestSetup] 最小测试场景创建完成！");
            Debug.Log("[CombatTestSetup] 运行游戏后，按 B 键开始测试战斗。");
            
            EditorUtility.DisplayDialog("完成", 
                "最小测试场景已创建！\n\n" +
                "运行游戏后：\n" +
                "- 按 B 键开始测试战斗\n" +
                "- 按 T 键测试骰子系统\n" +
                "- 按 G 键模拟气力系统\n" +
                "- 按 ESC 键强制结束战斗", 
                "确定");
        }

        /// <summary>
        /// 创建完整测试场景（战斗+地图系统）
        /// </summary>
        private static void CreateFullTestScene()
        {
            Debug.Log("[CombatTestSetup] 开始创建完整测试场景...");

            // 创建最小测试场景的所有内容
            CreateMinimalTestScene();

            // 7. MapManager
            CreateOrGetComponent<MapManager>("MapManager");

            // 8. RoomHandler
            CreateOrGetComponent<RoomHandler>("RoomHandler");

            Debug.Log("[CombatTestSetup] 完整测试场景创建完成！");
            Debug.Log("[CombatTestSetup] 运行游戏后，地图会自动生成，进入战斗房间会自动触发战斗。");
            
            EditorUtility.DisplayDialog("完成", 
                "完整测试场景已创建！\n\n" +
                "运行游戏后：\n" +
                "- 地图会自动生成\n" +
                "- 进入战斗房间会自动触发战斗\n" +
                "- 也可以按 B 键直接开始测试战斗", 
                "确定");
        }

        /// <summary>
        /// 创建或获取组件
        /// </summary>
        private static T CreateOrGetComponent<T>(string name) where T : Component
        {
            // 先查找是否已存在
            T existing = Object.FindObjectOfType<T>();
            if (existing != null)
            {
                Debug.Log($"[CombatTestSetup] {name} 已存在，跳过创建");
                return existing;
            }

            // 创建新的GameObject
            GameObject go = new GameObject(name);
            T component = go.AddComponent<T>();
            
            Debug.Log($"[CombatTestSetup] 创建 {name}");
            return component;
        }

        /// <summary>
        /// 创建CombatUI
        /// </summary>
        private static void CreateCombatUI()
        {
            // 检查是否已存在
            CombatUI existing = Object.FindObjectOfType<CombatUI>();
            if (existing != null)
            {
                Debug.Log("[CombatTestSetup] CombatUI 已存在，跳过创建");
                return;
            }

            // 查找或创建Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Debug.Log("[CombatTestSetup] 创建 Canvas");
            }

            // 创建CombatUI GameObject
            GameObject combatUIGO = new GameObject("CombatUI");
            combatUIGO.transform.SetParent(canvas.transform, false);
            CombatUI combatUI = combatUIGO.AddComponent<CombatUI>();

            Debug.Log("[CombatTestSetup] 创建 CombatUI（需要手动配置UI引用）");
            Debug.LogWarning("[CombatTestSetup] 请运行 'WanWanKan → 一键创建完整战斗UI' 来创建完整的UI结构");
        }

        /// <summary>
        /// 检查场景设置
        /// </summary>
        private static void CheckSceneSetup()
        {
            Debug.Log("=== 场景设置检查 ===");

            CheckComponent<GameManager>("GameManager");
            CheckComponent<CombatManager>("CombatManager");
            CheckComponent<CombatVisualEffects>("CombatVisualEffects");
            CheckComponent<CharacterSelection>("CharacterSelection");
            CheckComponent<DiceSystem>("DiceSystem");
            CheckComponent<CombatUI>("CombatUI");
            CheckComponent<MapManager>("MapManager");
            CheckComponent<RoomHandler>("RoomHandler");

            Debug.Log("====================");
            
            EditorUtility.DisplayDialog("检查完成", 
                "检查结果已输出到Console窗口。\n\n" +
                "如果缺少组件，请点击相应的创建按钮。", 
                "确定");
        }

        private static void CheckComponent<T>(string name) where T : Component
        {
            T component = Object.FindObjectOfType<T>();
            if (component != null)
            {
                Debug.Log($"✓ {name}: 已存在");
            }
            else
            {
                Debug.LogWarning($"✗ {name}: 不存在");
            }
        }

        /// <summary>
        /// 清理测试对象
        /// </summary>
        private static void CleanupTestObjects()
        {
            if (!EditorUtility.DisplayDialog("确认清理", 
                "这将删除场景中所有测试相关的GameObject。\n\n" +
                "确定要继续吗？", 
                "确定", "取消"))
            {
                return;
            }

            Debug.Log("[CombatTestSetup] 开始清理测试对象...");

            DestroyObject<GameManager>();
            DestroyObject<CombatManager>();
            DestroyObject<CombatVisualEffects>();
            DestroyObject<CharacterSelection>();
            DestroyObject<DiceSystem>();
            DestroyObject<CombatUI>();
            DestroyObject<MapManager>();
            DestroyObject<RoomHandler>();

            Debug.Log("[CombatTestSetup] 清理完成！");
        }

        private static void DestroyObject<T>() where T : Component
        {
            T[] components = Object.FindObjectsOfType<T>();
            foreach (T component in components)
            {
                Debug.Log($"[CombatTestSetup] 删除 {component.name}");
                DestroyImmediate(component.gameObject);
            }
        }
    }
}
