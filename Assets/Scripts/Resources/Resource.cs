using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer resourceImageHolder;
    [SerializeField]
    private SpriteMask spriteMask;

    private Worker worker;
    private City city;
    private ResourceIndividualSO resourceIndividual;

    //private CameraController mainCamera;

    private void Awake()
    {
        //mainCamera = FindObjectOfType<CameraController>();
    }

    void LateUpdate()
    {
        //transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public void SetSprites(Sprite sprite)
    {
        resourceImageHolder.sprite = sprite;
        spriteMask.sprite = sprite;
    }

    public void SetInfo(Worker worker, City city, ResourceIndividualSO resourceIndividual)
    {
        this.worker = worker;
        worker.SetResource(this);
        this.city = city;
        this.resourceIndividual = resourceIndividual;
    }

    public void SendResourceToCity()
    {
        worker.harvested = false;
        worker.isBusy = false;
        city.ResourceManager.CheckResource(resourceIndividual.resourceType, 1); //only add one of respective resource
        LeanTween.scale(gameObject, Vector3.zero, 0.1f).setOnComplete(DestroyResourceIcon);
    }

    public Worker GetHarvestingWorker()
    {
        return worker;
    }

    //public void PrepareResourceSendToCity()
    //{
    //    worker.SendResourceToCity();
    //}

    private void DestroyResourceIcon()
    {
        Destroy(gameObject);
    }
}
