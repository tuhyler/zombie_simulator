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
    private WaitForSeconds halfSecWait = new(0.5f);
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
        if ((city.world.scott == worker && !city.world.mainPlayer.harvested) || (city.world.mainPlayer == worker && !city.world.scott.harvested && !city.world.scott.gathering && !city.world.scott.isMoving))
            city.world.mainPlayer.NoLongerBusyCheck();

        worker.harvested = false;
        worker.harvestedForest = false;

        if (clearForest) //ammount of wood received for clearing forest / jungle
            gatheringAmount = worker.clearedForestlumberAmount;

        int amount = worker.world.GetTerrainDataAt(worker.world.RoundToInt(worker.transform.position)).GatherResourceAmount(gatheringAmount);
        worker.RemoveWorkLocation();
        LeanTween.scale(gameObject, Vector3.zero, 0.1f).setOnComplete(DestroyResourceIcon);
        yield return halfSecWait;
        city.PlayLightBullet();
        yield return halfSecWait;

        city.resourceManager.resourceCount = 0;
        int gatheredResource = city.resourceManager.AddResource(resourceIndividual.resourceType, amount); //only add one of respective resource
        Vector3 loc = city.cityLoc;
        if (gatheredResource > 0)
            InfoResourcePopUpHandler.CreateResourceStat(loc, amount, ResourceHolder.Instance.GetIcon(resourceIndividual.resourceType), city.world);
            
        city.world.StatsCheck(resourceIndividual.resourceType, amount);
        city.PlayResourceSplash();
        city.world.GameCheck("Resource");
        city.world.TutorialCheck("Resource");
    }

    public Worker GetHarvestingWorker()
    {
        return worker;
    }

    private void DestroyResourceIcon()
    {
        GameLoader.Instance.textList.Remove(gameObject);
        Destroy(gameObject);
    }
}
