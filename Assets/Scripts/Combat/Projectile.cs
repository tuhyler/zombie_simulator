using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
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
        float distance = (startPoint - endPoint).sqrMagnitude;
        flightSpeed = speed * 4 - ((speed * 4 - speed) * distance / 30);
        float flightHeight = archHeight * (distance / 30);
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
        gameObject.SetActive(true);

        Vector3 lookAtTarget = endPoint - transform.position;
        if (lookAtTarget == endPoint)
            lookAtTarget += new Vector3(0, 0.05f, 0);

        transform.rotation = Quaternion.LookRotation(lookAtTarget);

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
		target.ReduceHealth(unit.attackStrength, unit.transform.eulerAngles);
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
