using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainSaveData
{
	public string name;
	public Vector3Int tileCoordinates;
	public Quaternion rotation, propRotation;
	public ResourceType resourceType;

	public bool showProp = true;
	public int variant = 0;
	public int decor = 0;

	public bool isDiscovered, beingCleared;
	public int resourceAmount;
}
