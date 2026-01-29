using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WanWanKan.Combat;

namespace WanWanKan.Combat
{
    /// <summary>
    /// 战斗视觉特效管理器 - 处理伤害数字、攻击动画、受击效果等
    /// </summary>
    public class CombatVisualEffects : MonoBehaviour
    {
        public static CombatVisualEffects Instance { get; private set; }

        [Header("伤害数字")]
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private Transform damageNumberParent;
        [SerializeField] private float damageNumberLifetime = 1.5f;
        [SerializeField] private float damageNumberSpeed = 2f;
        [SerializeField] private float damageNumberSpread = 50f;

        [Header("治疗数字")]
        [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.2f);
        [SerializeField] private Color damageColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.8f, 0f);

        [Header("攻击动画")]
        [SerializeField] private float attackShakeDuration = 0.2f;
        [SerializeField] private float attackShakeIntensity = 10f;
        [SerializeField] private float attackScaleDuration = 0.15f;
        [SerializeField] private float attackScaleAmount = 1.2f;

        [Header("受击效果")]
        [SerializeField] private float hitFlashDuration = 0.1f;
        [SerializeField] private Color hitFlashColor = new Color(1f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private float hitShakeDuration = 0.15f;
        [SerializeField] private float hitShakeIntensity = 5f;

        [Header("暴击效果")]
        [SerializeField] private float criticalScaleMultiplier = 1.5f;
        [SerializeField] private float criticalShakeIntensity = 15f;

        [Header("死亡效果")]
        [SerializeField] private float deathFadeDuration = 0.8f;
        [SerializeField] private float deathScaleDuration = 0.5f;
        [SerializeField] private float deathFinalScale = 0.3f;

        // 单位UI引用（用于动画）
        private Dictionary<CombatUnit, RectTransform> unitUITransforms = new Dictionary<CombatUnit, RectTransform>();
        private Dictionary<CombatUnit, Image> unitUIImages = new Dictionary<CombatUnit, Image>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 如果没有父对象，创建一个
            if (damageNumberParent == null)
            {
                GameObject parentGO = new GameObject("DamageNumbers");
                parentGO.transform.SetParent(transform);
                damageNumberParent = parentGO.transform;
            }

            // 如果没有伤害数字预制体，创建一个简单的
            if (damageNumberPrefab == null)
            {
                CreateDefaultDamageNumberPrefab();
            }
        }

        /// <summary>
        /// 注册单位UI（用于动画效果）
        /// </summary>
        public void RegisterUnitUI(CombatUnit unit, RectTransform uiTransform, Image uiImage = null)
        {
            if (unit != null && uiTransform != null)
            {
                unitUITransforms[unit] = uiTransform;
                if (uiImage != null)
                {
                    unitUIImages[unit] = uiImage;
                }
            }
        }

        /// <summary>
        /// 取消注册单位UI
        /// </summary>
        public void UnregisterUnitUI(CombatUnit unit)
        {
            unitUITransforms.Remove(unit);
            unitUIImages.Remove(unit);
        }

        /// <summary>
        /// 显示伤害数字
        /// </summary>
        public void ShowDamageNumber(CombatUnit target, int damage, bool isCritical = false, bool isHeal = false)
        {
            if (target == null || damageNumberParent == null) return;

            // 获取目标UI位置
            Vector3 worldPos = GetUnitUIPosition(target);
            if (worldPos == Vector3.zero) return;

            // 创建伤害数字
            GameObject damageGO = Instantiate(damageNumberPrefab, damageNumberParent);
            TextMeshProUGUI damageText = damageGO.GetComponent<TextMeshProUGUI>();
            
            if (damageText == null)
            {
                damageText = damageGO.AddComponent<TextMeshProUGUI>();
            }

            // 设置文本和颜色
            damageText.text = isHeal ? $"+{damage}" : $"-{damage}";
            damageText.color = isHeal ? healColor : (isCritical ? criticalColor : damageColor);
            damageText.fontSize = isCritical ? 32 : 24;
            damageText.alignment = TextAlignmentOptions.Center;
            damageText.fontStyle = isCritical ? FontStyles.Bold : FontStyles.Normal;

            // 设置位置
            RectTransform rect = damageGO.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = damageGO.AddComponent<RectTransform>();
            }
            rect.position = worldPos;

