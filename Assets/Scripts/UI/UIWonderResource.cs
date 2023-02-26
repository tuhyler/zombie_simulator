using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIWonderResource : MonoBehaviour
{
    [SerializeField]
    private TMP_Text resourceAmount, resourcePercent;

    [SerializeField]
    public Image resourceImage, progressBarFill;

    public ResourceType resourceType;
    public bool isActive;

    public void SetResourceAmount(int amount, int totalAmount)
    {
        float perc = (float)amount / totalAmount;
        resourceAmount.text = $"{amount}/{totalAmount}";
        resourcePercent.text = $"{Mathf.RoundToInt(perc * 100)}%";
        progressBarFill.fillAmount = perc;
    }

    public void ToggleActive(bool v)
    {
        if (isActive == v)
            return;

        if (v)
        {
            gameObject.SetActive(v);
            isActive = true;
        }
        else
        {
            gameObject.SetActive(false);
            isActive = false;
        }
    }
    
}
