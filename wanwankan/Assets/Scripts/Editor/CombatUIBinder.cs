#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using WanWanKan.UI;

namespace WanWanKan.Editor
{
    /// <summary>
    /// 自动绑定CombatUI引用的工具
    /// </summary>
    public class CombatUIBinder : UnityEditor.Editor
    {
        [MenuItem("WanWanKan/自动绑定CombatUI引用")]
        public static void AutoBindCombatUI()
        {
            // 查找场景中的CombatUI
            CombatUI combatUI = FindObjectOfType<CombatUI>();
            if (combatUI == null)
            {
                EditorUtility.DisplayDialog("错误", "场景中没有找到 CombatUI 组件！", "确定");
                return;
            }

            SerializedObject so = new SerializedObject(combatUI);
            Transform root = combatUI.transform;

            // 绑定容器
            BindTransform(so, "playerGaugeContainer", root, "PlayerGauges");
            BindTransform(so, "enemyGaugeContainer", root, "EnemyGauges");
            BindTransform(so, "actionOrderContainer", root, "ActionOrderPreview");
            BindTransform(so, "targetButtonContainer", root, "TargetSelectionPanel/TargetButtonContainer");

            // 绑定面板
            BindGameObject(so, "actionMenuPanel", root, "ActionMenu");
            BindGameObject(so, "targetSelectionPanel", root, "TargetSelectionPanel");
            BindGameObject(so, "victoryPanel", root, "VictoryPanel");
            BindGameObject(so, "defeatPanel", root, "DefeatPanel");

            // 绑定按钮
            BindButton(so, "attackButton", root, "ActionMenu/AttackButton");
            BindButton(so, "defendButton", root, "ActionMenu/DefendButton");
            BindButton(so, "skillButton", root, "ActionMenu/SkillButton");
            BindButton(so, "itemButton", root, "ActionMenu/ItemButton");

            // 绑定文本
            BindTMPText(so, "combatLogText", root, "CombatLog/Viewport/Content/LogText");
            BindTMPText(so, "battleStateText", root, "BattleStateText");

            // 绑定ScrollRect
            BindScrollRect(so, "combatLogScroll", root, "CombatLog");

            // 绑定预制体
            BindPrefab(so, "actionGaugePrefab", "Assets/Prefabs/UI/ActionGauge.prefab");
            BindPrefab(so, "targetButtonPrefab", "Assets/Prefabs/UI/TargetButton.prefab");
            BindPrefab(so, "actionOrderIconPrefab", "Assets/Prefabs/UI/ActionOrderIcon.prefab");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(combatUI);

            Debug.Log("✅ CombatUI 引用绑定完成！");
            EditorUtility.DisplayDialog("成功", "CombatUI 引用已自动绑定！", "确定");
        }

        private static void BindTransform(SerializedObject so, string propertyName, Transform root, string path)
        {
            Transform target = root.Find(path);
            if (target != null)
            {
                so.FindProperty(propertyName).objectReferenceValue = target;
                Debug.Log($"绑定 {propertyName} → {path}");
            }
            else
            {
                Debug.LogWarning($"未找到: {path}");
            }
        }

        private static void BindGameObject(SerializedObject so, string propertyName, Transform root, string path)
        {
            Transform target = root.Find(path);
            if (target != null)
            {
                so.FindProperty(propertyName).objectReferenceValue = target.gameObject;
                Debug.Log($"绑定 {propertyName} → {path}");
            }
            else
            {
                Debug.LogWarning($"未找到: {path}");
            }
        }

        private static void BindButton(SerializedObject so, string propertyName, Transform root, string path)
        {
            Transform target = root.Find(path);
            if (target != null)
            {
                Button btn = target.GetComponent<Button>();
                if (btn != null)
                {
                    so.FindProperty(propertyName).objectReferenceValue = btn;
                    Debug.Log($"绑定 {propertyName} → {path}");
                }
            }
            else
            {
                Debug.LogWarning($"未找到: {path}");
            }
        }

        private static void BindTMPText(SerializedObject so, string propertyName, Transform root, string path)
        {
            Transform target = root.Find(path);
            if (target != null)
            {
                TextMeshProUGUI tmp = target.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    so.FindProperty(propertyName).objectReferenceValue = tmp;
                    Debug.Log($"绑定 {propertyName} → {path}");
                }
            }
            else
            {
                Debug.LogWarning($"未找到: {path}");
            }
        }

        private static void BindScrollRect(SerializedObject so, string propertyName, Transform root, string path)
        {
            Transform target = root.Find(path);
            if (target != null)
            {
                ScrollRect scroll = target.GetComponent<ScrollRect>();
                if (scroll != null)
                {
                    so.FindProperty(propertyName).objectReferenceValue = scroll;
                    Debug.Log($"绑定 {propertyName} → {path}");
                }
            }
            else
            {
                Debug.LogWarning($"未找到: {path}");
            }
        }

        private static void BindPrefab(SerializedObject so, string propertyName, string assetPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab != null)
            {
                so.FindProperty(propertyName).objectReferenceValue = prefab;
                Debug.Log($"绑定预制体 {propertyName} → {assetPath}");
            }
            else
            {
                Debug.LogWarning($"未找到预制体: {assetPath}");
            }
        }
    }
}
#endif

