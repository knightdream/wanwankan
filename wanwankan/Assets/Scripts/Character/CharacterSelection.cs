using System.Collections.Generic;
using UnityEngine;

namespace WanWanKan.Character
{
    /// <summary>
    /// 角色数据配置
    /// </summary>
    [System.Serializable]
    public class CharacterConfig
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int BaseSTR { get; set; }
        public int BaseAGI { get; set; }
        public int BaseWIS { get; set; }
        public Sprite Portrait { get; set; }
        public Sprite BattleSprite { get; set; }

        public CharacterConfig(string id, string name, int str, int agi, int wis, string description = "")
        {
            Id = id;
            Name = name;
            BaseSTR = str;
            BaseAGI = agi;
            BaseWIS = wis;
            Description = description;
        }
    }

    /// <summary>
    /// 角色选择管理器 - 类似杀戮尖塔，单次战斗只有1个主角
    /// </summary>
    public class CharacterSelection : MonoBehaviour
    {
        public static CharacterSelection Instance { get; private set; }

        [Header("角色配置")]
        [SerializeField] private List<CharacterConfig> availableCharacters = new List<CharacterConfig>();

        [Header("当前选择")]
        [SerializeField] private CharacterConfig currentCharacter;

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
            // 如果没有角色，创建默认角色
            if (availableCharacters.Count == 0)
            {
                CreateDefaultCharacters();
            }

            // 如果没有选择角色，选择第一个
            if (currentCharacter == null && availableCharacters.Count > 0)
            {
                currentCharacter = availableCharacters[0];
            }
        }

        /// <summary>
        /// 创建默认角色
        /// </summary>
        private void CreateDefaultCharacters()
        {
            availableCharacters.Add(new CharacterConfig(
                "warrior",
                "战士",
                8, 6, 4,
                "高力量，高血量，适合近战"
            ));

            availableCharacters.Add(new CharacterConfig(
                "rogue",
                "盗贼",
                4, 10, 6,
                "高敏捷，快速行动，适合闪避"
            ));

            availableCharacters.Add(new CharacterConfig(
                "mage",
                "法师",
                3, 6, 10,
                "高智慧，魔法伤害，适合远程"
            ));
        }

        /// <summary>
        /// 获取当前选择的角色
        /// </summary>
        public CharacterConfig GetCurrentCharacter()
        {
            return currentCharacter;
        }

        /// <summary>
        /// 选择角色
        /// </summary>
        public void SelectCharacter(string characterId)
        {
            CharacterConfig character = availableCharacters.Find(c => c.Id == characterId);
            if (character != null)
            {
                currentCharacter = character;
                Debug.Log($"[CharacterSelection] 选择角色: {character.Name}");
            }
            else
            {
                Debug.LogWarning($"[CharacterSelection] 角色 {characterId} 不存在");
            }
        }

        /// <summary>
        /// 选择角色（通过索引）
        /// </summary>
        public void SelectCharacter(int index)
        {
            if (index >= 0 && index < availableCharacters.Count)
            {
                currentCharacter = availableCharacters[index];
                Debug.Log($"[CharacterSelection] 选择角色: {currentCharacter.Name}");
            }
        }

        /// <summary>
        /// 获取所有可用角色
        /// </summary>
        public List<CharacterConfig> GetAvailableCharacters()
        {
            return new List<CharacterConfig>(availableCharacters);
        }

        /// <summary>
        /// 添加角色
        /// </summary>
        public void AddCharacter(CharacterConfig character)
        {
            if (character != null && !availableCharacters.Contains(character))
            {
                availableCharacters.Add(character);
            }
        }

        /// <summary>
        /// 创建战斗单位（基于当前选择的角色）
        /// </summary>
        public Combat.CombatUnit CreatePlayerCombatUnit(GameObject parent)
        {
            if (currentCharacter == null)
            {
                Debug.LogWarning("[CharacterSelection] 没有选择角色，使用默认属性");
                return Combat.CombatUnit.CreatePlayer(parent, "勇者", 8, 10, 6);
            }

            return Combat.CombatUnit.CreatePlayer(
                parent,
                currentCharacter.Name,
                currentCharacter.BaseSTR,
                currentCharacter.BaseAGI,
                currentCharacter.BaseWIS
            );
        }
    }
}