            // 启动动画
            StartCoroutine(AnimateDamageNumber(damageGO, rect, isCritical));
        }

        /// <summary>
        /// 播放攻击动画
        /// </summary>
        public void PlayAttackAnimation(CombatUnit attacker, CombatUnit target)
        {
            if (attacker == null) return;

            RectTransform attackerUI = GetUnitUITransform(attacker);
            if (attackerUI != null)
            {
                StartCoroutine(AttackAnimationCoroutine(attackerUI));
            }

            // 如果有目标，可以添加攻击方向指示
            if (target != null)
            {
                RectTransform targetUI = GetUnitUITransform(target);
                if (targetUI != null)
                {
                    // 可以添加攻击线或箭头指向目标
                }
            }
        }

        /// <summary>
        /// 播放受击动画
        /// </summary>
        public void PlayHitAnimation(CombatUnit target, bool isCritical = false)
        {
            if (target == null) return;

            RectTransform targetUI = GetUnitUITransform(target);
            Image targetImage = GetUnitUIImage(target);

            if (targetUI != null)
            {
                StartCoroutine(HitAnimationCoroutine(targetUI, targetImage, isCritical));
            }
        }

        /// <summary>
        /// 播放死亡动画
        /// </summary>
        public void PlayDeathAnimation(CombatUnit unit)
        {
            if (unit == null) return;

            RectTransform unitUI = GetUnitUITransform(unit);
            if (unitUI != null)
            {
                StartCoroutine(DeathAnimationCoroutine(unitUI));
            }
        }

        /// <summary>
        /// 播放技能特效（占位符）
        /// </summary>
        public void PlaySkillEffect(CombatUnit caster, string skillName, CombatUnit target = null)
        {
            // TODO: 实现技能特效
            Debug.Log($"[CombatVisualEffects] 播放技能特效: {skillName} (目标: {target?.UnitName ?? "无"})");
        }

        #region 动画协程

        private IEnumerator AnimateDamageNumber(GameObject damageGO, RectTransform rect, bool isCritical)
        {
            float elapsed = 0f;
            Vector3 startPos = rect.position;
            Vector3 randomOffset = new Vector3(
                Random.Range(-damageNumberSpread, damageNumberSpread),
                Random.Range(-damageNumberSpread * 0.5f, damageNumberSpread * 0.5f),
                0
            );

            // 初始缩放（暴击更大）
            float startScale = isCritical ? 1.5f : 1f;
            rect.localScale = Vector3.one * startScale;

            while (elapsed < damageNumberLifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / damageNumberLifetime;

                // 向上移动
                rect.position = startPos + randomOffset + Vector3.up * (damageNumberSpeed * elapsed * 100f);

                // 淡出
                TextMeshProUGUI text = damageGO.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    Color color = text.color;
                    color.a = 1f - t;
                    text.color = color;
                }

                // 缩放动画
                float scale = Mathf.Lerp(startScale, 0.5f, t);
                rect.localScale = Vector3.one * scale;

                yield return null;
            }

            Destroy(damageGO);
        }

        private IEnumerator AttackAnimationCoroutine(RectTransform attackerUI)
        {
            Vector3 originalScale = Vector3.one;
            Vector3 originalPos = attackerUI.localPosition;

            // 放大
            float elapsed = 0f;
            while (elapsed < attackScaleDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / attackScaleDuration;
                float scale = Mathf.Lerp(1f, attackScaleAmount, t);
                attackerUI.localScale = Vector3.one * scale;
                yield return null;
            }

            // 震动
            elapsed = 0f;
            while (elapsed < attackShakeDuration)
            {
                elapsed += Time.deltaTime;
                Vector3 shake = Random.insideUnitCircle * attackShakeIntensity;
                attackerUI.localPosition = originalPos + shake;
                yield return null;
            }

            // 恢复
            attackerUI.localScale = originalScale;
            attackerUI.localPosition = originalPos;
        }

        private IEnumerator HitAnimationCoroutine(RectTransform targetUI, Image targetImage, bool isCritical)
        {
            Vector3 originalPos = targetUI.localPosition;
            Color originalColor = targetImage != null ? targetImage.color : Color.white;
            float shakeIntensity = isCritical ? criticalShakeIntensity : hitShakeIntensity;

            // 闪烁
            if (targetImage != null)
            {
                float flashElapsed = 0f;
                while (flashElapsed < hitFlashDuration)
                {
                    flashElapsed += Time.deltaTime;
                    targetImage.color = Color.Lerp(hitFlashColor, originalColor, flashElapsed / hitFlashDuration);
                    yield return null;
                }
                targetImage.color = originalColor;
            }

            // 震动
            float shakeElapsed = 0f;
            while (shakeElapsed < hitShakeDuration)
            {
                shakeElapsed += Time.deltaTime;
                Vector3 shake = Random.insideUnitCircle * shakeIntensity;
                targetUI.localPosition = originalPos + shake;
                yield return null;
            }

            targetUI.localPosition = originalPos;
        }

        private IEnumerator DeathAnimationCoroutine(RectTransform unitUI)
        {
            Vector3 originalScale = unitUI.localScale;
            float elapsed = 0f;

            while (elapsed < deathFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / deathFadeDuration;

                // 缩放
                float scaleT = Mathf.Min(t / (deathScaleDuration / deathFadeDuration), 1f);
                unitUI.localScale = Vector3.Lerp(originalScale, Vector3.one * deathFinalScale, scaleT);

                // 旋转（可选）
                unitUI.localRotation = Quaternion.Euler(0, 0, t * 180f);

                yield return null;
            }

            unitUI.localScale = Vector3.one * deathFinalScale;
        }

        #endregion

        #region 工具方法

        private RectTransform GetUnitUITransform(CombatUnit unit)
        {
            return unitUITransforms.TryGetValue(unit, out var rect) ? rect : null;
        }

        private Image GetUnitUIImage(CombatUnit unit)
        {
            return unitUIImages.TryGetValue(unit, out var image) ? image : null;
        }

        private Vector3 GetUnitUIPosition(CombatUnit unit)
        {
            RectTransform rect = GetUnitUITransform(unit);
            if (rect != null)
            {
                return rect.position;
            }
            return Vector3.zero;
        }

        private void CreateDefaultDamageNumberPrefab()
        {
            GameObject prefab = new GameObject("DamageNumber");
            RectTransform rect = prefab.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 50);

            TextMeshProUGUI text = prefab.AddComponent<TextMeshProUGUI>();
            text.text = "0";
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = damageColor;

            damageNumberPrefab = prefab;
            prefab.SetActive(false);
        }

        #endregion
    }
}
