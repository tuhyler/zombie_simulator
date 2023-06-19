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
    
    [SerializeField]
    private Sprite grassland, desert, grasslandHill, desertHill, grasslandFloodPlain, desertFloodPlain, forest, forestHill, jungle, jungleHill, mountain, swamp, sea, undiscovered;

    [HideInInspector]
    public Vector3Int coordinates;


    public void SetMapPanel(UIMapPanel uiMapPanel)
    {
        this.uiMapPanel = uiMapPanel;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        uiMapPanel.CenterCamera(coordinates);
    }

    public void SetTile(Vector3Int loc, TerrainDesc desc)
    {
        Sprite sprite = null;

        switch (desc)
        {
            case TerrainDesc.Grassland:
                sprite = grassland;
                break;
            case TerrainDesc.Desert:
                sprite = desert;
                break;
            case TerrainDesc.GrasslandHill:
                sprite = grasslandHill;
                break;
            case TerrainDesc.DesertHill:
                sprite = desertHill;
                break;
            case TerrainDesc.GrasslandFloodPlain:
                sprite = grasslandFloodPlain;
                break;
            case TerrainDesc.DesertFloodPlain:
                sprite = desertFloodPlain;
                break;
            case TerrainDesc.Forest:
                sprite = forest;
                break;
            case TerrainDesc.ForestHill:
                sprite = forestHill;
                break;
            case TerrainDesc.Jungle:
                sprite = jungle;
                break;
            case TerrainDesc.JungleHill:
                sprite = jungleHill;
                break;
            case TerrainDesc.Swamp:
                sprite = swamp;
                break;
            case TerrainDesc.Mountain:
                sprite = mountain;
                break;
            case TerrainDesc.Sea:
                sprite = sea;
                break;
            case TerrainDesc.River:
                sprite = sea;
                break;
        }

        coordinates = loc;
        terrainImage.sprite = sprite;
    }

    public void SetResource(Sprite sprite)
    {
        resourceHolder.SetActive(true);
        resourceImage.sprite = sprite;
    }
}
