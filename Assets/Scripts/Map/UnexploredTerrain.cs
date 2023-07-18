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
        Material mat;
        
        for (int i = 0; i < fogMesh.Length; i++)
        {
            if (i == 0)
            {
                mat = fogMesh[i].material;
                Color color = mat.color;

                while (color.a > 0)
                {
                    float disappearSpeed = 3f;
                    color.a -= disappearSpeed * Time.deltaTime;
                    mat.color = color;

                    yield return null;
                }
            }
            //else
            //{
            //    mat = fogMesh[i].material;
            //    float alpha = 0;
            //    mat.SetFloat("_Alpha", alpha);

            //    while (alpha > 0)
            //    {
            //        float disappearSpeed = 2f;
            //        alpha -= disappearSpeed * Time.deltaTime;
            //        mat.SetFloat("_Alpha", alpha);

            //        yield return null;
            //    }
            //}
        }
        //Material mat = fogMesh.material;
        //Color color = mat.color;
        //float alpha = 1;

        //while (alpha > 0)
        //{
        //    float disappearSpeed = 2f;
        //    alpha -= disappearSpeed * Time.deltaTime;
        //    mat.SetFloat("_Alpha", alpha);

        //    yield return null;
        //}

        //while (color.a > 0)
        //{
        //    float disappearSpeed = 2f;
        //    color.a -= disappearSpeed * Time.deltaTime;
        //    mat.color = color;

        //    yield return null;
        //}

        gameObject.SetActive(false);
    }
}
