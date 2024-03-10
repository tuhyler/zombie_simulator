using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainSaveData
{
	public string name;
	public Vector3Int tileCoordinates;
	public Quaternion rotation, mainRotation, propRotation;
	public RawResourceType rawResourceType;
	public ResourceType resourceType;
	public List<int> uvMapIndex;

	public bool changeLeafColor, showProp = true, border;
	public int variant = 0;
	public int decor = 0;

	public bool isDiscovered, beingCleared;
	public int resourceAmount;
}
