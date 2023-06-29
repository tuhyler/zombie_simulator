using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowNoRotate : MonoBehaviour
{
    [SerializeField]
    public Transform objectToFollow; //in case it can't be specified programmatically

    [SerializeField]
    float yShift = 0.03f;

    void LateUpdate()
    {
        Vector3 follow = objectToFollow.transform.position;
        follow.y += yShift;
        gameObject.transform.position = follow;
    }
}
