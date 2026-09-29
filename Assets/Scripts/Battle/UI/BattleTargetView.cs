using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 선택한 전투원 위에 행동 종류, 공개된 상성과 현재 HP를 표시함.
/// </summary>
public sealed class BattleTargetView : MonoBehaviour
{
    [Serializable]
    private struct DamageTypeColor
    {
        [SerializeField] private DamageType type;
        [SerializeField] private Color color;

        public DamageType Type => type;
        public Color Color => color;

        public DamageTypeColor(DamageType type, Color color)
        {
            this.type = type;
            this.color = color;
        }
    }

    //현재 전투 메뉴 입력 단계와 선택 행동
    [SerializeField] private BattleMenuUI battleMenu;
    //현재 선택된 전투원을 알려주는 대상 선택기
    [SerializeField] private BattleTargetSelector targetSelector;
    //대상 위로 이동할 UI 패널들
    [SerializeField] private RectTransform[] targetPanels;
    //속성 배경에 적용할 색. 배경 이미지의 기존 투명도는 유지함.
    [SerializeField] private DamageTypeColor[] damageTypeColors =
    {
        new(DamageType.Slash, new Color(0.93f, 0.18f, 0.2f)),
        new(DamageType.Strike, new Color(0.95f, 0.45f, 0.15f)),
        new(DamageType.Pierce, new Color(0.25f, 0.75f, 0.85f)),
        new(DamageType.Joy, new Color(0.98f, 0.75f, 0.1f)),
        new(DamageType.Anger, new Color(0.9f, 0.15f, 0.1f)),
        new(DamageType.Despair, new Color(0.35f, 0.2f, 0.7f)),
        new(DamageType.Fear, new Color(0.12f, 0.7f, 0.36f)),
        new(DamageType.Love, new Color(0.95f, 0.35f, 0.65f)),
        new(DamageType.Hatred, new Color(0.2f, 0.25f, 0.65f)),
        new(DamageType.Desire, new Color(0.1f, 0.7f, 0.65f))
    };
    //피해 속성이 없는 스킬 종류별 배경색. 기존 배경 투명도는 유지함.
    [SerializeField] private Color healColor = new(0.12f, 0.9f, 0.42f);
    [SerializeField] private Color buffColor = new(0.08f, 0.78f, 0.95f);
    [SerializeField] private Color debuffColor = new(1f, 0.28f, 0.08f);
    [SerializeField] private Color ailmentColor = new(0.62f, 0.08f, 0.92f);
    //전투원 기준점에서 화면상으로 띄울 거리
    [SerializeField] private Vector2 screenOffset = new(0f, 25f);

    private IReadOnlyList<BattleUnitActor> selectedActors =
        Array.Empty<BattleUnitActor>();
    private GameObject[] damageDisplays = Array.Empty<GameObject>();
    private UnityEngine.UI.Image[] damageBackgrounds =
        Array.Empty<UnityEngine.UI.Image>();
    private UnityEngine.UI.Image[] damageImages =
        Array.Empty<UnityEngine.UI.Image>();
    private UnityEngine.UI.Image[] resistanceImages =
        Array.Empty<UnityEngine.UI.Image>();
    private UnityEngine.UI.Slider[] healthBars =
        Array.Empty<UnityEngine.UI.Slider>();
    private Sprite[] damageIcons = Array.Empty<Sprite>();
    private Sprite[] resistanceIcons = Array.Empty<Sprite>();
    private readonly Dictionary<string, Sprite> skillTypeIcons = new();
    private Sprite unknownResistanceIcon;
    private Camera battleCamera;

