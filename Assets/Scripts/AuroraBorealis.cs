using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuroraBorealis : MonoBehaviour
{
    public Transform mainCamera;
    Material mat;
    float r, g, b;

    void Start()
    {
        Prepare();
    }

    void LateUpdate()
    {
        float dist = Mathf.Abs(mainCamera.position.y - transform.position.y) + Mathf.Abs(mainCamera.position.z - transform.position.z);
        mat.SetColor("_Color", new Color(r, g, b, Mathf.Clamp(dist-5,0,1) * .1f));
	}

	private void Prepare()
	{
        mat = GetComponentInChildren<MeshRenderer>().material;
        r = mat.color.r;
        g = mat.color.g;
        b = mat.color.b;
	}
}
