using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 아군 한 명의 이름과 HP·SP를 표시하고 현재 차례를 움직임으로 보여줌.
/// </summary>
public sealed class BattleAllySlotView : MonoBehaviour
{
    //현재 차례일 때 움직일 내용 묶음
    [SerializeField] private RectTransform pivot;
    //전투원 이름
    [SerializeField] private TMP_Text nameText;
    //현재 HP와 SP
    [SerializeField] private TMP_Text statusText;
    //현재 차례일 때 이동할 X 위치
    [SerializeField] private float turnPositionX = -50f;
    //차례 표시 이동 시간
    [SerializeField] private float moveDuration = 0.1f;

    private BattleUnit unit;
    private Tween turnTween;

    public BattleUnit Unit => unit;

    private void Awake()
    {
        if (pivot == null || nameText == null || statusText == null)
            throw new InvalidOperationException(
                $"{name} 아군 슬롯 UI 연결이 빠졌습니다.");

        Vector2 position = pivot.anchoredPosition;
        position.x = 0f;
        pivot.anchoredPosition = position;
        turnTween = pivot.DOAnchorPosX(turnPositionX, moveDuration)
            .SetEase(Ease.OutCubic)
            .SetAutoKill(false)
            .Pause();
    }

    private void OnDestroy()
    {
        turnTween?.Kill();
    }

    //표시할 전투원을 연결하고 현재 수치를 갱신함
    public void Bind(BattleUnit battleUnit)
    {
        unit = battleUnit ?? throw new ArgumentNullException(nameof(battleUnit),
            "표시할 아군이 필요합니다.");
        gameObject.SetActive(true);
        Refresh();
    }

    //연결된 전투원의 현재 HP와 SP를 다시 표시함
    public void Refresh()
    {
        if (unit == null) return;

        nameText.text = unit.Data.DisplayName;
        statusText.text = $"HP : {unit.Hp} / {unit.MaxHp}\n" +
                          $"SP : {unit.Sp} / {unit.MaxSp}";
    }

    //현재 차례면 앞으로 움직이고 아니면 원래 위치로 돌아감
    public void ShowTurn(bool isCurrent)
    {
        if (isCurrent)
            turnTween.PlayForward();
        else
            turnTween.PlayBackwards();
    }

    //전투 종료 뒤 다시 사용할 수 있도록 슬롯을 초기화함
    public void Clear()
    {
        turnTween.Rewind();
        unit = null;
        nameText.text = string.Empty;
        statusText.text = string.Empty;
        gameObject.SetActive(false);
    }
}