    private void Awake()
    {
        if (battleMenu == null || targetSelector == null || !HasValidSlots())
        {
            Debug.LogError("대상 표시 UI 연결이 빠졌습니다.");
            enabled = false;
            return;
        }

        int slotCount = targetPanels.Length;
        damageDisplays = new GameObject[slotCount];
        damageBackgrounds = new UnityEngine.UI.Image[slotCount];
        damageImages = new UnityEngine.UI.Image[slotCount];
        resistanceImages = new UnityEngine.UI.Image[slotCount];
        healthBars = new UnityEngine.UI.Slider[slotCount];

        for (int index = 0; index < slotCount; index++)
        {
            Transform damageDisplay = targetPanels[index].Find("DamageDisplay");
            Transform background = damageDisplay?.Find("DamageBackground");
            Transform mask = damageDisplay?.Find("DamageMask");
            damageDisplays[index] = damageDisplay?.gameObject;
            damageBackgrounds[index] = background?.GetComponent<UnityEngine.UI.Image>();
            damageImages[index] = mask?.Find("DamageIcon")?
                .GetComponent<UnityEngine.UI.Image>();
            resistanceImages[index] = damageDisplay?.Find("ResistanceIcon")?
                .GetComponent<UnityEngine.UI.Image>();
            healthBars[index] = targetPanels[index].Find("HealthBar")?
                .GetComponent<UnityEngine.UI.Slider>();

            if (damageDisplays[index] == null || damageBackgrounds[index] == null ||
                damageImages[index] == null || resistanceImages[index] == null ||
                healthBars[index] == null)
                throw new InvalidOperationException(
                    $"대상 표시 {index + 1}번 슬롯 구성이 올바르지 않습니다.");
        }

        LoadIcons();
    }

    //대상 변경 알림을 받기 시작함
    private void OnEnable()
    {
        if (battleMenu == null || targetSelector == null || !HasValidSlots())
            return;

        battleCamera = Camera.main;
        targetSelector.SelectionChanged += ShowTargets;
        ShowTargets(targetSelector.SelectedActors);
    }

    //대상 변경 알림 연결을 끊음
    private void OnDisable()
    {
        if (targetSelector != null)
            targetSelector.SelectionChanged -= ShowTargets;
    }

    //선택한 전투원들의 위치와 정보를 계속 갱신함
    private void LateUpdate()
    {
        bool canShow = selectedActors.Count > 0 &&
                       (battleMenu.InputMode == BattleInputMode.Action ||
                        battleMenu.InputMode == BattleInputMode.Target);
        if (!canShow)
        {
            HideAll();
            return;
        }

        if (battleCamera == null)
            battleCamera = Camera.main;
        if (battleCamera == null)
        {
            HideAll();
            return;
        }

        for (int index = 0; index < targetPanels.Length; index++)
        {
            if (index >= selectedActors.Count || selectedActors[index] == null ||
                selectedActors[index].Unit == null)
            {
                targetPanels[index].gameObject.SetActive(false);
                continue;
            }

            ShowTarget(index, selectedActors[index]);
        }
    }

    //선택된 단일 또는 광역 대상을 저장함
    private void ShowTargets(IReadOnlyList<BattleUnitActor> actors)
    {
        selectedActors = actors == null
            ? Array.Empty<BattleUnitActor>()
            : new List<BattleUnitActor>(actors).AsReadOnly();
        HideAll();
    }

    //대상 하나의 패널을 해당 전투원 위에 표시함
    private void ShowTarget(int index, BattleUnitActor actor)
    {
        Vector3 screenPosition = battleCamera.WorldToScreenPoint(actor.TargetPosition);
        targetPanels[index].gameObject.SetActive(screenPosition.z > 0f);
        if (screenPosition.z <= 0f)
            return;

        targetPanels[index].position = (Vector2)screenPosition + screenOffset;
        healthBars[index].value = actor.Unit.MaxHp > 0
            ? (float)actor.Unit.Hp / actor.Unit.MaxHp
            : 0f;

        DamageType? damageType = battleMenu.GetTargetDamageType();
        string skillTypeIconName = damageType.HasValue
            ? null
            : battleMenu.GetTargetSkillTypeIconName();
        Sprite actionIcon = damageType.HasValue
            ? damageIcons[(int)damageType.Value]
            : GetSkillTypeIcon(skillTypeIconName);
        damageDisplays[index].SetActive(actionIcon != null);
        resistanceImages[index].gameObject.SetActive(false);
        if (actionIcon == null)
            return;

        SetSprite(damageImages[index], actionIcon);

        Color backgroundColor;
        if (damageType.HasValue)
        {
            backgroundColor = GetDamageTypeColor(damageType.Value);
            Sprite resistanceIcon = actor.Unit.IsEnemy
                ? GetResistanceIcon(battleMenu.GetTargetResistance(actor.Unit))
                : null;
            resistanceImages[index].gameObject.SetActive(resistanceIcon != null);
            if (resistanceIcon != null)
                SetSprite(resistanceImages[index], resistanceIcon);
        }
        else
        {
            backgroundColor = GetSkillTypeColor(skillTypeIconName);
        }

        backgroundColor.a = damageBackgrounds[index].color.a;
        damageBackgrounds[index].color = backgroundColor;
    }

