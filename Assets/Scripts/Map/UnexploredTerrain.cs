using System.Collections;
using System.Collections.Generic;
//using System.Drawing;
//using Unity.VisualScripting;
using UnityEngine;

public class UnexploredTerrain : MonoBehaviour
{
    private MeshRenderer[] fogMesh;

    private void Awake()
    {
        fogMesh = GetComponentsInChildren<MeshRenderer>();
    }

    public IEnumerator FadeFog()
    {
        Material mat = fogMesh[0].material;
        Color color = mat.color;
        float disappearSpeed = 3f;

        while (color.a > 0)
        {
            color.a -= disappearSpeed * Time.deltaTime;
            mat.color = color;

            yield return null;
        }

        Destroy(gameObject);
    }
}
