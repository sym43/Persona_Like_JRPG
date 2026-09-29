using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사용 가능한 스킬을 미리 만든 목록 행에 표시함.
/// </summary>
public sealed class BattleSkillListView : MonoBehaviour
{
    //현재 스킬 목록과 선택 스킬을 알려주는 전투 메뉴
    [SerializeField] private BattleMenuUI battleMenu;
    //스킬 행들이 들어 있는 세로 목록
    [SerializeField] private RectTransform skillList;
    //현재 행동자의 아니마 이름
    [SerializeField] private TMP_Text animaNameText;
    //현재 선택한 스킬 설명
    [SerializeField] private TMP_Text descriptionText;
    //현재 선택한 행 색상
    [SerializeField] private Color selectedColor = new(0.1f, 0.45f, 0.9f, 0.9f);
    //선택하지 않은 행 색상
    [SerializeField] private Color normalColor = new(0f, 0f, 0f, 0.55f);
    //SP 비용 표시 색상
    [SerializeField] private Color spCostColor = Color.yellow;

    private GameObject[] rows = Array.Empty<GameObject>();
    private TMP_Text[] nameTexts = Array.Empty<TMP_Text>();
    private TMP_Text[] costTexts = Array.Empty<TMP_Text>();
    private TMP_Text[] affinityTexts = Array.Empty<TMP_Text>();
    private GameObject[] iconMasks = Array.Empty<GameObject>();
    private Image[] iconImages = Array.Empty<Image>();
    private Color[] defaultCostColors = Array.Empty<Color>();
    private Image[] backgrounds = Array.Empty<Image>();
    private Sprite[] damageIcons = Array.Empty<Sprite>();
    private readonly Dictionary<string, Sprite> skillTypeIcons =
        new Dictionary<string, Sprite>();

    //미리 만든 스킬 행을 찾음
    private void Awake()
    {
        if (battleMenu == null || skillList == null ||
            animaNameText == null || descriptionText == null)
        {
            Debug.LogError("스킬 목록 UI 연결이 빠졌습니다.");
            enabled = false;
            return;
        }

        int rowCount = skillList.childCount;
        rows = new GameObject[rowCount];
        nameTexts = new TMP_Text[rowCount];
        costTexts = new TMP_Text[rowCount];
        affinityTexts = new TMP_Text[rowCount];
        iconMasks = new GameObject[rowCount];
        iconImages = new Image[rowCount];
        defaultCostColors = new Color[rowCount];
        backgrounds = new Image[rowCount];
        for (int index = 0; index < rowCount; index++)
        {
            Transform row = skillList.GetChild(index);
            rows[index] = row.gameObject;
            backgrounds[index] = row.GetComponent<Image>();
            nameTexts[index] = row.Find("Name")?.GetComponent<TMP_Text>();
            costTexts[index] = row.Find("Cost")?.GetComponent<TMP_Text>();
            affinityTexts[index] = row.Find("Affinity")?.GetComponent<TMP_Text>();
            Transform iconMask = row.Find("IconMask");
            iconMasks[index] = iconMask?.gameObject;
            iconImages[index] = iconMask?.Find("Icon")?.GetComponent<Image>();
            if (backgrounds[index] == null || nameTexts[index] == null ||
                costTexts[index] == null || affinityTexts[index] == null ||
                iconMasks[index] == null || iconImages[index] == null)
                throw new InvalidOperationException(
                    $"스킬 목록 {index + 1}번 행 구성이 올바르지 않습니다.");
            defaultCostColors[index] = costTexts[index].color;
        }

        LoadDamageIcons();
        LoadSkillTypeIcons();
    }

    //스킬 선택 변경을 받기 시작함
    private void OnEnable()
    {
        if (battleMenu != null)
            battleMenu.SkillSelectionChanged += ShowSkills;
    }

    //스킬 선택 변경 연결을 끊음
    private void OnDisable()
    {
        if (battleMenu != null)
            battleMenu.SkillSelectionChanged -= ShowSkills;
    }

