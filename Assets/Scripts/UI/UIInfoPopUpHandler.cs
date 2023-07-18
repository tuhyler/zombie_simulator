using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIInfoPopUpHandler : MonoBehaviour
{
    private static UIInfoPopUpHandler warningMessage;

    [SerializeField]
    private TMP_Text popUpText;

    private float visibleTime = 2f;
    private float disappearTimer;
    private Color textColor;
    private Coroutine co;


    private void Awake()
    {
        warningMessage = FindObjectOfType<UIInfoPopUpHandler>();
        
        popUpText.outlineWidth = 0.35f;
        popUpText.outlineColor = Color.black;
    }

    private void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public void SetWarningMessage(UIInfoPopUpHandler uiInfoPopUpHandler)
    {
        warningMessage = uiInfoPopUpHandler;
    }

    private IEnumerator ShowMessage()
    {
        float moveSpeed = 20f;

        while (disappearTimer > 0)
        {
            disappearTimer -= Time.deltaTime;
            transform.localPosition += new Vector3(0, moveSpeed, 0) * Time.deltaTime; 
            yield return null;
        }

        while (textColor.a > 0)
        {
            transform.localPosition += new Vector3(0, moveSpeed, 0) * Time.deltaTime;
            float disappearSpeed = 1f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            popUpText.color = textColor;
            yield return null;
        }

        gameObject.SetActive(false);
        co = null;
    }

    public static UIInfoPopUpHandler WarningMessage()
    {
        if (!warningMessage)
            warningMessage = FindObjectOfType<UIInfoPopUpHandler>();

        return warningMessage;
    }

    public void Create(Vector3 position, string text, bool toWorld = true) //'true' if inputting mouse position
    {
        if (co != null)
        {
            StopCoroutine(co);
            gameObject.SetActive(false);
        }

        if (toWorld)
        {
            //position.z = 935;
            position.z = 1;
            Vector3 positionWorld = Camera.main.ScreenToWorldPoint(position);
            transform.position = positionWorld;
        }
        else
        {
            Vector3 positionScreen = Camera.main.WorldToScreenPoint(position);
            //positionScreen.z = 935;
            position.z = 1;
            Vector3 positionAgain = Camera.main.ScreenToWorldPoint(positionScreen);
            transform.position = positionAgain;
        }

        gameObject.SetActive(true);
        disappearTimer = visibleTime;
        SetPopUpText(text);

        co = StartCoroutine(ShowMessage());
    }

    private void SetPopUpText(string text)
    {
        popUpText.text = text;
        textColor = popUpText.color;
        textColor.a = 1f;
        popUpText.color = textColor;
    }
}
