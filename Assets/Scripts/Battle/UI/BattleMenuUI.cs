using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 전투 메뉴를 열고 현재 입력 단계에 맞게 키 입력을 나눔.
/// 실제 전투 행동은 PlayerActionInput에 전달함.
/// </summary>
public sealed class BattleMenuUI : MonoBehaviour
{
    //전투 흐름과 선택 가능한 데이터를 받을 컨트롤러
    [SerializeField] private BattleController battleController;
    //확정한 행동을 전투에 전달함
    [SerializeField] private PlayerActionInput playerActionInput;
    //현재 선택한 전투 대상을 관리함
    [SerializeField] private BattleTargetSelector targetSelector;

    //공격, 스킬, 아이템, 방어 입력을 보여주는 기본 패널
    [SerializeField] private GameObject actionPanel;
    //사용할 스킬을 선택하는 패널
    [SerializeField] private GameObject skillPanel;
    //사용할 아이템을 선택하는 패널
    [SerializeField] private GameObject itemPanel;
    //선택한 적의 공개된 정보를 보여주는 패널
    [SerializeField] private GameObject analyzePanel;
    //현재 행동자의 아니마 정보를 보여주는 패널
    [SerializeField] private GameObject personaStatusPanel;
    //총공격 실행 여부를 선택하는 패널
    [SerializeField] private GameObject allOutAttackPanel;
    //방어 실행 여부를 선택하는 패널
    [SerializeField] private GameObject guardCheckPanel;
    //원모어를 동료에게 넘기는 입력 표시
    [SerializeField] private GameObject shiftAction;

    private readonly Stack<GameObject> openedPanels = new();
    private IReadOnlyList<SkillData> skills = Array.Empty<SkillData>();
    private IReadOnlyList<ItemData> items = Array.Empty<ItemData>();
    private IReadOnlyList<Anima> infoAnimas = Array.Empty<Anima>();
    private int skillIndex;
    private int itemIndex;
    private int infoAnimaIndex;
    private int infoOpenedFrame = -1;
    private SkillData pendingSkill;
    private ItemData pendingItem;
    private BattleInputMode analyzeReturnMode;
    private BattleTargetType analyzeReturnTargetType;

    //현재 키 입력이 조작하는 단계
    public BattleInputMode InputMode { get; private set; } =
        BattleInputMode.None;
    //스킬 목록에서 현재 가리키는 스킬
    public SkillData SelectedSkill => skills.Count == 0 ? null : skills[skillIndex];
    //현재 목록에 표시할 사용 가능한 스킬
    public IReadOnlyList<SkillData> Skills => skills;
    //현재 행동 중인 전투원
    public BattleUnit CurrentUnit => battleController?.CurrentUnit;
    //아이템 목록에서 현재 가리키는 아이템
    public ItemData SelectedItem => items.Count == 0 ? null : items[itemIndex];
    //현재 목록에 표시할 사용 가능한 아이템
    public IReadOnlyList<ItemData> Items => items;
    //적 분석 화면에서 현재 가리키는 적
    public BattleUnit SelectedAnalyzeUnit => targetSelector?.SelectedEnemy;
    //아니마 정보 화면에서 현재 가리키는 지참 아니마. 동료는 전투원 정보로 표시함
    public Anima SelectedAnimaInfo => infoAnimas.Count == 0
        ? null
        : infoAnimas[infoAnimaIndex];

    //현재 가진 아이템 수량을 구함
    public int GetItemCount(string itemId)
    {
        return battleController?.GetItemCount(itemId) ?? 0;
    }

    //전투원이 가진 액티브·패시브 스킬 원본을 구함
    public IReadOnlyList<SkillData> GetUnitSkills(BattleUnit unit)
    {
        return battleController?.GetUnitSkills(unit) ?? Array.Empty<SkillData>();
    }

    //지참 아니마가 가진 액티브·패시브 스킬 원본을 구함
    public IReadOnlyList<SkillData> GetAnimaSkills(Anima anima)
    {
        return battleController?.GetAnimaSkills(anima) ?? Array.Empty<SkillData>();
    }

