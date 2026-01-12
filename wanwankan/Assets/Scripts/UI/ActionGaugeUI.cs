using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using WanWanKan.Combat;

namespace WanWanKan.UI
{
    /// <summary>
    /// 单个单位的气力条UI - 支持HP显示、动画效果、点击选择
    /// </summary>
    public class ActionGaugeUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI组件 - 基础")]
        [SerializeField] private Image gaugeBackground;
        [SerializeField] private Image gaugeFill;
        [SerializeField] private Image unitIcon;
        [SerializeField] private TextMeshProUGUI unitNameText;
        [SerializeField] private TextMeshProUGUI gaugeValueText;
        [SerializeField] private Image readyIndicator;

        [Header("UI组件 - 血量")]
        [SerializeField] private Image hpBarBackground;
        [SerializeField] private Image hpBarFill;           // 当前血量（绿色）
        [SerializeField] private Image hpBarDamage;         // 伤害延迟条（红色/白色）
        [SerializeField] private TextMeshProUGUI hpValueText;

        [Header("颜色设置")]
        [SerializeField] private Color playerColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color enemyColor = new Color(1f, 0.3f, 0.3f);
        [SerializeField] private Color allyColor = new Color(0.3f, 1f, 0.3f);
        [SerializeField] private Color readyColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField] private Color selectableColor = new Color(1f, 1f, 0.5f, 0.3f);
        [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.5f);
        
        [Header("血量条颜色")]
        [SerializeField] private Color hpFullColor = new Color(0.2f, 0.8f, 0.2f);      // 满血绿色
        [SerializeField] private Color hpMidColor = new Color(0.9f, 0.8f, 0.1f);       // 中等黄色
        [SerializeField] private Color hpLowColor = new Color(0.9f, 0.2f, 0.2f);       // 低血红色
        [SerializeField] private Color hpDamageColor = new Color(1f, 1f, 1f, 0.8f);    // 伤害延迟条颜色
        
        [Header("动画设置")]
        [SerializeField] private float fillSmoothSpeed = 5f;
        [SerializeField] private float readyPulseSpeed = 2f;
        [SerializeField] private float hpAnimSpeed = 3f;           // 血条动画速度
        [SerializeField] private float hpDamageDelay = 0.3f;       // 伤害条延迟开始时间
        [SerializeField] private float hpDamageSpeed = 2f;         // 伤害条追赶速度
        [SerializeField] private float hpNumberSpeed = 10f;        // 数字变化速度
        [SerializeField] private float deathFadeDuration = 1f;     // 死亡渐隐时间

        private CombatUnit boundUnit;
        private float targetFillAmount;
        private float currentFillAmount;
        private bool isReady;
        private bool isSelectable = false;
        private bool isHovered = false;
        private Image highlightOverlay;
        
        // HP动画相关
        private float targetHPFill;
        private float currentHPFill;
        private float damageHPFill;
        private float displayedHP;
        private float targetHP;
        private bool isDamageAnimating = false;
        private bool isDying = false;
        private CanvasGroup canvasGroup;

        /// <summary>
        /// 绑定的战斗单位
        /// </summary>
        public CombatUnit BoundUnit => boundUnit;

        /// <summary>
        /// 点击事件
        /// </summary>
        public event Action<CombatUnit> OnUnitClicked;
        
        /// <summary>
        /// 死亡动画完成事件
        /// </summary>
        public event Action<ActionGaugeUI> OnDeathAnimationComplete;

        /// <summary>
        /// 绑定战斗单位
        /// </summary>
        public void BindUnit(CombatUnit unit)
        {
            // 解绑旧单位
            if (boundUnit != null)
            {
                boundUnit.OnActionGaugeChanged -= OnGaugeChanged;
                boundUnit.Stats.OnHPChanged -= OnHPChanged;
                boundUnit.Stats.OnDeath -= OnUnitDeath;
            }

            boundUnit = unit;
            
            if (unit != null)
            {
                // 设置名称
                if (unitNameText != null)
                    unitNameText.text = unit.UnitName;
                
                // 设置图标
                if (unitIcon != null && unit.UnitIcon != null)
                    unitIcon.sprite = unit.UnitIcon;

                // 设置气力条颜色
                Color unitColor = GetUnitColor(unit.UnitType);
                if (gaugeFill != null)
                    gaugeFill.color = unitColor;

                // 订阅事件
                unit.OnActionGaugeChanged += OnGaugeChanged;
                unit.Stats.OnHPChanged += OnHPChanged;
                unit.Stats.OnDeath += OnUnitDeath;
                
                // 初始化显示
                UpdateGaugeDisplay(unit.CurrentActionGauge, unit.MaxActionGauge);
                InitializeHPDisplay(unit.Stats.CurrentHP, unit.Stats.MaxHP);
                
                // 确保CanvasGroup存在
                EnsureCanvasGroup();
            }

            gameObject.SetActive(unit != null);
        }

        /// <summary>
        /// 解绑单位
        /// </summary>
        public void Unbind()
        {
            BindUnit(null);
        }
        
        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void Update()
        {
            if (isDying) return;
            
            // 气力条平滑更新
            if (gaugeFill != null && Mathf.Abs(currentFillAmount - targetFillAmount) > 0.001f)
            {
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * fillSmoothSpeed);
                gaugeFill.fillAmount = currentFillAmount;
            }

            // 就绪状态脉冲动画
            if (readyIndicator != null && isReady)
            {
                float pulse = (Mathf.Sin(Time.time * readyPulseSpeed * Mathf.PI) + 1f) / 2f;
                Color color = readyIndicator.color;
                color.a = 0.5f + pulse * 0.5f;
                readyIndicator.color = color;
            }
            
            // HP条动画更新
            UpdateHPAnimation();
        }

        #region 气力条显示

        private void OnGaugeChanged(float current, float max)
        {
            UpdateGaugeDisplay(current, max);
        }

        private void UpdateGaugeDisplay(float current, float max)
        {
            targetFillAmount = max > 0 ? current / max : 0;
            isReady = current >= max;

            if (gaugeValueText != null)
            {
                gaugeValueText.text = $"{current:F0}/{max:F0}";
            }

            if (readyIndicator != null)
            {
                readyIndicator.gameObject.SetActive(isReady);
                if (isReady)
                {
                    readyIndicator.color = readyColor;
                }
            }

            if (gaugeFill != null && boundUnit != null)
            {
                gaugeFill.color = isReady ? readyColor : GetUnitColor(boundUnit.UnitType);
            }
        }

        #endregion

        #region HP显示与动画

        /// <summary>
        /// 初始化HP显示
        /// </summary>
        private void InitializeHPDisplay(int currentHP, int maxHP)
        {
            float hpPercent = maxHP > 0 ? (float)currentHP / maxHP : 0;
            
            targetHPFill = hpPercent;
            currentHPFill = hpPercent;
            damageHPFill = hpPercent;
            displayedHP = currentHP;
            targetHP = currentHP;
            
            if (hpBarFill != null)
            {
                hpBarFill.fillAmount = hpPercent;
                hpBarFill.color = GetHPColor(hpPercent);
            }
            
            if (hpBarDamage != null)
            {
                hpBarDamage.fillAmount = hpPercent;
                hpBarDamage.color = hpDamageColor;
            }
            
            UpdateHPText();
        }

        /// <summary>
        /// HP变化回调
        /// </summary>
        private void OnHPChanged(int currentHP, int maxHP)
        {
            float newHPPercent = maxHP > 0 ? (float)currentHP / maxHP : 0;
            
            // 如果是受到伤害（HP减少）
            if (newHPPercent < targetHPFill)
            {
                // 启动伤害动画
                StartCoroutine(DamageAnimation(newHPPercent, currentHP));
            }
            else
            {
                // 治疗，直接更新
                targetHPFill = newHPPercent;
                damageHPFill = newHPPercent;
                targetHP = currentHP;
            }
        }

        /// <summary>
        /// 伤害动画协程
        /// </summary>
        private IEnumerator DamageAnimation(float newHPPercent, int newHP)
        {
            // 立即更新绿色血条目标
            targetHPFill = newHPPercent;
            targetHP = newHP;
            
            // 等待一小段时间后，伤害条开始追赶
            yield return new WaitForSeconds(hpDamageDelay);
            
            isDamageAnimating = true;
        }

        /// <summary>
        /// 更新HP动画
        /// </summary>
        private void UpdateHPAnimation()
        {
            if (boundUnit == null) return;
            
            // 绿色血条平滑过渡
            if (Mathf.Abs(currentHPFill - targetHPFill) > 0.001f)
            {
                currentHPFill = Mathf.Lerp(currentHPFill, targetHPFill, Time.deltaTime * hpAnimSpeed);
                
                if (hpBarFill != null)
                {
                    hpBarFill.fillAmount = currentHPFill;
                    hpBarFill.color = GetHPColor(currentHPFill);
                }
            }
            
            // 伤害条追赶（白色条）
            if (isDamageAnimating && Mathf.Abs(damageHPFill - targetHPFill) > 0.001f)
            {
                damageHPFill = Mathf.Lerp(damageHPFill, targetHPFill, Time.deltaTime * hpDamageSpeed);
                
                if (hpBarDamage != null)
                {
                    hpBarDamage.fillAmount = damageHPFill;
                }
            }
            else if (isDamageAnimating)
            {
                isDamageAnimating = false;
                damageHPFill = targetHPFill;
            }
            
            // HP数字动画
            if (Mathf.Abs(displayedHP - targetHP) > 0.5f)
            {
                displayedHP = Mathf.Lerp(displayedHP, targetHP, Time.deltaTime * hpNumberSpeed);
                UpdateHPText();
            }
            else if (displayedHP != targetHP)
            {
                displayedHP = targetHP;
                UpdateHPText();
            }
        }

        /// <summary>
        /// 更新HP文本
        /// </summary>
        private void UpdateHPText()
        {
            if (hpValueText != null && boundUnit != null)
            {
                hpValueText.text = $"{Mathf.RoundToInt(displayedHP)}/{boundUnit.Stats.MaxHP}";
            }
        }

        /// <summary>
        /// 根据HP百分比获取颜色
        /// </summary>
        private Color GetHPColor(float hpPercent)
        {
            if (hpPercent > 0.5f)
            {
                // 50%-100%: 绿色到黄色
                float t = (hpPercent - 0.5f) * 2f;
                return Color.Lerp(hpMidColor, hpFullColor, t);
            }
            else
            {
                // 0%-50%: 红色到黄色
                float t = hpPercent * 2f;
                return Color.Lerp(hpLowColor, hpMidColor, t);
            }
        }

        #endregion

        #region 死亡动画

        /// <summary>
        /// 单位死亡回调
        /// </summary>
        private void OnUnitDeath()
        {
            Debug.Log($"[ActionGaugeUI] {boundUnit?.UnitName} 死亡，开始渐隐动画");
            StartCoroutine(DeathFadeAnimation());
        }

        /// <summary>
        /// 死亡渐隐动画
        /// </summary>
        private IEnumerator DeathFadeAnimation()
        {
            isDying = true;
            EnsureCanvasGroup();
            
            // 先完成HP动画
            if (hpBarFill != null) hpBarFill.fillAmount = 0;
            if (hpBarDamage != null) hpBarDamage.fillAmount = 0;
            if (hpValueText != null) hpValueText.text = "0/" + boundUnit?.Stats.MaxHP;
            
            yield return new WaitForSeconds(0.5f);
            
            // 渐隐动画
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            
            while (elapsed < deathFadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / deathFadeDuration;
                
                // 使用 EaseOut 曲线
                t = 1f - Mathf.Pow(1f - t, 2f);
                
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                
                // 可选：添加缩放效果
                transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.8f, t);
                
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            
            // 触发完成事件
            OnDeathAnimationComplete?.Invoke(this);
            
            // 销毁物体
            Destroy(gameObject);
        }

        #endregion

        #region 点击选择功能

        private Color GetUnitColor(UnitType type)
        {
            return type switch
            {
                UnitType.Player => playerColor,
                UnitType.Enemy => enemyColor,
                UnitType.Ally => allyColor,
                _ => Color.white
            };
        }

        public void SetSelectable(bool selectable)
        {
            isSelectable = selectable;
            UpdateHighlight();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isSelectable && boundUnit != null && boundUnit.IsAlive)
            {
                Debug.Log($"[ActionGaugeUI] 点击选择目标: {boundUnit.UnitName}");
                OnUnitClicked?.Invoke(boundUnit);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            if (isSelectable)
            {
                UpdateHighlight();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            UpdateHighlight();
        }

        private void UpdateHighlight()
        {
            if (highlightOverlay == null)
            {
                CreateHighlightOverlay();
            }

            if (highlightOverlay != null)
            {
                if (isSelectable)
                {
                    highlightOverlay.gameObject.SetActive(true);
                    highlightOverlay.color = isHovered ? hoverColor : selectableColor;
                }
                else
                {
                    highlightOverlay.gameObject.SetActive(false);
                }
            }
        }

        private void CreateHighlightOverlay()
        {
            GameObject overlayObj = new GameObject("HighlightOverlay");
            overlayObj.transform.SetParent(transform, false);
            
            RectTransform rect = overlayObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            highlightOverlay = overlayObj.AddComponent<Image>();
            highlightOverlay.color = selectableColor;
            highlightOverlay.raycastTarget = false;
            highlightOverlay.gameObject.SetActive(false);
            
            overlayObj.transform.SetAsLastSibling();
        }

        #endregion

        private void OnDestroy()
        {
            if (boundUnit != null)
            {
                boundUnit.OnActionGaugeChanged -= OnGaugeChanged;
                boundUnit.Stats.OnHPChanged -= OnHPChanged;
                boundUnit.Stats.OnDeath -= OnUnitDeath;
            }
        }
    }
}
