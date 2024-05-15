using UnityEngine;

public class Road : MonoBehaviour
{
    private MeshFilter meshFilter;
    public MeshFilter MeshFilter { get { return meshFilter; } }

    private SelectionHighlight selectionHighlight;
    public SelectionHighlight SelectionHighlight { get {  return selectionHighlight; } }

    private bool embiggened;
    [HideInInspector]
    public int roadLevel;
    private UtilityType type = UtilityType.Road;

    private void Awake()
    {
        meshFilter = GetComponentInChildren<MeshFilter>();
        selectionHighlight = GetComponent<SelectionHighlight>();
    }

    public void Embiggen()
    {
        if (embiggened)
            return;

        embiggened = true;
        Vector3 newScale = new Vector3(1.01f, 1.01f, 1.01f);
        meshFilter.transform.localScale = newScale;
        Vector3 pos = meshFilter.transform.position;
        pos.y += 0.01f;
        meshFilter.transform.position = pos;
    }

    public RoadData SaveData(Vector3Int loc)
    {
        RoadData data = new();

        data.position = loc;
        data.utilityType = type;
        data.utilityLevel = roadLevel; 

        return data;
    }

    public void LoadData(RoadData data)
    {
		type = data.utilityType;
		roadLevel = data.utilityLevel;
	}
}