    //단일 스킬은 현재 적, 광역 스킬은 적 중 하나라도 약점이면 약점으로 표시함
    public ResistanceType? GetSkillResistance(SkillData skill)
    {
        if (skill == null)
            return null;

        if (skill.TargetType != BattleTargetType.AllEnemies)
        {
            BattleUnit enemy = targetSelector?.SelectedEnemy;
            return enemy == null
                ? null
                : battleController.GetKnownSkillResistance(enemy.BattleId, skill.Id);
        }

        foreach (BattleUnit enemy in battleController.GetOpponents())
        {
            if (battleController.GetKnownSkillResistance(enemy.BattleId, skill.Id) ==
                ResistanceType.Weak)
                return ResistanceType.Weak;
        }
        return null;
    }

    //스킬 아이콘에 사용할 첫 피해 효과 속성을 구함
    public DamageType? GetSkillDamageType(SkillData skill)
    {
        return skill == null
            ? null
            : battleController.GetSkillDamageType(skill.Id);
    }

    //피해 속성이 없는 스킬에 사용할 종류 아이콘 이름을 구함
    public string GetSkillTypeIconName(SkillData skill)
    {
        if (skill == null)
            return null;
        if (skill.UseType == SkillUseType.Passive)
            return "SkillType_Passive";

        string iconName = null;
        int iconPriority = 0;
        foreach (SkillEffectData effect in battleController.GetSkillEffects(skill.Id))
        {
            switch (effect.Type)
            {
                case SkillEffectType.Heal:
                case SkillEffectType.RemoveDebuffs:
                case SkillEffectType.CureMentalStates:
                    SetIcon("SkillType_Heal", 4, ref iconName, ref iconPriority);
                    break;
                case SkillEffectType.Analyze:
                case SkillEffectType.RemoveBuffs:
                    SetIcon("SkillType_Debuff", 2, ref iconName, ref iconPriority);
                    break;
                case SkillEffectType.ApplyEffect:
                    BattleEffectCategory? category =
                        battleController.GetBattleEffectCategory(effect.EffectId);
                    if (category == BattleEffectCategory.MentalState)
                        SetIcon("SkillType_Ailment", 3, ref iconName, ref iconPriority);
                    else if (category == BattleEffectCategory.Debuff)
                        SetIcon("SkillType_Debuff", 2, ref iconName, ref iconPriority);
                    else
                        SetIcon("SkillType_Buff", 1, ref iconName, ref iconPriority);
                    break;
            }
        }

        return iconName;
    }

    private static void SetIcon(string candidate, int priority,
        ref string iconName, ref int iconPriority)
    {
        if (priority <= iconPriority)
            return;

        iconName = candidate;
        iconPriority = priority;
    }

    //현재 적 대상 UI에 표시할 공격 속성을 구함
    public DamageType? GetTargetDamageType()
    {
        if (InputMode == BattleInputMode.Action)
            return CurrentUnit?.BasicAttack?.DamageType;
        if (InputMode != BattleInputMode.Target)
            return null;
        if (pendingSkill != null)
            return battleController.GetSkillDamageType(pendingSkill.Id);
        return pendingItem == null
            ? null
            : battleController.GetItemDamageType(pendingItem.Id);
    }

    //현재 대상 UI에 표시할 회복·지원·상태이상 스킬 아이콘 이름을 구함
    public string GetTargetSkillTypeIconName()
    {
        return InputMode == BattleInputMode.Target && pendingSkill != null
            ? GetSkillTypeIconName(pendingSkill)
            : null;
    }

    //현재 공격 속성이 대상 적에게 보이는 상성을 구함
    public ResistanceType? GetTargetResistance(BattleUnit enemy)
    {
        DamageType? damageType = GetTargetDamageType();
        return !damageType.HasValue || enemy == null || !enemy.IsEnemy
            ? null
            : battleController.GetKnownResistance(enemy.BattleId, damageType.Value);
    }

