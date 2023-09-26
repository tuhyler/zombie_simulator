using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResourceInfoPanel : MonoBehaviour
{
    [SerializeField]
    public TMP_Text resourceAmountText;

    [SerializeField]
    public Image image;

    [SerializeField]
    public Image resourceImage;

    [SerializeField]
    public CanvasGroup backgroundCanvas;

    [SerializeField]
    public Image backgroundImage;

    [SerializeField]
    public RectTransform resourceTransform, allContents;

    public ResourceType resourceType;
    
    private void Awake()
    {
        resourceAmountText.outlineColor = new Color(0f, 0f, 0f);
        resourceAmountText.outlineWidth = .1f;
    }
}
