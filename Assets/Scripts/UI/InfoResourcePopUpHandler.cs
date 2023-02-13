using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InfoResourcePopUpHandler : MonoBehaviour
{
    //private TMP_Text popUpText;
    private float disappearTimer = 2f;
    private Color textColor;
    private Color resourceIconColor;
    [SerializeField]
    private SpriteRenderer resourceImage;
    [SerializeField]
    private TMP_Text popUpText;

    private void Awake()
    {
        //popUpText = GetComponent<TMP_Text>();
        popUpText.outlineWidth = 0.35f;
        popUpText.outlineColor = Color.black;
    }

    private void Update()
    {
        float moveSpeed = 0.2f;
        transform.position += new Vector3(0, moveSpeed, 0) * Time.deltaTime;

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            // start disappearing
            float disappearSpeed = 1f;
            textColor.a -= disappearSpeed * Time.deltaTime;
            resourceIconColor.a -= disappearSpeed * Time.deltaTime;
            popUpText.color = textColor;
            resourceImage.color = resourceIconColor;

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

    public static InfoResourcePopUpHandler CreateResourceStat(Vector3 position, int number, Sprite image, bool waste = false)
    {
        GameObject popUpGO = Instantiate(GameAssets.Instance.popUpResourceNumbersPrefab, position, Quaternion.Euler(90, 0, 0));

        InfoResourcePopUpHandler popUpResourceNumbers = popUpGO.GetComponent<InfoResourcePopUpHandler>();
        popUpResourceNumbers.SetPopUpResourceNumber(number, image, waste);

        return popUpResourceNumbers;
    }

    public void SetPopUpResourceNumber(int number, Sprite image, bool waste)
    {
        if (waste)
        {
            popUpText.text = $"No Room for {number} ";
            popUpText.color = Color.red;
        }
        else if (number > 0)
        {
            popUpText.color = Color.green;
            popUpText.text = "+" + number.ToString();
        }
        else if (number < 0)
        {
            popUpText.color = Color.red;
            popUpText.text = number.ToString();
        }
        else
        {
            popUpText.color = Color.white;
            popUpText.text = "+" + number.ToString();
        }

        textColor = popUpText.color;
        resourceImage.sprite = image;
        resourceIconColor = resourceImage.color;
    }
}
