using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WanWanKan.UI
{
    /// <summary>
    /// 字体修复工具 - 确保所有TextMeshPro组件使用中文字体
    /// </summary>
    public static class FontFixer
    {
        // 中文字体路径（多个可能的位置）
        private static readonly string[] CHINESE_FONT_PATHS = new string[]
        {
            "Assets/Resources/Font/SourceHanSansSC-Normal SDF.asset",  // 首选位置
            "Assets/TextMesh Pro/Resources/Fonts & Materials/SourceHanSansSC-Normal SDF.asset",  // TextMeshPro目录
        };
        
        // 运行时字体资源
        private static TMP_FontAsset _chineseFontAsset;
        private static TMP_FontAsset _fallbackFontAsset;
        
        /// <summary>
        /// 获取Fallback字体资源（用于显示数字和英文）
        /// </summary>
        private static TMP_FontAsset GetFallbackFont()
        {
            if (_fallbackFontAsset == null)
            {
                // 方法1: 尝试从Resources加载
                _fallbackFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
                
                // 方法2: 尝试从TextMeshPro Resources加载
                if (_fallbackFontAsset == null)
                {
                    _fallbackFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                }
                
                // 方法3: 编辑器模式下从路径加载
                if (_fallbackFontAsset == null)
                {
                    #if UNITY_EDITOR
                    string[] fallbackPaths = new string[]
                    {
                        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset",
                        "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset"
                    };
                    
                    foreach (string path in fallbackPaths)
                    {
                        _fallbackFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                        if (_fallbackFontAsset != null)
                        {
                            Debug.Log($"[FontFixer] 从路径加载Fallback字体: {path}");
                            break;
                        }
                    }
                    #endif
                }
                
                // 方法4: 使用TMP默认字体作为fallback
                if (_fallbackFontAsset == null && TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
                {
                    _fallbackFontAsset = TMP_Settings.defaultFontAsset;
                }
            }
            
            return _fallbackFontAsset;
        }
        
        /// <summary>
        /// 获取中文字体资源（运行时）
        /// </summary>
        public static TMP_FontAsset GetChineseFont()
        {
            if (_chineseFontAsset == null)
            {
                // 方法1: 尝试从Resources加载（运行时）
                _chineseFontAsset = Resources.Load<TMP_FontAsset>("Font/SourceHanSansSC-Normal SDF");
                
                // 方法2: 尝试从TextMeshPro Resources加载（运行时）
                if (_chineseFontAsset == null)
                {
                    _chineseFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/SourceHanSansSC-Normal SDF");
                }
                
                // 方法3: 尝试直接路径加载（编辑器模式）
                if (_chineseFontAsset == null)
                {
                    #if UNITY_EDITOR
                    foreach (string path in CHINESE_FONT_PATHS)
                    {
                        _chineseFontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                        if (_chineseFontAsset != null)
                        {
                            Debug.Log($"[FontFixer] 从路径加载中文字体: {path}");
                            break;
                        }
                    }
                    #endif
                }
                
                // 方法4: 搜索项目中所有中文字体（编辑器模式）
                if (_chineseFontAsset == null)
                {
                    #if UNITY_EDITOR
                    string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                        if (font != null && IsChineseFont(font))
                        {
                            _chineseFontAsset = font;
                            Debug.Log($"[FontFixer] 自动找到中文字体: {path}");
                            break;
                        }
                    }
                    #endif
                }
                
                // 方法5: 如果还是找不到，使用TMP的默认字体（不报错）
                if (_chineseFontAsset == null)
                {
                    // 使用TMP Settings的默认字体
                    if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
                    {
                        _chineseFontAsset = TMP_Settings.defaultFontAsset;
                        Debug.LogWarning("[FontFixer] 未找到中文字体，使用TextMeshPro默认字体（可能不支持中文）");
                    }
                }
                else
                {
                    Debug.Log($"[FontFixer] 成功加载中文字体: {_chineseFontAsset.name}");
                }
            }
            
            return _chineseFontAsset;
        }
        
        /// <summary>
        /// 设置TextMeshPro组件使用中文字体
        /// </summary>
        public static void SetChineseFont(TextMeshProUGUI tmp)
        {
            if (tmp == null) return;
            
            var font = GetChineseFont();
            if (font != null)
            {
                tmp.font = font;
                
                // 设置fallback字体以支持数字和英文
                var fallbackFont = GetFallbackFont();
                if (fallbackFont != null && font.fallbackFontAssetTable != null)
                {
                    // 如果fallback列表为空或没有包含fallback字体，则添加
                    if (font.fallbackFontAssetTable.Count == 0 || 
                        !font.fallbackFontAssetTable.Contains(fallbackFont))
                    {
                        #if UNITY_EDITOR
                        // 编辑器模式下直接修改字体资源
                        if (!font.fallbackFontAssetTable.Contains(fallbackFont))
                        {
                            font.fallbackFontAssetTable.Add(fallbackFont);
                            UnityEditor.EditorUtility.SetDirty(font);
                        }
                        #else
                        // 运行时模式下，通过组件设置fallback
                        // 注意：运行时无法直接修改字体资源的fallback列表
                        // 但可以通过TMP Settings全局fallback来处理
                        #endif
                    }
                }
                
                // 强制刷新文本显示
                tmp.ForceMeshUpdate();
            }
            else
            {
                // 如果找不到中文字体，至少确保有字体（使用TMP默认字体）
                if (tmp.font == null && TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
                {
                    tmp.font = TMP_Settings.defaultFontAsset;
                    tmp.ForceMeshUpdate();
                }
            }
        }
        
        /// <summary>
        /// 设置TextMeshPro组件使用中文字体（3D版本）
        /// </summary>
        public static void SetChineseFont(TextMeshPro tmp)
        {
            if (tmp == null) return;
            
            var font = GetChineseFont();
            if (font != null)
            {
                tmp.font = font;
                
                // 设置fallback字体以支持数字和英文
                var fallbackFont = GetFallbackFont();
                if (fallbackFont != null && font.fallbackFontAssetTable != null)
                {
                    // 如果fallback列表为空或没有包含fallback字体，则添加
                    if (font.fallbackFontAssetTable.Count == 0 || 
                        !font.fallbackFontAssetTable.Contains(fallbackFont))
                    {
                        #if UNITY_EDITOR
                        // 编辑器模式下直接修改字体资源
                        if (!font.fallbackFontAssetTable.Contains(fallbackFont))
                        {
                            font.fallbackFontAssetTable.Add(fallbackFont);
                            UnityEditor.EditorUtility.SetDirty(font);
                        }
                        #endif
                    }
                }
                
                // 强制刷新文本显示
                tmp.ForceMeshUpdate();
            }
            else
            {
                // 如果找不到中文字体，至少确保有字体（使用TMP默认字体）
                if (tmp.font == null && TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null)
                {
                    tmp.font = TMP_Settings.defaultFontAsset;
                    tmp.ForceMeshUpdate();
                }
            }
        }
        
        /// <summary>
        /// 修复场景中所有TextMeshPro组件的字体
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void FixAllFontsInScene()
        {
            // 先尝试获取中文字体
            var chineseFont = GetChineseFont();
            if (chineseFont == null)
            {
                Debug.LogWarning("[FontFixer] 未找到中文字体资源，部分中文可能无法正确显示。请确保字体资源存在于: Assets/Resources/Font/SourceHanSansSC-Normal SDF.asset");
            }
            
            // 修复所有TextMeshProUGUI
            TextMeshProUGUI[] allTexts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int fixedCount = 0;
            
            foreach (var text in allTexts)
            {
                if (text.font == null || !IsChineseFont(text.font))
                {
                    SetChineseFont(text);
                    fixedCount++;
                }
            }
            
            // 修复所有TextMeshPro（3D）
            TextMeshPro[] allTexts3D = Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var text in allTexts3D)
            {
                if (text.font == null || !IsChineseFont(text.font))
                {
                    SetChineseFont(text);
                    fixedCount++;
                }
            }
            
            if (fixedCount > 0)
            {
                Debug.Log($"[FontFixer] 已修复 {fixedCount} 个TextMeshPro组件的字体");
            }
        }
        
        /// <summary>
        /// 检查字体是否是中文字体
        /// </summary>
        private static bool IsChineseFont(TMP_FontAsset font)
        {
            if (font == null) return false;
            
            // 检查字体名称是否包含中文相关关键词
            string fontName = font.name.ToLower();
            return fontName.Contains("han") || 
                   fontName.Contains("chinese") || 
                   fontName.Contains("sourcehan") ||
                   fontName.Contains("思源");
        }
        
        /// <summary>
        /// 修复指定GameObject及其子对象的所有TextMeshPro组件
        /// </summary>
        public static void FixFontsInGameObject(GameObject target)
        {
            if (target == null) return;
            
            TextMeshProUGUI[] texts = target.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                SetChineseFont(text);
            }
            
            TextMeshPro[] texts3D = target.GetComponentsInChildren<TextMeshPro>(true);
            foreach (var text in texts3D)
            {
                SetChineseFont(text);
            }
        }
    }
}
