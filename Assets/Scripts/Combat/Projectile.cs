using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public AudioClip[] launches, attacks;
    
    public float speed = 2, archHeight = 2.5f;
    public Vector3 adjustment;

    private float flightSpeed;
    private Vector3 originalPosition, startPoint, endPoint, archTop;

    public void SetProjectilePos()
    {
        originalPosition = transform.localPosition;
    }

    public void SetPoints(Vector3 startPoint, Vector3 endPoint)
    {
        startPoint += adjustment;
        this.startPoint = startPoint;
        endPoint.y += 0.5f;
        this.endPoint = endPoint;
        float distance = Mathf.Abs(startPoint.x - endPoint.x) + Mathf.Abs(startPoint.z - endPoint.z);
        flightSpeed = Mathf.Max(speed * 4 - ((speed * 4 - speed) * distance / 7),speed); 
		float flightHeight = Mathf.Min(archHeight * (distance / 7), archHeight);
        archTop = new Vector3((startPoint.x + endPoint.x) * 0.5f, flightHeight, (startPoint.z + endPoint.z) * 0.5f); 
    }

    private Vector3 Evaluate(float t)
    {
        Vector3 ac = Vector3.Lerp(startPoint, archTop, t);
        Vector3 cb = Vector3.Lerp(archTop, endPoint, t);
        return Vector3.Lerp(ac, cb, t);
    }

    public IEnumerator Shoot(Unit unit, Unit target)
    {
        unit.PlayAudioClip(launches[0]);
        gameObject.SetActive(true);

        Vector3 lookAtTarget = endPoint - transform.position;

        Quaternion rotation;
        if (lookAtTarget == Vector3.zero)
            rotation = Quaternion.identity;
        else
            rotation = Quaternion.LookRotation(lookAtTarget);
		//if (lookAtTarget == endPoint)
		//    lookAtTarget += new Vector3(0, 0.05f, 0);

		transform.rotation = rotation;

        //transform.LookAt(endPoint);
        
        float sampleTime = 0;

        while ((transform.position - endPoint).sqrMagnitude > 0.1f)
        {
            sampleTime += Time.deltaTime * flightSpeed;
            transform.position = Evaluate(sampleTime);
            transform.forward = Evaluate(sampleTime + 0.001f) - transform.position;
            yield return null;
        }

        gameObject.SetActive(false);
		transform.localPosition = originalPosition;
		target.ReduceHealth(unit.attackStrength, unit.transform.eulerAngles, attacks[Random.Range(0,attacks.Length)]);
	}

	public IEnumerator ShootTest()
	{
		gameObject.SetActive(true);
		transform.LookAt(endPoint);

		float sampleTime = 0;

		while ((transform.position - endPoint).sqrMagnitude > 0.1f)
		{
			sampleTime += Time.deltaTime * flightSpeed;
			Debug.Log(sampleTime);
			transform.position = Evaluate(sampleTime);
			transform.forward = Evaluate(sampleTime + 0.001f) - transform.position;
			yield return null;
		}

		gameObject.SetActive(false);
		transform.localPosition = originalPosition;
	}
}
