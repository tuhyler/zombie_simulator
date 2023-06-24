using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMapPanelTile : MonoBehaviour
{
    [SerializeField]
    private Image terrainImage, improvementImage, resourceImage;

    [SerializeField]
    private GameObject resourceHolder;
    
    [HideInInspector]
    public bool isDiscovered, hasResources;


    public void SetTile(Sprite sprite)
    {
        terrainImage.sprite = sprite;
    }

    public void SetResource(Sprite sprite)
    {
        resourceHolder.SetActive(true);
        resourceImage.sprite = sprite;
        hasResources = true;
    }

    public void SetImprovement(Sprite sprite)
    {
        improvementImage.gameObject.SetActive(true);
        improvementImage.sprite = sprite;
        if (!hasResources)
            improvementImage.transform.localPosition = new Vector3(0, 0, 0);
    }

    public void RemoveImprovement()
    {
        improvementImage.gameObject.SetActive(false);
    }

    public void RemoveResource()
    {
        hasResources = false;
        improvementImage.transform.localPosition = new Vector3(0, 0, 0);
        resourceHolder.SetActive(false);
    }
}
