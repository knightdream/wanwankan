#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;
using WanWanKan.UI;

namespace WanWanKan.Editor
{
    /// <summary>
    /// 字体修复编辑器工具
    /// </summary>
    public class FontFixerEditor : EditorWindow
    {
        [MenuItem("WanWanKan/字体修复/修复场景中所有字体")]
        public static void FixAllFontsInScene()
        {
            // 先尝试获取中文字体
            var font = FontFixer.GetChineseFont();
            if (font == null)
            {
                EditorUtility.DisplayDialog("警告", 
                    "未找到中文字体资源！\n\n" +
                    "请确保字体资源存在于:\n" +
                    "Assets/Resources/Font/SourceHanSansSC-Normal SDF.asset\n\n" +
                    "将使用TextMeshPro默认字体（可能不支持中文）", 
                    "继续");
            }
            
            FontFixer.FixAllFontsInScene();
            EditorUtility.DisplayDialog("完成", "已修复场景中所有TextMeshPro组件的字体", "确定");
        }
        
        [MenuItem("WanWanKan/字体修复/修复选中对象的所有字体")]
        public static void FixFontsInSelection()
        {
            if (Selection.activeGameObject == null)
            {
                EditorUtility.DisplayDialog("错误", "请先选择一个GameObject", "确定");
                return;
            }
            
            FontFixer.FixFontsInGameObject(Selection.activeGameObject);
            EditorUtility.DisplayDialog("完成", $"已修复 {Selection.activeGameObject.name} 及其子对象的所有字体", "确定");
        }
        
        [MenuItem("WanWanKan/字体修复/修复所有预制体中的字体")]
        public static void FixFontsInAllPrefabs()
        {
            if (!EditorUtility.DisplayDialog("确认", 
                "这将修改项目中所有预制体的字体设置。\n\n确定要继续吗？", 
                "确定", "取消"))
            {
                return;
            }
            
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            int fixedCount = 0;
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    bool modified = false;
                    TextMeshProUGUI[] texts = prefab.GetComponentsInChildren<TextMeshProUGUI>(true);
                    
                    foreach (var text in texts)
                    {
                        if (text.font == null || !IsChineseFont(text.font))
                        {
                            var font = FontFixer.GetChineseFont();
                            if (font != null)
                            {
                                text.font = font;
                                modified = true;
                                fixedCount++;
                            }
                        }
                    }
                    
                    if (modified)
                    {
                        EditorUtility.SetDirty(prefab);
                    }
                }
            }
            
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("完成", $"已修复 {fixedCount} 个TextMeshPro组件的字体", "确定");
        }
        
        [MenuItem("WanWanKan/字体修复/配置中文字体Fallback")]
        public static void ConfigureChineseFontFallback()
        {
            var chineseFont = FontFixer.GetChineseFont();
            if (chineseFont == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到中文字体资源！\n\n请先确保字体资源存在。", "确定");
                return;
            }
            
            // 获取fallback字体
            TMP_FontAsset fallbackFont = null;
            string[] fallbackPaths = new string[]
            {
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset",
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"
            };
            
            foreach (string path in fallbackPaths)
            {
                fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (fallbackFont != null) break;
            }
            
            if (fallbackFont == null && TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
            {
                fallbackFont = TMP_Settings.defaultFontAsset;
            }
            
            if (fallbackFont == null)
            {
                EditorUtility.DisplayDialog("错误", "未找到Fallback字体资源！", "确定");
                return;
            }
            
            // 配置fallback
            if (chineseFont.fallbackFontAssetTable == null)
            {
                chineseFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            }
            
            if (!chineseFont.fallbackFontAssetTable.Contains(fallbackFont))
            {
                chineseFont.fallbackFontAssetTable.Add(fallbackFont);
                EditorUtility.SetDirty(chineseFont);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("完成", 
                    $"已为字体 '{chineseFont.name}' 配置Fallback字体 '{fallbackFont.name}'\n\n" +
                    "现在数字和英文应该可以正确显示了。", 
                    "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", 
                    $"字体 '{chineseFont.name}' 已经配置了Fallback字体 '{fallbackFont.name}'", 
                    "确定");
            }
        }
        
        [MenuItem("WanWanKan/字体修复/打开字体修复窗口")]
        public static void ShowWindow()
        {
            GetWindow<FontFixerEditor>("字体修复工具");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("字体修复工具", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "此工具用于修复项目中所有TextMeshPro组件的字体设置，\n" +
                "确保中文和英文都能正确显示。",
                MessageType.Info);
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("修复场景中所有字体", GUILayout.Height(30)))
            {
                FixAllFontsInScene();
            }
            
            if (GUILayout.Button("修复选中对象的所有字体", GUILayout.Height(30)))
            {
                FixFontsInSelection();
            }
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            EditorGUILayout.HelpBox(
                "⚠️ 批量修复预制体会修改项目文件，建议先备份！",
                MessageType.Warning);
            
            if (GUILayout.Button("修复所有预制体中的字体", GUILayout.Height(30)))
            {
                FixFontsInAllPrefabs();
            }
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            EditorGUILayout.HelpBox(
                "💡 如果数字显示为方块，请点击下方按钮配置Fallback字体",
                MessageType.Info);
            
            if (GUILayout.Button("配置中文字体Fallback（修复数字显示）", GUILayout.Height(30)))
            {
                ConfigureChineseFontFallback();
            }
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // 显示当前字体信息
            var font = FontFixer.GetChineseFont();
            if (font != null)
            {
                EditorGUILayout.LabelField("当前中文字体:", font.name);
                
                // 检查是否是中文字体
                if (IsChineseFont(font))
                {
                    EditorGUILayout.HelpBox("✓ 已找到中文字体，中文应该可以正常显示", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("⚠️ 当前字体可能不支持中文，中文可能显示为方块", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("未找到中文字体！\n\n请检查以下位置：\n" +
                    "1. Assets/Resources/Font/SourceHanSansSC-Normal SDF.asset\n" +
                    "2. Assets/TextMesh Pro/Resources/Fonts & Materials/SourceHanSansSC-Normal SDF.asset\n\n" +
                    "如果字体存在但仍未找到，请点击下方按钮手动选择字体。", 
                    MessageType.Error);
                
                if (GUILayout.Button("手动选择中文字体资源", GUILayout.Height(30)))
                {
                    string path = EditorUtility.OpenFilePanel("选择中文字体资源", "Assets", "asset");
                    if (!string.IsNullOrEmpty(path))
                    {
                        // 转换为相对路径
                        if (path.StartsWith(Application.dataPath))
                        {
                            path = "Assets" + path.Substring(Application.dataPath.Length);
                            TMP_FontAsset selectedFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                            if (selectedFont != null)
                            {
                                // 设置到所有TextMeshPro组件
                                TextMeshProUGUI[] allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                                foreach (var text in allTexts)
                                {
                                    text.font = selectedFont;
                                    EditorUtility.SetDirty(text);
                                }
                                
                                EditorUtility.DisplayDialog("完成", $"已设置 {allTexts.Length} 个TextMeshPro组件使用字体: {selectedFont.name}", "确定");
                            }
                        }
                    }
                }
            }
        }
        
        private static bool IsChineseFont(TMP_FontAsset font)
        {
            if (font == null) return false;
            
            string fontName = font.name.ToLower();
            return fontName.Contains("han") || 
                   fontName.Contains("chinese") || 
                   fontName.Contains("sourcehan") ||
                   fontName.Contains("思源");
        }
    }
}
#endif