    //적 정보 화면에서 해당 속성의 공개된 상성을 구함
    public ResistanceType? GetTargetResistanceForInfo(BattleUnit enemy,
        DamageType damageType)
    {
        return enemy == null || !enemy.IsEnemy
            ? null
            : battleController.GetKnownResistance(enemy.BattleId, damageType);
    }

    //스킬 목록의 선택이 바뀌었을 때 알림
    public event Action<SkillData> SkillSelectionChanged;
    //아이템 목록의 선택이 바뀌었을 때 알림
    public event Action<ItemData> ItemSelectionChanged;
    //시프트에서 가리키는 동료가 바뀌었을 때 알림
    public event Action<BattleUnitActor> ShiftTargetChanged;
    //시프트 대상 선택이 끝났을 때 알림
    public event Action ShiftSelectionClosed;
    //단일 아군 행동에서 가리키는 아군이 바뀌었을 때 알림
    public event Action<BattleUnitActor> AllyTargetChanged;
    //단일 아군 대상 선택을 취소했을 때 알림
    public event Action AllyTargetSelectionClosed;
    //적 분석 화면에서 가리키는 적이 바뀌었을 때 알림
    public event Action<BattleUnit> AnalyzeTargetChanged;
    //아니마 상태 화면에서 표시 대상이 바뀌었을 때 알림
    public event Action PersonaStatusChanged;

    //처음에는 모든 메뉴 패널을 닫아둠
    private void Awake()
    {
        actionPanel.SetActive(false);
        skillPanel.SetActive(false);
        itemPanel.SetActive(false);
        analyzePanel.SetActive(false);
        personaStatusPanel.SetActive(false);
        allOutAttackPanel.SetActive(false);
        guardCheckPanel.SetActive(false);
        shiftAction.SetActive(false);
    }

