using System.Collections.Generic;
using UnityEngine;

public class SelectionHighlight : MonoBehaviour
{
    [SerializeField]
    private Material glowMaterial; 

    [HideInInspector]
    public bool isGlowing;

    List<MeshRenderer> renderers = new();
    List<SkinnedMeshRenderer> renderersSkinned = new();
    List<Material> materialsToUse = new();

    private void Awake()
    {
        PrepareMaterialDictionaries();
    }

    private void PrepareMaterialDictionaries()
    {
        //can only do one material per renderer
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
        {
            renderers.Add(renderer);
            materialsToUse.Add(renderer.sharedMaterial); //accessing info on materials creates a new material, sharedMaterial doesn't
        }

        foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderersSkinned.Add(renderer);
            materialsToUse.Add(renderer.sharedMaterial); //accessing info on materials creates a new material, sharedMaterial doesn't
        }
    }

    public void SetNewRenderer(MeshRenderer[] oldRenderer, MeshRenderer[] newRenderer)
    {
        foreach (MeshRenderer renderer in oldRenderer)
        {
            renderers.Remove(renderer);
        }

        foreach (MeshRenderer renderer in newRenderer)
        {
            renderers.Add(renderer);            
        }
    }

    public void EnableHighlight(Color highlightColor)
    {
        isGlowing = true;

        Material glow = glowMaterial;

        foreach (MeshRenderer renderer in renderers)
        {
            renderer.material = glow;
            foreach (Material item in renderer.materials)
            {
                item.SetColor("_GlowColor", highlightColor);
            }
        }

        foreach (SkinnedMeshRenderer renderer in renderersSkinned)
        {
            renderer.material = glow;
            foreach (Material item in renderer.materials)
            {
                item.SetColor("_GlowColor", highlightColor);
            }
        }
    }

    public void DisableHighlight()
    {
        isGlowing = false;

        int i = 0;
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.material = materialsToUse[i];
            i++;
        }

        foreach (SkinnedMeshRenderer renderer in renderersSkinned)
        {
            renderer.material = materialsToUse[i];
            i++;
        }
    }
}
