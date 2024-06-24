using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CityImprovementStats : MonoBehaviour
{
	[SerializeField]
	private TMP_Text laborNumberText;

	[SerializeField]
	private SpriteRenderer resourceImage;

	private void Awake()
	{
		laborNumberText.outlineWidth = 0.35f;
		laborNumberText.outlineColor = new Color(0, 0, 0, 255);
	}

	void LateUpdate()
	{
		transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
	}

	public void SetLaborNumber(string turnCount)
	{
		laborNumberText.text = turnCount;
	}

	public void SetImage(Sprite resourceImage)
	{
		this.resourceImage.gameObject.SetActive(true);
		this.resourceImage.sprite = resourceImage;
	}

	public void HideImage()
	{
		resourceImage.gameObject.SetActive(false);
	}

	public void SetActive(bool v)
	{
		gameObject.SetActive(v);
	}
}
