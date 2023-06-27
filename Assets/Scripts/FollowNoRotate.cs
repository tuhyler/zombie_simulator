using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowNoRotate : MonoBehaviour
{
    [HideInInspector]
    public Transform objectToFollow;
    
    void LateUpdate()
    {
        gameObject.transform.position = objectToFollow.transform.position;
    }
}
