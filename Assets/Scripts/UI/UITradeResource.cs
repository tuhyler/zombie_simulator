using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITradeResource : MonoBehaviour
{
    [SerializeField]
    private TMP_Text resourceAmountText;
    [SerializeField]
    public Image resourceImage;
    [HideInInspector]
    public int resourceAmount;

    [HideInInspector]
    public ResourceType resourceType;

	private void Awake()
	{
        resourceAmountText.outlineColor = Color.black;
        resourceAmountText.outlineWidth = 0.2f;
    }

	public void SetValue(int val)
    {
        string str = val.ToString();
        resourceAmountText.text = str;
        resourceAmount = val;
        resourceAmountText.rectTransform.sizeDelta = new Vector2(15 + 10 * str.Length, 30);
    }

    public void SetColor(Color color)
    {
        resourceAmountText.color = color;
    }
}
