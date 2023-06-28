using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowNoRotate : MonoBehaviour
{
    [SerializeField]
    public Transform objectToFollow; //in case it can't be specified programmatically
    
    void LateUpdate()
    {
        gameObject.transform.position = objectToFollow.transform.position;
    }
}
