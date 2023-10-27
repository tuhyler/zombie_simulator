using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeHandler : MonoBehaviour
{
    public MeshFilter leafMesh;
    public GameObject treeFlat, treeHill, treeFlatRoad, treeHillRoad, treeFlatIcon, treeHillIcon;
    public bool keepTrees;

    public void TurnOffGraphics(bool clearForest)
    {
        if (!keepTrees || clearForest)
        {
            treeFlat.SetActive(false);
            treeHill.SetActive(false);
            treeFlatRoad.SetActive(false);
            treeHillRoad.SetActive(false);
            treeFlatIcon.SetActive(false);
            treeHillIcon.SetActive(false);
        }
    }

    public void SetMapIcon(bool isHill)
    {
        if (keepTrees)
            return;
        
        if (isHill)
            treeHillIcon.SetActive(true);
        else
            treeFlatIcon.SetActive(true);
    }

    public void SwitchToRoad(bool isHill)
    {
        if (keepTrees)
            return;

        if (isHill)
        {
            treeHill.SetActive(false);
            treeHillRoad.SetActive(true);
        }
        else
        {
            treeFlat.SetActive(false);
            treeFlatRoad.SetActive(true);
        }
    }

    public void SwitchFromRoad(bool isHill)
    {
        if (keepTrees)
            return;

        if (isHill)
        {
            treeHill.SetActive(true);
            treeHillRoad.SetActive(false);
        }
        else
        {
            treeFlat.SetActive(true);
            treeFlatRoad.SetActive(false);
        }
    }
}
