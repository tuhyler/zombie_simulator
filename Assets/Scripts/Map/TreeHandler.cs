using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeHandler : MonoBehaviour
{
    //public MeshFilter leafMesh, hillLeafMesh, roadLeafMesh, roadHillLeafMesh;
    public List<MeshFilter> leafMeshList, hillLeafMeshList, roadLeafMeshList, roadHillLeafMeshList = new();
    public GameObject treeFlat, treeHill, treeFlatRoad, treeHillRoad, treeFlatIcon, treeHillIcon;
    public bool keepTrees;
    public Transform propMesh;

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

    public void SetMapIcon(bool isHill/*, Quaternion rotation*/)
    {
        if (keepTrees)
            return;
        
        if (isHill)
        {
            treeHillIcon.SetActive(true);
            //treeHillIcon.transform.rotation = rotation/*Quaternion.Inverse(rotation)*/;
        }
        else
        {
            treeFlatIcon.SetActive(true);
            //treeFlatIcon.transform.rotation = rotation/*Quaternion.Inverse(rotation)*/;
        }
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
