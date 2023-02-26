using UnityEngine;
using UnityEngine.UI;

public class ButtonHighlight : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private Image borderImage; 

    private void Awake()
    {
        canvasGroup.alpha = 0;
    }

    public void EnableHighlight(Color colorToChange)
    {
        canvasGroup.alpha = 1;
        borderImage.color = colorToChange;
    }

    public void DisableHighlight()
    {
        canvasGroup.alpha = 0;
    }
}
