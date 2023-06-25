using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMapPanelTile : MonoBehaviour
{
    [SerializeField]
    public Image terrainImage;

    [HideInInspector]
    public bool isDiscovered, hasResources;

    private TerrainDesc tileDesc;
    public TerrainDesc TileDesc { get { return tileDesc; } set { tileDesc = value; } }

    public void SetTile(Sprite sprite)
    {
        terrainImage.sprite = sprite;
    }
}
