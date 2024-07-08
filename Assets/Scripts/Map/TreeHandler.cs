using System.Collections.Generic;
using UnityEngine;

public class TreeHandler : MonoBehaviour
{
    public List<MeshFilter> leafMeshList;
    public List<GameObject> roadTreeObjects;
    public Transform propMesh;

    public void RoadSwitch(bool toRoad)
    {
        for (int i = 0; i < roadTreeObjects.Count; i++)
            roadTreeObjects[i].SetActive(toRoad);
    }
}