    //보유 스킬 수만큼 행을 켜고 현재 선택을 표시함
    private void ShowSkills(SkillData selectedSkill)
    {
        animaNameText.text = battleMenu.CurrentUnit?.AnimaDisplayName
            ?? string.Empty;
        descriptionText.text = selectedSkill?.Description ?? string.Empty;
        int skillCount = battleMenu.Skills.Count;
        for (int index = 0; index < rows.Length; index++)
        {
            bool used = index < skillCount;
            rows[index].SetActive(used);
            if (!used) continue;

            SkillData skill = battleMenu.Skills[index];
            nameTexts[index].text = skill.DisplayName;
            costTexts[index].text = GetCostText(skill);
            affinityTexts[index].text = GetAffinityText(
                battleMenu.GetSkillResistance(skill));
            DamageType? damageType = battleMenu.GetSkillDamageType(skill);
            Sprite icon = damageType.HasValue
                ? damageIcons[(int)damageType.Value]
                : GetSkillTypeIcon(skill);
            iconMasks[index].SetActive(icon != null);
            if (icon != null)
                SetSprite(iconImages[index], icon);
            costTexts[index].color = skill.CostType == SkillCostType.Sp
                ? spCostColor
                : defaultCostColors[index];
            backgrounds[index].color = skill == selectedSkill
                ? selectedColor
                : normalColor;
        }
    }

    //Resources의 속성 아이콘을 한 번만 읽음
    private void LoadDamageIcons()
    {
        Array damageTypes = Enum.GetValues(typeof(DamageType));
        damageIcons = new Sprite[damageTypes.Length];
        foreach (DamageType damageType in damageTypes)
        {
            string category = (int)damageType <= (int)DamageType.Pierce
                ? "Physical"
                : "Emotion";
            string path = $"UI/DamageTypeIcon/{category}_{damageType}";
            Sprite icon = Resources.Load<Sprite>(path);
            if (icon == null)
                throw new InvalidOperationException(
                    $"스킬 속성 아이콘을 찾을 수 없습니다: Resources/{path}");
            damageIcons[(int)damageType] = icon;
        }
    }

    //Resources의 회복·지원·상태이상 아이콘을 한 번만 읽음
    private void LoadSkillTypeIcons()
    {
        string[] iconNames =
        {
            "SkillType_Heal",
            "SkillType_Buff",
            "SkillType_Debuff",
            "SkillType_Ailment",
            "SkillType_Passive"
        };
        foreach (string iconName in iconNames)
        {
            string path = $"UI/SkillTypeIcon/{iconName}";
            Sprite icon = Resources.Load<Sprite>(path);
            if (icon == null)
                throw new InvalidOperationException(
                    $"스킬 종류 아이콘을 찾을 수 없습니다: Resources/{path}");
            skillTypeIcons.Add(iconName, icon);
        }
    }

    private Sprite GetSkillTypeIcon(SkillData skill)
    {
        string iconName = battleMenu.GetSkillTypeIconName(skill);
        return iconName != null && skillTypeIcons.TryGetValue(iconName, out Sprite icon)
            ? icon
            : null;
    }

    //스프라이트에서 설정한 피벗을 UI 아이콘에도 적용함
    private static void SetSprite(Image image, Sprite sprite)
    {
        if (image.sprite != sprite)
            image.sprite = sprite;

        Vector2 pivot = new(sprite.pivot.x / sprite.rect.width,
            sprite.pivot.y / sprite.rect.height);
        if (image.rectTransform.pivot != pivot)
            image.rectTransform.pivot = pivot;
    }

    //스킬 비용을 실제 소모되는 HP 또는 SP 값으로 표시함
    private string GetCostText(SkillData skill)
    {
        return skill.CostType switch
        {
            SkillCostType.Hp =>
                $"{BattleAttackCalculator.CalculateHpCost(battleMenu.CurrentUnit, skill.Cost)} HP",
            SkillCostType.Sp => $"{skill.Cost} SP",
            _ => string.Empty
        };
    }

    //공개된 상성을 원작식 문구로 바꿈
    private static string GetAffinityText(ResistanceType? resistance)
    {
        return resistance switch
        {
            ResistanceType.Weak => "WEAK",
            ResistanceType.Resist => "RESIST",
            ResistanceType.Immune => "BLOCK",
            ResistanceType.Reflect => "REPEL",
            ResistanceType.Drain => "DRAIN",
            _ => string.Empty
        };
    }
}
