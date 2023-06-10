using System.Collections.Generic;
using UnityEngine;

public class SelectionHighlight : MonoBehaviour
{
    [SerializeField]
    private Material glowMaterial, originalMaterial, secondaryGlowMaterial, originalShader; //first one is for textures, 2nd is regular material, 3rd is alternate

    //[SerializeField]
    //private Material glowMaterialColor; //for colors
    [HideInInspector]
    public bool isGlowing;

    List<MeshRenderer> renderers = new();
    List<SkinnedMeshRenderer> renderersSkinned = new();

    List<int> shaderLoc = new();

    //private Color glowColor;

    private void Awake()
    {
        PrepareMaterialDictionaries();
        //glowColor = glowMaterial.GetColor("_GlowColor");
    }

    private void PrepareMaterialDictionaries() //puts glowing and original materials in dictionaries at the beginning
    {
        //careful working with materials in this, could eliminate static status
        
        if (originalMaterial != null)
        {
            glowMaterial.mainTexture = originalMaterial.mainTexture;
            if (secondaryGlowMaterial != null)
                secondaryGlowMaterial.mainTexture = originalMaterial.mainTexture;
        }

        int i = 0;
        //first meshrenderers then skinnedmeshrenderers
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
        {
            if (originalShader != null && renderer.material.shader.name == originalShader.shader.name)
                shaderLoc.Add(i); 
            renderers.Add(renderer);
            i++;
        }

        foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            renderersSkinned.Add(renderer);
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

    //private Material[] GetMaterialsFromMesh(Material[] originalMaterials, int materialLength)
    //{
    //    Material[] newMaterials = new Material[materialLength];

    //    for (int i = 0; i < materialLength; i++)
    //    {
    //        if (originalMaterials[i].mainTexture == null)
    //        {
    //            //for simple colors;
    //            if (!cachedGlowColors.TryGetValue(originalMaterials[i].color, out Material mat))
    //            {
    //                //mat = new Material(glowMaterialColor);
    //                //By default, Unity considers a color with the property name "_Color" to be the main color
    //                mat.color = originalMaterials[i].color;
    //                cachedGlowColors[mat.color] = mat;
    //            }

    //            newMaterials[i] = mat;

    //            continue;
    //        }

    //        //for textures;
    //        if (!cachedGlowTextures.TryGetValue(originalMaterials[i].mainTexture, out Material mat2))
    //        {
    //            mat2 = new Material(glowMaterial);
    //            //By default, Unity considers a texture with the property name "_MainTex" to be the main texture
    //            mat2.mainTexture = originalMaterials[i].mainTexture;
    //            cachedGlowTextures[mat2.mainTexture] = mat2;
    //        }

    //        newMaterials[i] = mat2;
    //    }

    //    return newMaterials; 
    //}

    //public void ResetGlowHighlight() //goes back to original color (not necessary)
    //{
    //    foreach (MeshRenderer renderer in renderers)
    //    {
    //        foreach (Material item in renderer.materials)
    //        {
    //            item.SetColor("_GlowColor", glowColor);
    //        }
    //    }

    //    foreach (SkinnedMeshRenderer renderer in renderersSkinned)
    //    {
    //        foreach (Material item in renderer.materials)
    //        {
    //            item.SetColor("_GlowColor", glowColor);
    //        }
    //    }
    //}


    public void EnableHighlight(Color highlightColor, bool secondary = false)
    {
        isGlowing = true;

        Material glow;

        if (secondary)
            glow = secondaryGlowMaterial;
        else
            glow = glowMaterial;

        foreach(MeshRenderer renderer in renderers)
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
            if (shaderLoc.Contains(i))
                renderer.material = originalShader;
            else
                renderer.material = originalMaterial;

            i++;
        }

        foreach (SkinnedMeshRenderer renderer in renderersSkinned)
        {
            renderer.material = originalMaterial;
        }
    }
}
