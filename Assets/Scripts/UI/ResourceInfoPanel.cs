using TMPro;
using UnityEngine;

public class ResourceInfoPanel : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer resourceImageHolder;
    //[SerializeField]
    //private SpriteMask spriteMask;
    [SerializeField]
    private TMP_Text resourceAmount;

    //void LateUpdate()
    //{
    //    transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    //}

    public void SetResourcePanel(Sprite sprite, int amount, bool haveEnough)
    {
        resourceImageHolder.sprite = sprite;
        //spriteMask.sprite = sprite;
        resourceAmount.text = amount.ToString();
        if (haveEnough)
            resourceAmount.color = Color.white;
        else
            resourceAmount.color = Color.red;
    }
}
