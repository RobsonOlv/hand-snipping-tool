using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Cysharp.Threading.Tasks;

public class StoredItemsManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform parentTransform;
    public AnchorManager anchorManager;
    public GameObject buttonsContainer;
    public ScrollRect scrollRect;
    public Sprite uiSprite;
    public GameObject emptyListText;
    private List<ScreenshotAnchorData> NonLocalizedAnchors = new();
    private int _menuIndex = 0;
    private Button _selectedButton;
    public List<Button> _buttonList;
    private List<GameObject> _generatedButtons = new List<GameObject>();

    void Start()
    {
        _menuIndex = 0;
        _selectedButton = _buttonList[0];
        _selectedButton.OnSelect(null);
    }

    // Update is called once per frame
    void Update()
    {
        // lock the rotation of this object only around the Y axis from parent
        Vector3 parentEulerAngles = parentTransform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0, parentEulerAngles.y, 0);
        HandleMenuNavigation();
    }

    public void ChangeComponentVisibility()
    {
        bool currentState = gameObject.activeSelf;
        if(!currentState)
        {
            NonLocalizedAnchors = anchorManager.NonLocalizedAnchors;
            PopulateButtons();
            emptyListText.SetActive(NonLocalizedAnchors.Count == 0);
        }
        gameObject.SetActive(!currentState);
        Debug.Log("Component visibility changed. Now active: " + gameObject.activeSelf);
    }

    private void PopulateButtons()
    {
        // Clear previously generated buttons
        foreach (var btn in _generatedButtons)
        {
            if (btn != null)
            {
                Button b = btn.GetComponent<Button>();
                if (_buttonList.Contains(b)) _buttonList.Remove(b);
                Destroy(btn);
            }
        }
        _generatedButtons.Clear();

        foreach (var anchorData in NonLocalizedAnchors)
        {
            CreateAnchorButton(anchorData);
        }
    }

    private void CreateTestButtons()
    {
        Texture2D dogTex = Resources.Load<Texture2D>("Images/dog");
        for (int i = 0; i < 3; i++)
        {
            CreateButtonInstance($"Btn_Test_{i}", dogTex, $"Test Load {i}");
        }
    }

    private void CreateAnchorButton(ScreenshotAnchorData data)
    {
        Texture2D tex = null;
        if (File.Exists(data.texturePath))
        {
            byte[] bytes = File.ReadAllBytes(data.texturePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
        }
        CreateButtonInstance($"Btn_{data.uuid}", tex, "Load");
    }

    private void CreateButtonInstance(string btnName, Texture2D contentTexture, string btnText)
    {
        GameObject buttonObj = new GameObject(btnName);
        buttonObj.transform.SetParent(buttonsContainer.transform, false);
        _generatedButtons.Add(buttonObj);

        // RectTransform
        RectTransform rt = buttonObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(188, 60);

        // Image
        Image img = buttonObj.AddComponent<Image>();
        if (uiSprite != null) img.sprite = uiSprite;
        else img.sprite = Resources.Load<Sprite>("UISprite");
        img.color = Color.white;
        img.type = Image.Type.Sliced;

        // Button
        Button btn = buttonObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = HexToColor("#353535");
        colors.highlightedColor = HexToColor("#353535");
        colors.pressedColor = HexToColor("#1F1F1F");
        colors.selectedColor = HexToColor("#A6A6A6");
        colors.colorMultiplier = 1;
        colors.fadeDuration = 0.1f;
        btn.colors = colors;
        btn.onClick.AddListener(InstantiateSelectedAnchor);

        // Add RectMask2D to prevent overflow
        buttonObj.AddComponent<RectMask2D>();

        // Generic Container with Horizontal Layout Group
        GameObject container = new GameObject("Content");
        container.transform.SetParent(buttonObj.transform, false);
        RectTransform containerRT = container.AddComponent<RectTransform>();
        containerRT.anchorMin = Vector2.zero;
        containerRT.anchorMax = Vector2.one;
        containerRT.offsetMin = Vector2.zero;
        containerRT.offsetMax = Vector2.zero;

        HorizontalLayoutGroup hlg = container.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlHeight = false;
        hlg.childControlWidth = true;
        hlg.spacing = 10;
        hlg.padding = new RectOffset(0, 0, 0, 0);

        // Image (Texture)
        if (contentTexture != null)
        {
            GameObject imgChild = new GameObject("Preview");
            imgChild.transform.SetParent(container.transform, false);
            RawImage rawImg = imgChild.AddComponent<RawImage>();
            rawImg.texture = contentTexture;
            
            LayoutElement le = imgChild.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            le.preferredWidth = 40;
        }

        // TMP Text
        GameObject textChild = new GameObject("Text");
        textChild.transform.SetParent(container.transform, false);
        TextMeshProUGUI tmp = textChild.AddComponent<TextMeshProUGUI>();
        tmp.text = btnText;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 14;
        tmp.color = Color.white;

        _buttonList.Add(btn);
    }

    private Color HexToColor(string hex)
    {
        Color color = Color.white;
        if (ColorUtility.TryParseHtmlString(hex, out color))
        {
            return color;
        }
        return color;
    }

    public async void InstantiateSelectedAnchor()
    {
        int index = _menuIndex - 1; // remove 1 for "close" button
        Debug.Log("InstantiateSelectedAnchor called for menu index: " + index + ", total anchors: " + NonLocalizedAnchors.Count);
        if (index >= 0 && index < NonLocalizedAnchors.Count)
        {
            ScreenshotAnchorData data = NonLocalizedAnchors[index];
            await anchorManager.CreateAnchorObject(data, default, false);
            Debug.Log("Instantiated anchor with UUID: " + data.uuid);
        }
        else
        {
            Debug.LogWarning("Invalid menu index selected: " + index);
        }
    } 

    private void HandleMenuNavigation()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.LThumbstickUp))
        {
            NavigateToIndexInMenu(false);
        }

        if (OVRInput.GetDown(OVRInput.RawButton.LThumbstickDown))
        {
            NavigateToIndexInMenu(true);
        }

        if (OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger))
        {
            _selectedButton.OnSubmit(null);
        }
    }

    private void NavigateToIndexInMenu(bool moveNext)
    {
        if (moveNext)
        {
            _menuIndex++;
            if (_menuIndex > _buttonList.Count - 1)
            {
                _menuIndex = 0;
            }
        }
        else
        {
            _menuIndex--;
            if (_menuIndex < 0)
            {
                _menuIndex = _buttonList.Count - 1;
            }
        }

        _selectedButton.OnDeselect(null);
        _selectedButton = _buttonList[_menuIndex];
        _selectedButton.OnSelect(null);
        ScrollToSelected();
    }

    private void ScrollToSelected()
    {
        if (scrollRect == null || _selectedButton == null) return;

        Canvas.ForceUpdateCanvases();

        RectTransform target = _selectedButton.GetComponent<RectTransform>();
        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;

        // Get target position in content's local space
        Vector2 targetLocalPos = content.InverseTransformPoint(target.position);
        
        float viewportHeight = viewport.rect.height;
        float contentHeight = content.rect.height;

        // Calculate target top position relative to content
        // We want to align the top of the button with the top of the viewport
        float targetTop = targetLocalPos.y + (target.rect.height * (1 - target.pivot.y));

        // Calculate new Y position for content
        // We negate because moving content UP (positive Y) shows lower items
        float newContentY = -targetTop;

        // Clamp to valid scroll range
        float maxScroll = Mathf.Max(0, contentHeight - viewportHeight);
        newContentY = Mathf.Clamp(newContentY, 0, maxScroll);

        content.anchoredPosition = new Vector2(content.anchoredPosition.x, newContentY);
    }
}
