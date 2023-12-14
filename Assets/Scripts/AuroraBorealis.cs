using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuroraBorealis : MonoBehaviour
{
    public Transform mainCamera;
    Material mat;

    void Start()
    {
        Prepare();
    }

    void LateUpdate()
    {
        float dist = Mathf.Abs(mainCamera.position.z - transform.position.z);
        mat.SetFloat("_Alpha", Mathf.Clamp(dist-3,0,1) * .1f);
	}

	private void Prepare()
	{
        mat = GetComponentInChildren<MeshRenderer>().sharedMaterial; //shared material so they all disappear. Otherwise leaves a weird shadow on mesh
	}
}
