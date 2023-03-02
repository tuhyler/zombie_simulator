using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Road : MonoBehaviour
{
    private MeshFilter meshFilter;
    public MeshFilter MeshFilter { get { return meshFilter; } }

    private SelectionHighlight selectionHighlight;
    public SelectionHighlight SelectionHighlight { get {  return selectionHighlight; } }

    private void Awake()
    {
        meshFilter = GetComponentInChildren<MeshFilter>();
        selectionHighlight = GetComponent<SelectionHighlight>();
    }
}
