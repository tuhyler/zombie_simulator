using System.Collections;
using UnityEngine;

public class Resource : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer resourceImageHolder;
    [SerializeField]
    private SpriteMask spriteMask;

    private Worker worker;
    private City city;
    [HideInInspector]
    public ResourceIndividualSO resourceIndividual;
    bool clearForest;


    void LateUpdate()
    {
        transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
    }

    public void SetSprites(Sprite sprite)
    {
        resourceImageHolder.sprite = sprite;
        spriteMask.sprite = sprite;
    }

    public void SetInfo(Worker worker, City city, ResourceIndividualSO resourceIndividual, bool clearForest)
    {
        this.worker = worker;
        this.clearForest = clearForest;
        worker.SetResource(this);
        this.city = city;
        this.resourceIndividual = resourceIndividual;
    }

    public IEnumerator SendResourceToCity(int gatheringAmount)
    {
        worker.harvested = false;
        worker.isBusy = false;

        if (clearForest) //ammount of wood received for clearing forest / jungle
            gatheringAmount = 100;

        int amount = worker.world.GetTerrainDataAt(worker.world.RoundToInt(worker.transform.position)).GatherResourceAmount(gatheringAmount, worker);
        worker.RemoveWorkLocation();
        LeanTween.scale(gameObject, Vector3.zero, 0.1f).setOnComplete(DestroyResourceIcon);
        yield return new WaitForSeconds(0.5f);
        city.PlayLightBullet();
        yield return new WaitForSeconds(0.5f);

        int gatheredResource = city.ResourceManager.CheckResource(resourceIndividual.resourceType, amount); //only add one of respective resource
        Vector3 loc = city.cityLoc;
        bool wasted = false;
        if (gatheredResource == 0)
            wasted = true;

        InfoResourcePopUpHandler.CreateResourceStat(loc, amount, ResourceHolder.Instance.GetIcon(resourceIndividual.resourceType), wasted);
        city.PlayResourceSplash();
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
