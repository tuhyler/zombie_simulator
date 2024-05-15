using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITooltip : MonoBehaviour
{
    public TMP_Text messageText;
    public RectTransform allContents;
    public LayoutElement layoutElement;
    public int characterWrapLimit = 40;

    private void Awake()
    {
        gameObject.SetActive(false);
        Color fade = messageText.color;
        fade.a = 0;
        messageText.color = fade;
    }

    public void SetInfo(string text)
    {
        Vector3 p = Input.mousePosition;
        float x = 0.5f;
        float y = 0f;

        p.z = 935f;
        if (p.y + allContents.rect.height > Screen.height)
            y = 1f;

        if (p.x + allContents.rect.width * 0.5f > Screen.width)
            x = 1f;
        else if (p.x - allContents.rect.width * 0.5 < 0)
            x = 0f;

        allContents.pivot = new Vector2(x, y);

        Vector3 pos = Camera.main.ScreenToWorldPoint(p);
        allContents.transform.position = pos;

        layoutElement.enabled = (text.Length > characterWrapLimit) ? true : false;        
        messageText.text = text;
    }
}