    //전투 진행 이벤트를 받기 시작함
    private void OnEnable()
    {
        if (battleController == null || playerActionInput == null ||
            targetSelector == null || shiftAction == null)
        {
            Debug.LogError("전투 메뉴에 컨트롤러, 행동 입력 또는 대상 선택기가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        battleController.PlayerTurnStarted += OpenActionPanel;
        battleController.AllOutAttackAvailable += OpenAllOutAttackPanel;
        battleController.ActionStarted += CloseMenuForAction;
        battleController.BattleEnded += CloseMenuForBattleEnd;
    }

    //전투 진행 이벤트 연결을 끊음
    private void OnDisable()
    {
        if (battleController == null)
            return;

        battleController.PlayerTurnStarted -= OpenActionPanel;
        battleController.AllOutAttackAvailable -= OpenAllOutAttackPanel;
        battleController.ActionStarted -= CloseMenuForAction;
        battleController.BattleEnded -= CloseMenuForBattleEnd;
    }

    //아군 차례가 시작되면 기본 행동 패널과 첫 상대를 선택함
    private void OpenActionPanel(BattleUnit unit)
    {
        CloseAll();
        OpenPanel(actionPanel, BattleInputMode.Action);
        SelectBasicAttackTarget();
        shiftAction.SetActive(battleController.IsOneMoreTurn &&
                              battleController.GetShiftTargets().Count > 0);
    }

    //스킬 선택 패널을 엶
    private void OpenSkillPanel()
    {
        skills = battleController.GetUsableSkills();
        if (skills.Count == 0) return;

        skillIndex = 0;
        OpenPanel(skillPanel, BattleInputMode.Skill);
        SkillSelectionChanged?.Invoke(SelectedSkill);
    }

    //아이템 선택 패널을 엶
    private void OpenItemPanel()
    {
        items = battleController.GetUsableItems();
        if (items.Count == 0) return;

        itemIndex = 0;
        OpenPanel(itemPanel, BattleInputMode.Item);
        ItemSelectionChanged?.Invoke(SelectedItem);
    }

    //적 분석 패널을 엶
    private void OpenAnalyzePanel()
    {
        analyzeReturnMode = InputMode;
        analyzeReturnTargetType = targetSelector.TargetType;
        if (analyzeReturnTargetType == BattleTargetType.AllEnemies)
            SetTargets(BattleTargetType.OneEnemy);
        infoOpenedFrame = Time.frameCount;
        OpenPanel(analyzePanel, BattleInputMode.Analyze);
        NotifyAnalyzeTarget();
    }

    //현재 행동자의 아니마 상태 패널을 엶
    private void OpenPersonaStatusPanel()
    {
        infoAnimas = CurrentUnit?.Data.Role == UnitRole.MainCharacter
            ? battleController.GetBattleAnimas()
            : Array.Empty<Anima>();
        infoAnimaIndex = 0;
        for (int index = 0; index < infoAnimas.Count; index++)
        {
            if (!string.Equals(infoAnimas[index].InstanceId,
                    CurrentUnit.AnimaInstanceId, StringComparison.Ordinal))
                continue;
            infoAnimaIndex = index;
            break;
        }
        infoOpenedFrame = Time.frameCount;
        OpenPanel(personaStatusPanel, BattleInputMode.PersonaStatus);
        PersonaStatusChanged?.Invoke();
    }

    //대상 선택 없이 총공격 실행 여부를 선택함
    private void OpenAllOutAttackPanel(IReadOnlyList<BattleUnit> _)
    {
        CloseAll();
        OpenPanel(allOutAttackPanel, BattleInputMode.AllOutAttack);
    }

    //스킬 입력을 받으면 스킬 선택 패널을 엶
    public void HandleSkillInput(InputAction.CallbackContext context)
    {
        if (context.performed && InputMode == BattleInputMode.Action)
            OpenSkillPanel();
    }

    //아이템 입력을 받으면 아이템 선택 패널을 엶
    public void HandleItemInput(InputAction.CallbackContext context)
    {
        if (context.performed && InputMode == BattleInputMode.Action)
            OpenItemPanel();
    }

    //행동 선택 또는 적 대상 선택 중 적 정보 패널을 엶
    public void HandleShowEnemyAnimaInfoInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (InputMode == BattleInputMode.Action ||
            (InputMode == BattleInputMode.Target && pendingSkill != null &&
             (pendingSkill.TargetType == BattleTargetType.OneEnemy ||
              pendingSkill.TargetType == BattleTargetType.AllEnemies)))
            OpenAnalyzePanel();
    }

    //스킬 선택 중 현재 행동자의 아니마 정보 패널을 엶
    public void HandleShowAllyAnimaInfoInput(InputAction.CallbackContext context)
    {
        if (context.performed && InputMode == BattleInputMode.Skill)
            OpenPersonaStatusPanel();
    }

    //정보 화면의 적 또는 지참 아니마를 순환함
    public void HandleMoveInfoInput(InputAction.CallbackContext context)
    {
        if (!context.performed || Time.frameCount == infoOpenedFrame) return;

        int direction = GetDirection(context.ReadValue<float>());
        if (direction == 0) return;
        if (InputMode == BattleInputMode.Analyze)
        {
            targetSelector.Move(direction);
            NotifyAnalyzeTarget();
        }
        else if (InputMode == BattleInputMode.PersonaStatus &&
                 infoAnimas.Count > 1)
        {
            infoAnimaIndex = Wrap(infoAnimaIndex + direction,
                infoAnimas.Count);
            PersonaStatusChanged?.Invoke();
        }
    }

    //기본 행동 화면에서 현재 대상을 일반 공격함
    public void HandleAttackInput(InputAction.CallbackContext context)
    {
        if (!context.performed || InputMode != BattleInputMode.Action)
            return;

        playerActionInput.ChooseBasicAttack(targetSelector.GetTargetId());
    }

    //기본 행동 화면에서 방어를 선택함
    public void HandleGuardInput(InputAction.CallbackContext context)
    {
        if (context.performed && InputMode == BattleInputMode.Action)
            OpenPanel(guardCheckPanel, BattleInputMode.GuardCheck);
    }

    //원모어에서 시프트 대상 선택을 시작함
    public void HandleShiftInput(InputAction.CallbackContext context)
    {
        if (context.performed && InputMode == BattleInputMode.Action &&
            shiftAction.activeSelf)
            BeginShiftSelection();
    }

    //스킬 또는 아이템 목록의 선택을 위아래로 이동함
    public void HandleMoveListInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        int direction = GetDirection(context.ReadValue<float>());
        if (direction == 0) return;
        if (InputMode == BattleInputMode.Skill)
            MoveSkill(-direction);
        else if (InputMode == BattleInputMode.Item)
            MoveItem(-direction);
    }

