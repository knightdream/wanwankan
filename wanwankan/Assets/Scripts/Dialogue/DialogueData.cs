using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dialogue
{
    /// <summary>
    /// 单条对话数据
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        /// <summary>
        /// 对话ID（用于跳转）
        /// </summary>
        public int id;

        /// <summary>
        /// 说话者名称
        /// </summary>
        public string speakerName;

        /// <summary>
        /// 对话文字内容
        /// </summary>
        public string content;

        /// <summary>
        /// 说话者位置：left 或 right
        /// </summary>
        public string position = "left";

        /// <summary>
        /// 左侧人物立绘资源名（Resources/Characters/下的图片名）
        /// </summary>
        public string leftCharacterImage;

        /// <summary>
        /// 右侧人物立绘资源名
        /// </summary>
        public string rightCharacterImage;

        /// <summary>
        /// 是否高亮左侧人物（非说话者变暗）
        /// </summary>
        public bool highlightLeft = true;

        /// <summary>
        /// 是否高亮右侧人物
        /// </summary>
        public bool highlightRight = false;

        /// <summary>
        /// 是否逐字显示文字
        /// </summary>
        public bool typewriterEffect = true;

        /// <summary>
        /// 逐字显示速度（每个字的间隔时间，秒）
        /// </summary>
        public float typewriterSpeed = 0.05f;

        /// <summary>
        /// 是否可以点击跳过逐字效果
        /// </summary>
        public bool canSkipTypewriter = true;

        /// <summary>
        /// 是否自动播放下一句（不需要点击）
        /// </summary>
        public bool autoPlay = false;

        /// <summary>
        /// 自动播放延迟时间（秒）
        /// </summary>
        public float autoPlayDelay = 2f;

        /// <summary>
        /// 对话结束后触发的事件名称（可选）
        /// </summary>
        public string triggerEvent;

        /// <summary>
        /// 对话选项（可选，用于分支对话）
        /// </summary>
        public List<DialogueChoice> choices;

        /// <summary>
        /// 选项是否逐字显示
        /// </summary>
        public bool choicesTypewriterEffect = false;

        /// <summary>
        /// 选项逐字显示速度（秒/字）
        /// </summary>
        public float choicesTypewriterSpeed = 0.03f;

        /// <summary>
        /// 所有选项是否同时开始逐字显示（true=同时，false=依次）
        /// </summary>
        public bool choicesAppearSimultaneously = true;

        /// <summary>
        /// 选项之间的出现延迟（仅当choicesAppearSimultaneously=false时生效）
        /// </summary>
        public float choicesAppearDelay = 0.3f;

        /// <summary>
        /// 选项出现前的延迟（对话文字显示完后等待多久显示选项）
        /// </summary>
        public float choicesShowDelay = 0.5f;
    }

    /// <summary>
    /// 对话选项（用于分支）
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        /// <summary>
        /// 选项文字
        /// </summary>
        public string text;

        /// <summary>
        /// 选择后跳转的对话ID
        /// </summary>
        public int nextDialogueId;

        /// <summary>
        /// 选择后触发的事件
        /// </summary>
        public string triggerEvent;

        /// <summary>
        /// 善恶值变化
        /// </summary>
        public int moralityChange = 0;
    }

    /// <summary>
    /// 对话配置（一整段对话）
    /// </summary>
    [Serializable]
    public class DialogueConfig
    {
        /// <summary>
        /// 对话配置ID
        /// </summary>
        public string dialogueId;

        /// <summary>
        /// 对话标题（用于编辑器识别）
        /// </summary>
        public string title;

        /// <summary>
        /// 全局默认设置：是否使用逐字效果
        /// </summary>
        public bool defaultTypewriterEffect = true;

        /// <summary>
        /// 全局默认设置：逐字速度
        /// </summary>
        public float defaultTypewriterSpeed = 0.05f;

        /// <summary>
        /// 全局默认设置：是否可跳过逐字
        /// </summary>
        public bool defaultCanSkipTypewriter = true;

        /// <summary>
        /// 对话背景图片（可选）
        /// </summary>
        public string backgroundImage;

        /// <summary>
        /// 背景音乐（可选）
        /// </summary>
        public string bgmName;

        /// <summary>
        /// 对话内容列表
        /// </summary>
        public List<DialogueLine> dialogues;

        /// <summary>
        /// 对话结束后触发的事件
        /// </summary>
        public string onCompleteEvent;
    }

    /// <summary>
    /// 对话数据加载器
    /// </summary>
    public static class DialogueLoader
    {
        /// <summary>
        /// 从Resources加载对话配置
        /// </summary>
        /// <param name="dialogueName">对话文件名（不含扩展名）</param>
        /// <returns>对话配置</returns>
        public static DialogueConfig LoadDialogue(string dialogueName)
        {
            TextAsset jsonFile = Resources.Load<TextAsset>($"Dialogues/{dialogueName}");
            if (jsonFile == null)
            {
                Debug.LogError($"[DialogueLoader] 找不到对话配置: Dialogues/{dialogueName}");
                return null;
            }

            try
            {
                DialogueConfig config = JsonUtility.FromJson<DialogueConfig>(jsonFile.text);
                
                // 应用默认设置到每条对话
                if (config.dialogues != null)
                {
                    foreach (var line in config.dialogues)
                    {
                        // 如果单条对话没有设置，使用全局默认值
                        if (line.typewriterSpeed <= 0)
                            line.typewriterSpeed = config.defaultTypewriterSpeed;
                    }
                }

                Debug.Log($"[DialogueLoader] 成功加载对话: {config.title} ({config.dialogues?.Count ?? 0}条)");
                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueLoader] 解析对话配置失败: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从JSON字符串加载对话配置
        /// </summary>
        public static DialogueConfig LoadDialogueFromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<DialogueConfig>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueLoader] 解析JSON失败: {e.Message}");
                return null;
            }
        }
    }
}

