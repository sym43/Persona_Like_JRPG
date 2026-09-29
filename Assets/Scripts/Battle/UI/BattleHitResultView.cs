using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 행동 결과의 상성 문구와 피해 숫자를 전투원 위치에 표시함.
/// </summary>
public sealed class BattleHitResultView : MonoBehaviour
{
    [SerializeField] private BattleActorSpawner actorSpawner;
    [SerializeField] private TMP_Text textTemplate;
    [SerializeField] private float displaySeconds = 0.7f;
    [SerializeField] private Vector2 screenOffset = new(0f, 55f);
    [SerializeField] private Vector2 hitOffset = new(20f, -14f);

    private readonly List<TMP_Text> textPool = new();
    private readonly List<ResultText> activeTexts = new();
    private Camera battleCamera;

    //상성 문구와 타수별 피해 숫자를 동시에 표시함
    public IEnumerator Show(BattleActionResult result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result), "표시할 행동 결과가 필요합니다.");

        HideAll();
        if (actorSpawner == null || textTemplate == null)
        {
            Debug.LogError("전투 결과 UI 연결이 빠졌습니다.");
            yield break;
        }

        battleCamera = Camera.main;
        if (battleCamera == null)
        {
            Debug.LogError("전투 결과 UI에 사용할 카메라가 없습니다.");
            yield break;
        }

        HashSet<string> shownLabels = new();
        Dictionary<string, int> damageCounts = new();
        foreach (BattleImpactResult impact in result.Impacts)
        {
            AddLabel(impact, shownLabels);
            if (impact.Damage > 0)
            {
                int hitIndex = damageCounts.TryGetValue(impact.AffectedUnitId, out int count)
                    ? count
                    : 0;
                damageCounts[impact.AffectedUnitId] = hitIndex + 1;
                AddText(impact.AffectedUnitId, impact.Damage.ToString(),
                    hitOffset * hitIndex, Color.white);
            }
        }

        float elapsed = 0f;
        while (elapsed < displaySeconds)
        {
            UpdatePositions();
            elapsed += Time.deltaTime;
            yield return null;
        }

        HideAll();
    }

    //한 대상에 같은 판정 문구가 반복되지 않게 추가함
    private void AddLabel(BattleImpactResult impact, ISet<string> shownLabels)
    {
        if (!impact.Hit)
        {
            AddUniqueLabel(impact.TargetUnitId, "MISS", shownLabels, Color.white);
            return;
        }

        string originalLabel = GetAffinityText(impact.Resistance);
        if (!string.IsNullOrEmpty(originalLabel))
            AddUniqueLabel(impact.TargetUnitId, originalLabel, shownLabels,
                GetLabelColor(impact.Resistance));

        if (!string.Equals(impact.TargetUnitId, impact.AffectedUnitId,
                StringComparison.Ordinal))
        {
            string appliedLabel = GetAffinityText(impact.AppliedResistance);
            if (!string.IsNullOrEmpty(appliedLabel))
                AddUniqueLabel(impact.AffectedUnitId, appliedLabel, shownLabels,
                    GetLabelColor(impact.AppliedResistance));
        }

        bool hasAffinityLabel = !string.IsNullOrEmpty(
            string.Equals(impact.TargetUnitId, impact.AffectedUnitId,
                StringComparison.Ordinal)
                ? originalLabel
                : GetAffinityText(impact.AppliedResistance));
        if (impact.Critical && !hasAffinityLabel)
            AddUniqueLabel(impact.AffectedUnitId, "CRITICAL", shownLabels,
                new Color(1f, 0.85f, 0.2f));
    }

    //대상과 문구 조합을 한 번만 표시함
    private void AddUniqueLabel(string unitId, string label,
        ISet<string> shownLabels, Color color)
    {
        if (string.IsNullOrEmpty(unitId) || !shownLabels.Add(unitId + "\n" + label))
            return;

        AddText(unitId, label, new Vector2(0f, 38f), color);
    }

    //풀에서 텍스트를 가져와 표시 항목에 추가함
    private void AddText(string unitId, string value, Vector2 offset, Color color)
    {
        BattleUnitActor actor = actorSpawner.GetActor(unitId);
        if (actor == null)
            return;

        TMP_Text text = GetText();
        text.text = value;
        text.color = color;
        text.gameObject.SetActive(true);
        activeTexts.Add(new ResultText(text, actor, offset));
    }

    //필요한 만큼 생성하고 이후 행동에서 재사용함
    private TMP_Text GetText()
    {
        foreach (TMP_Text text in textPool)
        {
            if (!text.gameObject.activeSelf)
                return text;
        }

        TMP_Text created = Instantiate(textTemplate, textTemplate.transform.parent);
        created.name = "ResultText";
        textPool.Add(created);
        return created;
    }

    //표시 중인 텍스트를 각 전투원 화면 위치로 이동함
    private void UpdatePositions()
    {
        foreach (ResultText entry in activeTexts)
        {
            Vector3 screenPosition = battleCamera.WorldToScreenPoint(
                entry.Actor.TargetPosition);
            entry.Text.gameObject.SetActive(screenPosition.z > 0f);
            if (screenPosition.z > 0f)
                entry.Text.rectTransform.position = (Vector2)screenPosition +
                                                    screenOffset + entry.Offset;
        }
    }

    //표시 중인 텍스트를 모두 풀로 돌려보냄
    private void HideAll()
    {
        foreach (ResultText entry in activeTexts)
            entry.Text.gameObject.SetActive(false);
        activeTexts.Clear();
    }

    //상성 판정을 화면 문구로 바꿈
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

    //약점만 임시 강조색을 사용함
    private static Color GetLabelColor(ResistanceType? resistance)
    {
        return resistance == ResistanceType.Weak
            ? new Color(0.25f, 0.9f, 1f)
            : Color.white;
    }

    private sealed class ResultText
    {
        public TMP_Text Text { get; }
        public BattleUnitActor Actor { get; }
        public Vector2 Offset { get; }

        public ResultText(TMP_Text text, BattleUnitActor actor, Vector2 offset)
        {
            Text = text;
            Actor = actor;
            Offset = offset;
        }
    }
}