    //기본 행동 또는 대상 선택 화면에서 대상을 좌우로 이동함
    public void HandleMoveTargetInput(InputAction.CallbackContext context)
    {
        if (!context.performed ||
            (InputMode != BattleInputMode.Action &&
             InputMode != BattleInputMode.Target &&
             InputMode != BattleInputMode.Shift))
            return;

        int direction = GetDirection(context.ReadValue<float>());
        if (direction != 0)
        {
            targetSelector.Move(direction);
            if (InputMode == BattleInputMode.Shift)
                NotifyShiftTarget();
            else if (InputMode == BattleInputMode.Target &&
                     targetSelector.TargetType == BattleTargetType.OneAlly)
                NotifyAllyTarget();
        }
    }

    //현재 선택한 스킬·아이템·대상을 확정함
    public void HandleConfirmInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (InputMode == BattleInputMode.Skill)
        {
            pendingSkill = SelectedSkill;
            pendingItem = null;
            BeginTargetSelection(pendingSkill.TargetType);
        }
        else if (InputMode == BattleInputMode.Item)
        {
            pendingItem = SelectedItem;
            pendingSkill = null;
            BeginTargetSelection(pendingItem.TargetType);
        }
        else if (InputMode == BattleInputMode.Target)
        {
            SubmitPendingAction();
        }
        else if (InputMode == BattleInputMode.Shift)
        {
            SubmitShift();
        }
        else if (InputMode == BattleInputMode.PersonaStatus)
        {
            ConfirmSelectedAnima();
        }
        else if (InputMode == BattleInputMode.AllOutAttack)
        {
            playerActionInput.ChooseAllOutAttack(true);
        }
        else if (InputMode == BattleInputMode.GuardCheck)
        {
            playerActionInput.ChooseGuard();
        }
    }

    //현재 대상 선택을 취소하거나 이전 패널로 돌아감
    public void HandleCancelInput(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (InputMode == BattleInputMode.Action)
            OpenPanel(guardCheckPanel, BattleInputMode.GuardCheck);
        else if (InputMode == BattleInputMode.Analyze)
            CloseAnalyzePanel();
        else if (InputMode == BattleInputMode.Target)
            CancelTargetSelection();
        else if (InputMode == BattleInputMode.Shift)
            CancelShiftSelection();
        else if (InputMode == BattleInputMode.AllOutAttack)
            playerActionInput.ChooseAllOutAttack(false);
        else
            GoBack();
    }

    //편성 순서대로 시프트할 동료를 선택하기 시작함
    private void BeginShiftSelection()
    {
        IReadOnlyList<BattleUnit> shiftTargets = battleController.GetShiftTargets();
        if (shiftTargets.Count == 0)
        {
            shiftAction.SetActive(false);
            return;
        }

        targetSelector.SetTargets(BattleTargetType.OneAlly,
            battleController.CurrentUnit, Array.Empty<BattleUnit>(),
            shiftTargets, Array.Empty<BattleUnit>());
        actionPanel.SetActive(false);
        InputMode = BattleInputMode.Shift;
        NotifyShiftTarget();
    }

    //시프트를 취소하고 기본 행동 화면으로 돌아감
    private void CancelShiftSelection()
    {
        InputMode = BattleInputMode.Action;
        actionPanel.SetActive(true);
        SelectBasicAttackTarget();
        ShiftSelectionClosed?.Invoke();
    }

    //선택한 동료에게 원모어를 넘김
    private void SubmitShift()
    {
        string unitId = targetSelector.GetTargetId();
        CloseAll();
        playerActionInput.ChooseShift(unitId);
    }

    //현재 시프트 대상의 화면 표시와 카메라 갱신을 요청함
    private void NotifyShiftTarget()
    {
        if (targetSelector.SelectedActors.Count == 1)
            ShiftTargetChanged?.Invoke(targetSelector.SelectedActors[0]);
    }

    //고른 행동의 대상 방식에 맞는 대상 선택을 시작함
    private void BeginTargetSelection(BattleTargetType targetType)
    {
        SetTargets(targetType);
        if (openedPanels.Count > 0)
            openedPanels.Peek().SetActive(false);
        InputMode = BattleInputMode.Target;
        if (targetType == BattleTargetType.OneAlly)
            NotifyAllyTarget();
    }

    //대상 선택을 취소하고 스킬 또는 아이템 목록으로 돌아감
    private void CancelTargetSelection()
    {
        bool wasSelectingAlly = targetSelector.TargetType ==
                                BattleTargetType.OneAlly;
        pendingSkill = null;
        pendingItem = null;
        if (openedPanels.Count == 0) return;

        GameObject panel = openedPanels.Peek();
        panel.SetActive(true);
        InputMode = GetInputMode(panel);
        if (InputMode == BattleInputMode.Skill)
            SkillSelectionChanged?.Invoke(SelectedSkill);
        else if (InputMode == BattleInputMode.Item)
            ItemSelectionChanged?.Invoke(SelectedItem);
        if (wasSelectingAlly)
            AllyTargetSelectionClosed?.Invoke();
    }

    //확정한 스킬 또는 아이템을 현재 대상에게 사용함
    private void SubmitPendingAction()
    {
        string targetId = targetSelector.GetTargetId();
        if (pendingSkill != null)
            playerActionInput.ChooseSkill(pendingSkill.Id, targetId);
        else if (pendingItem != null)
            playerActionInput.ChooseItem(pendingItem.Id, targetId);
        else
            throw new InvalidOperationException("확정할 스킬 또는 아이템이 없습니다.");
    }

    //현재 행동의 기본 공격 대상으로 살아 있는 상대를 설정함
    private void SelectBasicAttackTarget()
    {
        SetTargets(BattleTargetType.OneEnemy);
    }

    //대상 종류에 필요한 전투원 목록을 대상 선택기에 전달함
    private void SetTargets(BattleTargetType targetType)
    {
        targetSelector.SetTargets(targetType, battleController.CurrentUnit,
            battleController.GetOpponents(), battleController.GetAllies(),
            battleController.GetDeadAllies());
    }

    //현재 선택한 단일 아군의 화면 갱신을 요청함
    private void NotifyAllyTarget()
    {
        if (targetSelector.SelectedActors.Count == 1)
            AllyTargetChanged?.Invoke(targetSelector.SelectedActors[0]);
    }

    //현재 분석 화면에서 가리키는 적의 정보 갱신을 요청함
    private void NotifyAnalyzeTarget()
    {
        AnalyzeTargetChanged?.Invoke(targetSelector.SelectedEnemy);
    }

    //정보 화면에서 고른 아니마를 장착하고 스킬 목록을 갱신함
    private void ConfirmSelectedAnima()
    {
        Anima anima = SelectedAnimaInfo;
        if (anima == null || CurrentUnit == null ||
            string.Equals(anima.InstanceId, CurrentUnit.AnimaInstanceId,
                StringComparison.Ordinal) || !battleController.CanChangeAnima)
            return;

        playerActionInput.ChooseAnima(anima.InstanceId);
        skills = battleController.GetUsableSkills();
        skillIndex = 0;
        GoBack();
    }

    //적 정보 화면을 닫고 열기 전 행동 또는 대상 선택으로 돌아감
    private void CloseAnalyzePanel()
    {
        if (openedPanels.Count == 0) return;

        openedPanels.Pop().SetActive(false);
        if (analyzeReturnMode == BattleInputMode.Action &&
            openedPanels.Count > 0)
            openedPanels.Peek().SetActive(true);
        InputMode = analyzeReturnMode;
        if (analyzeReturnTargetType == BattleTargetType.AllEnemies)
            SetTargets(BattleTargetType.AllEnemies);
        analyzeReturnMode = BattleInputMode.None;
    }

    //스킬 목록에서 선택 위치를 이동함
    private void MoveSkill(int direction)
    {
        if (skills.Count == 0) return;
        skillIndex = Wrap(skillIndex + direction, skills.Count);
        SkillSelectionChanged?.Invoke(SelectedSkill);
    }

    //아이템 목록에서 선택 위치를 이동함
    private void MoveItem(int direction)
    {
        if (items.Count == 0) return;
        itemIndex = Wrap(itemIndex + direction, items.Count);
        ItemSelectionChanged?.Invoke(SelectedItem);
    }

    //새 패널을 열고 현재 패널은 숨김
    private void OpenPanel(GameObject panel, BattleInputMode inputMode)
    {
        if (panel == null)
        {
            Debug.LogError("열려는 전투 패널이 연결되지 않았습니다.");
            return;
        }

        if (openedPanels.Count > 0)
        {
            GameObject currentPanel = openedPanels.Peek();
            if (currentPanel == panel) return;
            currentPanel.SetActive(false);
        }

        openedPanels.Push(panel);
        panel.SetActive(true);
        InputMode = inputMode;
    }

    //현재 패널을 닫고 이전 패널로 돌아감
    private void GoBack()
    {
        if (openedPanels.Count <= 1) return;

        openedPanels.Pop().SetActive(false);
        GameObject previousPanel = openedPanels.Peek();
        previousPanel.SetActive(true);
        InputMode = GetInputMode(previousPanel);
        if (InputMode == BattleInputMode.Action)
            SelectBasicAttackTarget();
        else if (InputMode == BattleInputMode.Skill)
            SkillSelectionChanged?.Invoke(SelectedSkill);
    }

    //전투 행동이 시작되면 모든 메뉴를 닫음
    private void CloseMenuForAction(BattleAction action)
    {
        CloseAll();
    }

    //전투가 끝나면 모든 메뉴를 닫음
    private void CloseMenuForBattleEnd(BattleState state)
    {
        CloseAll();
    }

    //열린 패널과 대상 선택을 모두 정리함
    private void CloseAll()
    {
        bool wasSelectingShift = InputMode == BattleInputMode.Shift;
        while (openedPanels.Count > 0)
            openedPanels.Pop().SetActive(false);

        skills = Array.Empty<SkillData>();
        items = Array.Empty<ItemData>();
        infoAnimas = Array.Empty<Anima>();
        pendingSkill = null;
        pendingItem = null;
        analyzeReturnMode = BattleInputMode.None;
        shiftAction?.SetActive(false);
        targetSelector?.Clear();
        InputMode = BattleInputMode.None;
        if (wasSelectingShift)
            ShiftSelectionClosed?.Invoke();
    }

    //패널에 맞는 입력 단계를 구함
    private BattleInputMode GetInputMode(GameObject panel)
    {
        if (panel == actionPanel) return BattleInputMode.Action;
        if (panel == skillPanel) return BattleInputMode.Skill;
        if (panel == itemPanel) return BattleInputMode.Item;
        if (panel == analyzePanel) return BattleInputMode.Analyze;
        if (panel == personaStatusPanel) return BattleInputMode.PersonaStatus;
        if (panel == allOutAttackPanel) return BattleInputMode.AllOutAttack;
        if (panel == guardCheckPanel) return BattleInputMode.GuardCheck;
        return BattleInputMode.None;
    }

    private static int GetDirection(float value)
    {
        if (value > 0.5f) return 1;
        if (value < -0.5f) return -1;
        return 0;
    }

    private static int Wrap(int value, int count)
    {
        return (value % count + count) % count;
    }
}
