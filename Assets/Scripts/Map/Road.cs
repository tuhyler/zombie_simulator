using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Road : MonoBehaviour
{
    private MeshFilter meshFilter;
    public MeshFilter MeshFilter { get { return meshFilter; } }

    private SelectionHighlight selectionHighlight;
    public SelectionHighlight SelectionHighlight { get {  return selectionHighlight; } }

    private bool embiggened;

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
}
