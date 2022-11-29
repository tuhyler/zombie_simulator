using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InfoPopUpHandler : MonoBehaviour
{
    private TMP_Text popUpText;
    private float disappearTimer = 2f;
    private Color textColor;

    private void Awake()
    {
        popUpText = GetComponent<TMP_Text>();
        popUpText.outlineWidth = 0.35f;
        popUpText.outlineColor = Color.black;
    }

    private void Update()
    {
        float moveSpeed = 0.2f;
        transform.position += new Vector3(0, 0, moveSpeed) * Time.deltaTime;

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            // start disappearing
            float disappearSpeed = 1f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            popUpText.color = textColor;

            if (textColor.a < 0)
            {
                Destroy(gameObject);
            }
        }
    }

    private void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public static InfoPopUpHandler Create(Vector3 position, string text)
    {
        GameObject popUpGO = Instantiate(GameAssets.Instance.popUpTextPrefab, position, Quaternion.Euler(90,0,0));

        InfoPopUpHandler popUpText = popUpGO.GetComponent<InfoPopUpHandler>();
        popUpText.SetPopUpText(text);

        return popUpText;
    }

    public void SetPopUpText(string text)
    {
        popUpText.text = text;
        textColor = popUpText.color;
    }
}
