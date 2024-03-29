using TMPro;
using UnityEngine;

public class CityLaborTileNumber : MonoBehaviour
{
    //[SerializeField]
    //private SpriteRenderer laborNumberHolder;

    [SerializeField]
    private TMP_Text laborNumberText;

    private void Awake()
    {
        //laborNumberHolder.GetComponent<SpriteRenderer>().enabled = false;
        laborNumberText.outlineWidth = 0.35f;
        laborNumberText.outlineColor = new Color(0, 0, 0, 255); 
        laborNumberText.GetComponent<TMP_Text>().enabled = false;
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public void SetLaborNumber(string turnCount)
    {
        //laborNumberHolder.enabled = true;
        laborNumberText.enabled = true;
        laborNumberText.text = turnCount;
    }

    public void SetActive(bool v)
    {
        gameObject.SetActive(v);
    }
}
