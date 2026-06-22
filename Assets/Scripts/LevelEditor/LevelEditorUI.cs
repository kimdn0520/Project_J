using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class LevelEditorUI : MonoBehaviour
{
    [Header("UI Panel References")]
    [SerializeField] private RectTransform paletteContentParent; // ScrollView의 Content
    [SerializeField] private GameObject paletteButtonPrefab;    // 템플릿 버튼 프리팹
    [SerializeField] private RectTransform mouseFollowerIcon;   // 마우스 커서를 따라다닐 선택된 타일 아이콘

    [Header("Action Buttons")]
    [SerializeField] private Button saveButton;
    [SerializeField] private Button clearButton;

    private List<Image> spawnedButtonBgs = new List<Image>();
    private List<LevelItemData> itemsList = new List<LevelItemData>();

    private int currentSelectedIndex = -1;

    private void Start()
    {
        InitializePalette();

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(() =>
            {
                if (LevelEditorManager.Instance != null)
                {
                    LevelSaveLoad.SaveLevelRootAsPrefab(LevelEditorManager.Instance.LevelRoot);
                }
            });
        }

        if (clearButton != null)
        {
            clearButton.onClick.AddListener(() =>
            {
                if (LevelEditorManager.Instance != null)
                {
                    LevelEditorManager.Instance.ClearAll();
                }
            });
        }
    }

    private void Update()
    {
        UpdateMouseFollower();
    }

    private void UpdateMouseFollower()
    {
        if (mouseFollowerIcon == null) return;

        if (LevelEditorManager.Instance != null && LevelEditorManager.Instance.CurrentSelectedItem != null)
        {
            LevelItemData selected = LevelEditorManager.Instance.CurrentSelectedItem;
            Image img = mouseFollowerIcon.GetComponent<Image>();
            
            if (img != null)
            {
                img.sprite = selected.thumbnail;
                img.enabled = selected.thumbnail != null;
            }

            mouseFollowerIcon.gameObject.SetActive(true);
            
            if (Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                // 마우스 커서의 오른쪽 아래 오프셋
                mouseFollowerIcon.position = mousePos + new Vector2(20, -20);
            }
        }
        else
        {
            mouseFollowerIcon.gameObject.SetActive(false);
        }
    }

    private void InitializePalette()
    {
        if (LevelEditorManager.Instance == null) return;
        
        itemsList = LevelEditorManager.Instance.AvailableItems;
        if (itemsList == null) return;

        // 기존 자식들 제거
        foreach (Transform child in paletteContentParent)
        {
            Destroy(child.gameObject);
        }
        spawnedButtonBgs.Clear();
        currentSelectedIndex = -1; // 시작 시 미선택 상태

        // LevelItemData 목록을 받아와 버튼을 동적으로 생성
        for (int i = 0; i < itemsList.Count; i++)
        {
            LevelItemData item = itemsList[i];
            GameObject btnObj = Instantiate(paletteButtonPrefab, paletteContentParent);
            
            // 썸네일 이미지 및 텍스트 세팅
            Image iconImage = btnObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = item.thumbnail != null ? item.thumbnail : null;
            }

            Text nameText = btnObj.transform.Find("NameText")?.GetComponent<Text>();
            if (nameText != null)
            {
                nameText.text = item.itemName;
            }

            Image bgImage = btnObj.GetComponent<Image>();
            if (bgImage != null)
            {
                spawnedButtonBgs.Add(bgImage);
            }

            // 버튼 이벤트 연결
            Button btn = btnObj.GetComponent<Button>();
            int index = i; // 클로저 캡처 방지
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnPaletteItemSelected(index));
            }
        }
    }

    private void OnPaletteItemSelected(int index)
    {
        if (index < 0 || index >= itemsList.Count) return;

        LevelItemData selectedItem = itemsList[index];

        if (currentSelectedIndex == index)
        {
            // 토글 해제: 이미 선택된 단추를 누르면 선택 해제
            currentSelectedIndex = -1;
            LevelEditorManager.Instance.SelectItem(null);
        }
        else
        {
            // 새로 선택
            currentSelectedIndex = index;
            LevelEditorManager.Instance.SelectItem(selectedItem);
        }

        // 하이라이트 UI 연출 (선택된 것만 노란색이나 밝은 색으로, 나머지는 기본색)
        for (int i = 0; i < spawnedButtonBgs.Count; i++)
        {
            if (spawnedButtonBgs[i] == null) continue;

            if (i == currentSelectedIndex)
            {
                spawnedButtonBgs[i].color = Color.yellow; // 하이라이트 색상
            }
            else
            {
                spawnedButtonBgs[i].color = Color.white;  // 기본 색상
            }
        }
    }
}
