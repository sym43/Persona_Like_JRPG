using TMPro;
using UnityEngine;

/// <summary>
/// 시프트에서 선택한 동료의 이름을 화면 왼쪽 위에 표시함.
/// </summary>
public sealed class BattleShiftTargetView : MonoBehaviour
{
    //시프트 대상 변경을 알려주는 전투 메뉴
    [SerializeField] private BattleMenuUI battleMenu;
    //시프트 대상 이름 패널
    [SerializeField] private GameObject targetPanel;
    //선택한 동료 이름
    [SerializeField] private TMP_Text nameText;

    //처음에는 이름 패널을 숨김
    private void Awake()
    {
        targetPanel.SetActive(false);
    }

    //시프트 대상 변경과 선택 종료 알림을 받기 시작함
    private void OnEnable()
    {
        if (battleMenu == null || targetPanel == null || nameText == null)
        {
            Debug.LogError("시프트 대상 이름 UI 연결이 빠졌습니다.");
            enabled = false;
            return;
        }

        battleMenu.ShiftTargetChanged += ShowTarget;
        battleMenu.ShiftSelectionClosed += Hide;
    }

    //시프트 대상 변경과 선택 종료 연결을 끊음
    private void OnDisable()
    {
        if (battleMenu == null)
            return;

        battleMenu.ShiftTargetChanged -= ShowTarget;
        battleMenu.ShiftSelectionClosed -= Hide;
    }

    //선택한 동료 이름을 표시함
    private void ShowTarget(BattleUnitActor actor)
    {
        if (actor == null || actor.Unit == null)
        {
            Hide();
            return;
        }

        nameText.text = actor.Unit.Data.DisplayName;
        targetPanel.SetActive(true);
    }

    //시프트 대상 이름을 숨김
    private void Hide()
    {
        targetPanel.SetActive(false);
    }
}
