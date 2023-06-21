using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

public class UIMapPanelTile : MonoBehaviour, IPointerDownHandler
{
    private UIMapPanel uiMapPanel;
    
    [SerializeField]
    private Image terrainImage, resourceImage;

    [SerializeField]
    private GameObject resourceHolder;
    
    [HideInInspector]
    public Vector3Int coordinates;

    [HideInInspector]
    public Vector3 localCoordinates;

    [HideInInspector]
    public bool isDiscovered;


    public void SetMapPanel(UIMapPanel uiMapPanel)
    {
        this.uiMapPanel = uiMapPanel;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        uiMapPanel.CenterCamera(coordinates);
    }

    public void SetTile(Vector3Int pos, Sprite sprite, int increment)
    {
        coordinates = pos;
        terrainImage.sprite = sprite;

        Vector3 loc = pos;
        loc /= increment;
        loc *= 75;
        loc.x += 37.5f;
        loc.z += -37.5f;
        loc.y = loc.z;
        loc.z = 0;
        localCoordinates = loc;
    }

    public void SetResource(Sprite sprite)
    {
        resourceHolder.SetActive(true);
        resourceImage.sprite = sprite;
    }
}
