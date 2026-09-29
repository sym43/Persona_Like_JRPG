using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 분석 또는 현재 아니마 상태를 같은 정보 행 구조에 표시함.
/// </summary>
public sealed class BattleInfoView : MonoBehaviour
{
    private enum InfoMode
    {
        Enemy = 0,
        Anima = 1
    }

    [SerializeField] private BattleMenuUI battleMenu;
    [SerializeField] private InfoMode mode;

    private TMPro.TMP_Text nameText;
    private TMPro.TMP_Text summaryText;
    private TMPro.TMP_Text statsText;
    private UnityEngine.UI.Image[] damageImages =
        Array.Empty<UnityEngine.UI.Image>();
    private UnityEngine.UI.Image[] resistanceImages =
        Array.Empty<UnityEngine.UI.Image>();
    private GameObject[] skillRows = Array.Empty<GameObject>();
    private GameObject[] skillIconMasks = Array.Empty<GameObject>();
    private UnityEngine.UI.Image[] skillImages =
        Array.Empty<UnityEngine.UI.Image>();
    private TMPro.TMP_Text[] skillNameTexts = Array.Empty<TMPro.TMP_Text>();
    private Sprite[] damageIcons = Array.Empty<Sprite>();
    private Sprite[] resistanceIcons = Array.Empty<Sprite>();
    private readonly Dictionary<string, Sprite> skillTypeIcons = new();
    private Sprite unknownResistanceIcon;
    private GameObject leftRightPanel;
    private GameObject animaChangeHint;

