using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResourceInfoPanel : MonoBehaviour
{
    [SerializeField]
    public TMP_Text resourceAmount;

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
}
