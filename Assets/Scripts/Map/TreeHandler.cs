using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeHandler : MonoBehaviour
{
    //public MeshFilter leafMesh, hillLeafMesh, roadLeafMesh, roadHillLeafMesh;
    public List<MeshFilter> leafMeshList, hillLeafMeshList, roadLeafMeshList, roadHillLeafMeshList = new();
    public GameObject treeFlat, treeHill, treeFlatRoad, treeHillRoad, treeFlatIcon, treeHillIcon;
    public bool keepTrees, hasRoad;
    public Transform propMesh;
    public List<MeshRenderer> leafRendererList, hillLeafRendererList;

    private List<Material> originalMat = new();

	private void Awake()
	{
        originalMat.Add(leafRendererList[0].sharedMaterial);

        if (leafRendererList.Count > 1)
    		originalMat.Add(leafRendererList[leafRendererList.Count - 1].sharedMaterial);
	}

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

        hasRoad = true;

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

        hasRoad = false;

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

    public void ToggleForestClear(bool v, bool isHill, Material clearMat)
    {
        if (isHill)
        {
            if (v)
            {
                for (int i = 0; i < hillLeafRendererList.Count; i++)
                    hillLeafRendererList[i].sharedMaterial = clearMat;
            }
            else
            {
				for (int i = 0; i < hillLeafRendererList.Count; i++)
                {
                    if (i < hillLeafRendererList.Count - 1)
                        hillLeafRendererList[i].sharedMaterial = originalMat[0];
                    else
                        hillLeafRendererList[i].sharedMaterial = originalMat[originalMat.Count - 1];
				}
			}
		}
        else
        {
			if (v)
			{
				for (int i = 0; i < leafRendererList.Count; i++)
					leafRendererList[i].sharedMaterial = clearMat;
			}
			else
			{
				for (int i = 0; i < leafRendererList.Count; i++)
				{
					if (i < leafRendererList.Count - 1)
						leafRendererList[i].sharedMaterial = originalMat[0];
					else
						leafRendererList[i].sharedMaterial = originalMat[originalMat.Count - 1];
				}
			}
		}
    }
}
