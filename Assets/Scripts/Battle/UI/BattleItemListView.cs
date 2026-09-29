using System;
using TMPro;
using UnityEngine;

/// <summary>
/// 사용 가능한 아이템을 8개 고정 행에 표시하고 선택 위치에 맞춰 목록을 한 칸씩 이동함.
/// </summary>
public sealed class BattleItemListView : MonoBehaviour
{
    private const int VisibleSlotCount = 8;

    //현재 아이템 목록과 선택 아이템을 알려주는 전투 메뉴
    [SerializeField] private BattleMenuUI battleMenu;
    //아이템 행들이 들어 있는 세로 목록
    [SerializeField] private RectTransform itemList;
    //현재 선택한 아이템 설명
    [SerializeField] private TMP_Text descriptionText;
    //현재 목록 위치를 보여주는 스크롤 표시
    [SerializeField] private UnityEngine.UI.Scrollbar scrollbar;
    //현재 선택한 행 색상
    [SerializeField] private Color selectedColor = new(0.1f, 0.45f, 0.9f, 0.9f);
    //선택하지 않은 행 색상
    [SerializeField] private Color normalColor = new(0f, 0f, 0f, 0.55f);

    private GameObject[] rows = Array.Empty<GameObject>();
    private TMP_Text[] nameTexts = Array.Empty<TMP_Text>();
    private TMP_Text[] countTexts = Array.Empty<TMP_Text>();
    private UnityEngine.UI.Image[] backgrounds = Array.Empty<UnityEngine.UI.Image>();
    private int firstVisibleIndex;

    //미리 만든 아이템 행을 찾음
    private void Awake()
    {
        if (battleMenu == null || itemList == null || descriptionText == null ||
            scrollbar == null)
        {
            Debug.LogError("아이템 목록 UI 연결이 빠졌습니다.");
            enabled = false;
            return;
        }

        if (itemList.childCount != VisibleSlotCount)
            throw new InvalidOperationException("아이템 목록에는 8개 행이 필요합니다.");

        rows = new GameObject[VisibleSlotCount];
        nameTexts = new TMP_Text[VisibleSlotCount];
        countTexts = new TMP_Text[VisibleSlotCount];
        backgrounds = new UnityEngine.UI.Image[VisibleSlotCount];
        for (int index = 0; index < VisibleSlotCount; index++)
        {
            Transform row = itemList.GetChild(index);
            rows[index] = row.gameObject;
            backgrounds[index] = row.GetComponent<UnityEngine.UI.Image>();
            nameTexts[index] = row.Find("Name")?.GetComponent<TMP_Text>();
            countTexts[index] = row.Find("Count")?.GetComponent<TMP_Text>();
            if (backgrounds[index] == null || nameTexts[index] == null ||
                countTexts[index] == null)
                throw new InvalidOperationException(
                    $"아이템 목록 {index + 1}번 행 구성이 올바르지 않습니다.");
        }
    }

    //아이템 선택 변경을 받기 시작함
    private void OnEnable()
    {
        if (battleMenu != null)
            battleMenu.ItemSelectionChanged += ShowItems;
    }

    //아이템 선택 변경 연결을 끊음
    private void OnDisable()
    {
        if (battleMenu != null)
            battleMenu.ItemSelectionChanged -= ShowItems;
    }

    //선택 위치에 맞는 8개 아이템과 수량을 표시함
    private void ShowItems(ItemData selectedItem)
    {
        descriptionText.text = selectedItem?.Description ?? string.Empty;
        int selectedIndex = FindItemIndex(selectedItem);
        int itemCount = battleMenu.Items.Count;
        firstVisibleIndex = CalculateFirstVisibleIndex(selectedIndex, itemCount,
            VisibleSlotCount, firstVisibleIndex);

        for (int slotIndex = 0; slotIndex < rows.Length; slotIndex++)
        {
            int itemIndex = firstVisibleIndex + slotIndex;
            bool used = itemIndex < itemCount;
            rows[slotIndex].SetActive(used);
            if (!used) continue;

            ItemData item = battleMenu.Items[itemIndex];
            nameTexts[slotIndex].text = item.DisplayName;
            countTexts[slotIndex].text = $"× {battleMenu.GetItemCount(item.Id)}";
            backgrounds[slotIndex].color = item == selectedItem
                ? selectedColor
                : normalColor;
        }

        int maxFirstIndex = Mathf.Max(0, itemCount - VisibleSlotCount);
        scrollbar.size = itemCount == 0
            ? 1f
            : Mathf.Min(1f, VisibleSlotCount / (float)itemCount);
        scrollbar.value = maxFirstIndex == 0
            ? 1f
            : 1f - firstVisibleIndex / (float)maxFirstIndex;
    }

    //선택한 아이템의 전체 목록 위치를 찾음
    private int FindItemIndex(ItemData selectedItem)
    {
        for (int index = 0; index < battleMenu.Items.Count; index++)
        {
            if (battleMenu.Items[index] == selectedItem)
                return index;
        }
        return 0;
    }

    //선택이 위·아래 끝 행에 닿을 때 보이는 범위를 한 칸 이동함
    private static int CalculateFirstVisibleIndex(int selectedIndex, int itemCount,
        int slotCount, int currentFirstIndex)
    {
        int maxFirstIndex = Mathf.Max(0, itemCount - slotCount);
        if (selectedIndex <= 0) return 0;
        if (selectedIndex >= itemCount - 1) return maxFirstIndex;

        int visibleIndex = selectedIndex - currentFirstIndex;
        if (visibleIndex >= slotCount - 1)
            return Mathf.Min(currentFirstIndex + 1, maxFirstIndex);
        if (visibleIndex <= 0)
            return Mathf.Max(currentFirstIndex - 1, 0);
        return Mathf.Clamp(currentFirstIndex, 0, maxFirstIndex);
    }
}