    private void Awake()
    {
        if (battleMenu == null)
        {
            Debug.LogError("전투 정보 UI에 전투 메뉴가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        nameText = transform.Find("Header/Name")?.GetComponent<TMPro.TMP_Text>();
        summaryText = transform.Find("Header/Summary")?.GetComponent<TMPro.TMP_Text>();
        statsText = transform.Find("Stats")?.GetComponent<TMPro.TMP_Text>();
        Transform affinityList = transform.Find("AffinityList");
        Transform skillList = transform.Find("SkillList");
        if (mode == InfoMode.Anima)
        {
            leftRightPanel = transform.Find("Header/LeftRightPanel")?.gameObject;
            animaChangeHint = transform.Find("AnimaChangeHint")?.gameObject;
        }
        if (nameText == null || summaryText == null || statsText == null ||
            affinityList == null || skillList == null ||
            (mode == InfoMode.Anima &&
             (leftRightPanel == null || animaChangeHint == null)) ||
            affinityList.childCount != Enum.GetValues(typeof(DamageType)).Length)
            throw new InvalidOperationException("전투 정보 UI 구성이 올바르지 않습니다.");

        damageImages = new UnityEngine.UI.Image[affinityList.childCount];
        resistanceImages = new UnityEngine.UI.Image[affinityList.childCount];
        for (int index = 0; index < affinityList.childCount; index++)
        {
            Transform slot = affinityList.GetChild(index);
            damageImages[index] = slot.Find("DamageIcon")?
                .GetComponent<UnityEngine.UI.Image>();
            resistanceImages[index] = slot.Find("ResistanceIcon")?
                .GetComponent<UnityEngine.UI.Image>();
            if (damageImages[index] == null || resistanceImages[index] == null)
                throw new InvalidOperationException(
                    $"전투 정보 상성 {index + 1}번 슬롯 구성이 올바르지 않습니다.");
        }

        skillRows = new GameObject[skillList.childCount];
        skillIconMasks = new GameObject[skillList.childCount];
        skillImages = new UnityEngine.UI.Image[skillList.childCount];
        skillNameTexts = new TMPro.TMP_Text[skillList.childCount];
        for (int index = 0; index < skillList.childCount; index++)
        {
            Transform row = skillList.GetChild(index);
            Transform iconMask = row.Find("IconMask");
            skillRows[index] = row.gameObject;
            skillIconMasks[index] = iconMask?.gameObject;
            skillImages[index] = iconMask?.Find("Icon")?
                .GetComponent<UnityEngine.UI.Image>();
            skillNameTexts[index] = row.Find("Name")?
                .GetComponent<TMPro.TMP_Text>();
            if (skillIconMasks[index] == null || skillImages[index] == null ||
                skillNameTexts[index] == null)
                throw new InvalidOperationException(
                    $"전투 정보 기술 {index + 1}번 행 구성이 올바르지 않습니다.");
        }

        LoadIcons();
    }

    private void OnEnable()
    {
        if (battleMenu == null) return;

        if (mode == InfoMode.Enemy)
        {
            battleMenu.AnalyzeTargetChanged += Show;
            Show(battleMenu.SelectedAnalyzeUnit);
        }
        else
        {
            battleMenu.PersonaStatusChanged += ShowPersona;
            ShowPersona();
        }
    }

    private void OnDisable()
    {
        if (battleMenu == null) return;
        battleMenu.AnalyzeTargetChanged -= Show;
        battleMenu.PersonaStatusChanged -= ShowPersona;
    }

    private void Show(BattleUnit unit)
    {
        if (unit == null) return;

        bool enemy = mode == InfoMode.Enemy;
        if (!enemy)
        {
            leftRightPanel.SetActive(false);
            animaChangeHint.SetActive(false);
        }
        nameText.text = enemy ? unit.Data.DisplayName : unit.AnimaDisplayName;
        summaryText.text = enemy
            ? $"LV {unit.Level}    MAX HP {unit.MaxHp}    SP {unit.MaxSp}"
            : $"LV {unit.Level}";
        statsText.gameObject.SetActive(!enemy);
        if (!enemy)
        {
            BattleStats stats = unit.Stats;
            statsText.text = $"힘 {stats.Strength:00}   마 {stats.Magic:00}   " +
                             $"내 {stats.Endurance:00}   속 {stats.Agility:00}   " +
                             $"운 {stats.Luck:00}";
        }

        foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
        {
            int index = (int)damageType;
            SetSprite(damageImages[index], damageIcons[index]);
            ResistanceType? resistance = enemy
                ? battleMenu.GetTargetResistanceForInfo(unit, damageType)
                : unit.GetResistance(damageType);
            Sprite resistanceIcon = resistance.HasValue
                ? resistanceIcons[(int)resistance.Value]
                : unknownResistanceIcon;
            SetSprite(resistanceImages[index], resistanceIcon);
        }

        ShowSkills(battleMenu.GetUnitSkills(unit));
    }

    private void ShowPersona()
    {
        Anima anima = battleMenu.SelectedAnimaInfo;
        if (anima == null)
        {
            Show(battleMenu.CurrentUnit);
            return;
        }

        leftRightPanel.SetActive(true);
        animaChangeHint.SetActive(true);
        nameText.text = anima.Data.DisplayName;
        summaryText.text = $"LV {anima.Level}";
        statsText.gameObject.SetActive(true);
        BattleStats stats = anima.Stats;
        statsText.text = $"힘 {stats.Strength:00}   마 {stats.Magic:00}   " +
                         $"내 {stats.Endurance:00}   속 {stats.Agility:00}   " +
                         $"운 {stats.Luck:00}";

        foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
        {
            int index = (int)damageType;
            SetSprite(damageImages[index], damageIcons[index]);
            SetSprite(resistanceImages[index],
                resistanceIcons[(int)anima.Data.Resistances.Get(damageType)]);
        }

        ShowSkills(battleMenu.GetAnimaSkills(anima));
    }

    private void ShowSkills(IReadOnlyList<SkillData> skills)
    {
        for (int index = 0; index < skillRows.Length; index++)
        {
            bool used = index < skills.Count;
            skillRows[index].SetActive(used);
            if (!used) continue;

            SkillData skill = skills[index];
            skillNameTexts[index].text = skill.DisplayName;
            DamageType? damageType = battleMenu.GetSkillDamageType(skill);
            Sprite icon = damageType.HasValue
                ? damageIcons[(int)damageType.Value]
                : GetSkillTypeIcon(skill);
            skillIconMasks[index].SetActive(icon != null);
            if (icon != null)
                SetSprite(skillImages[index], icon);
        }
    }

    private void LoadIcons()
    {
        Array damageTypes = Enum.GetValues(typeof(DamageType));
        damageIcons = new Sprite[damageTypes.Length];
        foreach (DamageType damageType in damageTypes)
        {
            string category = (int)damageType <= (int)DamageType.Pierce
                ? "Physical"
                : "Emotion";
            damageIcons[(int)damageType] = LoadIcon(
                $"UI/DamageTypeIcon/{category}_{damageType}");
        }

        Array resistances = Enum.GetValues(typeof(ResistanceType));
        resistanceIcons = new Sprite[resistances.Length];
        foreach (ResistanceType resistance in resistances)
            resistanceIcons[(int)resistance] = LoadIcon(
                $"UI/ResistanceTypeIcon/ResistanceType_{resistance}");
        unknownResistanceIcon = LoadIcon(
            "UI/ResistanceTypeIcon/ResistanceType_Unknown");

        string[] iconNames =
        {
            "SkillType_Heal",
            "SkillType_Buff",
            "SkillType_Debuff",
            "SkillType_Ailment",
            "SkillType_Passive"
        };
        foreach (string iconName in iconNames)
            skillTypeIcons.Add(iconName,
                LoadIcon($"UI/SkillTypeIcon/{iconName}"));
    }

    private Sprite GetSkillTypeIcon(SkillData skill)
    {
        string iconName = battleMenu.GetSkillTypeIconName(skill);
        return iconName != null && skillTypeIcons.TryGetValue(iconName,
            out Sprite icon) ? icon : null;
    }

    private static Sprite LoadIcon(string path)
    {
        Sprite icon = Resources.Load<Sprite>(path);
        if (icon == null)
            throw new InvalidOperationException(
                $"전투 정보 아이콘을 찾을 수 없습니다: Resources/{path}");
        return icon;
    }

    private static void SetSprite(UnityEngine.UI.Image image, Sprite sprite)
    {
        if (image.sprite != sprite)
            image.sprite = sprite;

        Vector2 pivot = new(sprite.pivot.x / sprite.rect.width,
            sprite.pivot.y / sprite.rect.height);
        if (image.rectTransform.pivot != pivot)
            image.rectTransform.pivot = pivot;
    }
}