    //Resources의 속성·상성 아이콘을 한 번만 읽음
    private void LoadIcons()
    {
        Array damageTypes = Enum.GetValues(typeof(DamageType));
        damageIcons = new Sprite[damageTypes.Length];
        foreach (DamageType damageType in damageTypes)
        {
            string category = (int)damageType <= (int)DamageType.Pierce
                ? "Physical"
                : "Emotion";
            string path = $"UI/DamageTypeIcon/{category}_{damageType}";
            damageIcons[(int)damageType] = LoadIcon(path);
        }

        Array resistances = Enum.GetValues(typeof(ResistanceType));
        resistanceIcons = new Sprite[resistances.Length];
        foreach (ResistanceType resistance in resistances)
        {
            if (resistance == ResistanceType.Normal)
                continue;

            string path = $"UI/ResistanceTypeIcon/ResistanceType_{resistance}";
            resistanceIcons[(int)resistance] = LoadIcon(path);
        }
        unknownResistanceIcon = LoadIcon(
            "UI/ResistanceTypeIcon/ResistanceType_Unknown");

        string[] skillTypeIconNames =
        {
            "SkillType_Heal",
            "SkillType_Buff",
            "SkillType_Debuff",
            "SkillType_Ailment"
        };
        foreach (string iconName in skillTypeIconNames)
            skillTypeIcons.Add(iconName,
                LoadIcon($"UI/SkillTypeIcon/{iconName}"));
    }

    private static Sprite LoadIcon(string path)
    {
        Sprite icon = Resources.Load<Sprite>(path);
        if (icon == null)
            throw new InvalidOperationException(
                $"대상 표시 아이콘을 찾을 수 없습니다: Resources/{path}");
        return icon;
    }

    private Sprite GetResistanceIcon(ResistanceType? resistance)
    {
        if (resistance == ResistanceType.Normal)
            return null;

        return resistance.HasValue
            ? resistanceIcons[(int)resistance.Value]
            : unknownResistanceIcon;
    }

    private Sprite GetSkillTypeIcon(string iconName)
    {
        return iconName != null &&
               skillTypeIcons.TryGetValue(iconName, out Sprite icon)
            ? icon
            : null;
    }

    private Color GetDamageTypeColor(DamageType damageType)
    {
        foreach (DamageTypeColor entry in damageTypeColors)
        {
            if (entry.Type == damageType)
                return entry.Color;
        }
        return Color.white;
    }

    private Color GetSkillTypeColor(string iconName)
    {
        return iconName switch
        {
            "SkillType_Heal" => healColor,
            "SkillType_Buff" => buffColor,
            "SkillType_Debuff" => debuffColor,
            "SkillType_Ailment" => ailmentColor,
            _ => Color.white
        };
    }

    //스프라이트에서 설정한 피벗을 UI 아이콘에도 적용함
    private static void SetSprite(UnityEngine.UI.Image image, Sprite sprite)
    {
        if (image.sprite != sprite)
            image.sprite = sprite;

        Vector2 pivot = new(sprite.pivot.x / sprite.rect.width,
            sprite.pivot.y / sprite.rect.height);
        if (image.rectTransform.pivot != pivot)
            image.rectTransform.pivot = pivot;
    }

    //모든 대상 패널을 숨김
    private void HideAll()
    {
        if (targetPanels == null) return;
        foreach (RectTransform panel in targetPanels)
        {
            if (panel != null)
                panel.gameObject.SetActive(false);
        }
    }

    private bool HasValidSlots()
    {
        if (targetPanels == null || targetPanels.Length == 0)
            return false;

        foreach (RectTransform panel in targetPanels)
        {
            if (panel == null)
                return false;
        }
        return true;
    }
}
