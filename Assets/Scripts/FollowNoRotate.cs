using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowNoRotate : MonoBehaviour
{
    [SerializeField]
    public Transform objectToFollow; //in case it can't be specified programmatically

    [SerializeField]
    private float yShift = 0.03f;

    public bool active = true;

    void LateUpdate()
    {
        if (active)
        {
            Vector3 follow = objectToFollow.transform.position;
            follow.y += yShift;
            gameObject.transform.position = follow;
        }
    }
}
