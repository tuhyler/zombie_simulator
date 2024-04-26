using System.Collections;
using UnityEngine;
using TMPro;

public class InfoPopUpHandler : MonoBehaviour
{
    private static InfoPopUpHandler warningMessage;

    //private GameObject uiPrefab;

    private TMP_Text popUpText;
    private float visibleTime = 2f;
    private float disappearTimer;
    private Color textColor;
    private Coroutine co;


    private void Awake()
    {
        popUpText = GetComponent<TMP_Text>();
        popUpText.outlineWidth = 0.35f;
        popUpText.outlineColor = Color.black;
    }

    private void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    private IEnumerator ShowMessage()
    {
        float moveSpeed = 0.2f;
        
        while (disappearTimer > 0)
        {
            disappearTimer -= Time.deltaTime;
            transform.position += new Vector3(0, moveSpeed, 0) * Time.deltaTime; //multiplying z to make sure message goes straight up in world space
            
            yield return null;
        }

        while (textColor.a > 0)
        {
            transform.position += new Vector3(0, moveSpeed, 0) * Time.deltaTime;
            float disappearSpeed = 1f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            popUpText.color = textColor;
            yield return null;
        }

        Destroy(gameObject);
    }

    public static InfoPopUpHandler WarningMessage(Transform objectTransform)
    {
        if (warningMessage == null)
        {
            GameObject popUpGO = Instantiate(GameAssets.Instance.popUpTextPrefab, new Vector3(0, 0, 0), Quaternion.Euler(90, 0, 0));
            popUpGO.transform.SetParent(objectTransform, false);
            warningMessage = popUpGO.GetComponent<InfoPopUpHandler>();
        }

        return warningMessage;
    }
    
    public void Create(Vector3 position, string text)
    {
        if (co != null)
            StopCoroutine(co);
        gameObject.SetActive(true);
        //position.x = Mathf.Clamp(position.x, 100f, Screen.width - 100);
        disappearTimer = visibleTime;
        warningMessage.transform.position = position;
        warningMessage.SetPopUpText(text);

        co = StartCoroutine(ShowMessage());
    }

    public static void GetWarningMessage()
    {

    }

    public void CreateWarning(Vector3 position, string text)
    {

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
