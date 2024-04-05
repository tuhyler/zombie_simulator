using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Road Data", menuName = "EconomyData/RoadData")]
public class RoadDataSO : ScriptableObject
{
	public Vector2 roadMaterialCoord;
	public string roadDisplayName;
	public string roadDecription = "Fill in description";
	public Era roadEra;
	public Sprite image;
	public List<ResourceValue> roadCost;
}
