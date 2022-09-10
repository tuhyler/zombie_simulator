using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGeneratorAbstract), true)]
public class TerrainGeneratorButton : Editor
{
    TerrainGeneratorAbstract generator;

    private void Awake()
    {
        generator = (TerrainGeneratorAbstract)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (DrawDefaultInspector())
        {
            if (generator.autoUpdate)
            {
                generator.GenerateMap();
            }
        }

        if (GUILayout.Button("Generate New Map"))
        {
            generator.GenerateMap();
        }

        if (GUILayout.Button("Remove Map"))
        {
            generator.RemoveMap();
        }
    }
}
