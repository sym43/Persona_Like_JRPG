using System.Collections;
using UnityEngine;

/// <summary>
/// 한 전투 행동의 애니메이션·카메라·결과 표시 순서를 진행함.
/// 전투 결과를 직접 계산하지 않음.
/// </summary>
public sealed class BattleActionView : MonoBehaviour
{
    //전투 흐름을 연결할 컨트롤러
    [SerializeField] private BattleController battleController;
    //상성 문구와 피해 숫자를 표시하는 UI
    [SerializeField] private BattleHitResultView hitResultView;

    //활성화될 때 전투 행동 이벤트를 받음
    private void OnEnable()
    {
        if (battleController == null)
            return;

        battleController.ActionStarted += StartAction;
        battleController.ActionResolved += ShowResult;
    }

    //비활성화될 때 전투 행동 이벤트 연결을 끊음
    private void OnDisable()
    {
        if (battleController == null)
            return;

        battleController.ActionStarted -= StartAction;
        battleController.ActionResolved -= ShowResult;
    }

    //행동 시작 연출을 재생함
    private void StartAction(BattleAction action)
    {
        StartCoroutine(PlayAction(action));
    }

    //행동 결과 연출을 재생함
    private void ShowResult(BattleActionResult result)
    {
        StartCoroutine(PlayResult(result));
    }

    //임시: 실제 공격 애니메이션의 타격 신호를 연결하면 이 대기를 교체함.
    private IEnumerator PlayAction(BattleAction action)
    {
        yield return null;
        battleController.ExecuteAction();
    }

    //임시: 피격 애니메이션을 연결하면 결과 UI와 함께 재생하도록 확장함.
    private IEnumerator PlayResult(BattleActionResult result)
    {
        if (hitResultView != null)
            yield return hitResultView.Show(result);
        else
            yield return null;

        battleController.FinishAction();
    }
}
