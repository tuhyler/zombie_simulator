using UnityEngine;
using UnityEngine.UI;

public class ImprovementResource : MonoBehaviour
{
    public SpriteRenderer image;

    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public void SetImage(Sprite image)
    {
        this.image.sprite = image;
    }
}
