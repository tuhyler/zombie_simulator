using System.Collections.Generic;
using UnityEngine;

public class SelectionHighlight : MonoBehaviour
{
    Dictionary<MeshRenderer, Material[]> glowMaterialDictionary = new();
    Dictionary<MeshRenderer, Material[]> originalMaterialDictionary = new();
    Dictionary<SkinnedMeshRenderer, Material[]> glowSkinnedMaterialDictionary = new();
    Dictionary<SkinnedMeshRenderer, Material[]> originalSkinnedMaterialDictionary = new();

    Dictionary<Color, Material> cachedGlowColors = new();
    Dictionary<Texture, Material> cachedGlowTextures = new();

    [SerializeField]
    private Material glowMaterial; //for textures

    [SerializeField]
    private Material glowMaterialColor; //for colors
    private bool isGlowing;

    private Color glowColor;

    private void Awake()
    {
        PrepareMaterialDictionaries();
        glowColor = glowMaterial.GetColor("_GlowColor");
    }

    private void PrepareMaterialDictionaries() //puts glowing and original materials in dictionaries at the beginning
    {
        //first meshrenderers then skinnedmeshrenderers
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
        {
            Material[] originalMaterials = renderer.materials;
            originalMaterialDictionary.Add(renderer, originalMaterials);

            Material[] newMaterials = GetMaterialsFromMesh(originalMaterials, renderer.materials.Length);
            glowMaterialDictionary.Add(renderer, newMaterials);
        }
        
        foreach (SkinnedMeshRenderer renderer in GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            Material[] originalMaterials = renderer.materials;
            originalSkinnedMaterialDictionary.Add(renderer, originalMaterials);

            Material[] newMaterials = GetMaterialsFromMesh(originalMaterials, renderer.materials.Length);
            glowSkinnedMaterialDictionary.Add(renderer, newMaterials);
        }
    }

    public void SetNewRenderer(MeshRenderer[] oldRenderer, MeshRenderer[] newRenderer)
    {
        foreach (MeshRenderer renderer in oldRenderer)
        {
            originalMaterialDictionary.Remove(renderer);
            glowMaterialDictionary.Remove(renderer);
        }

        foreach (MeshRenderer renderer in newRenderer)
        {
            Material[] originalMaterials = renderer.materials;
            originalMaterialDictionary.Add(renderer, originalMaterials);

            Material[] newMaterials = GetMaterialsFromMesh(originalMaterials, renderer.materials.Length);
            glowMaterialDictionary.Add(renderer, newMaterials);
        }
    }

    private Material[] GetMaterialsFromMesh(Material[] originalMaterials, int materialLength)
    {
        Material[] newMaterials = new Material[materialLength];

        for (int i = 0; i < materialLength; i++)
        {
            if (originalMaterials[i].mainTexture == null)
            {
                //for simple colors;
                if (!cachedGlowColors.TryGetValue(originalMaterials[i].color, out Material mat))
                {
                    mat = new Material(glowMaterialColor);
                    //By default, Unity considers a color with the property name "_Color" to be the main color
                    mat.color = originalMaterials[i].color;
                    cachedGlowColors[mat.color] = mat;
                }

                newMaterials[i] = mat;

                continue;
            }

            //for textures;
            if (!cachedGlowTextures.TryGetValue(originalMaterials[i].mainTexture, out Material mat2))
            {
                mat2 = new Material(glowMaterial);
                //By default, Unity considers a texture with the property name "_MainTex" to be the main texture
                mat2.mainTexture = originalMaterials[i].mainTexture;
                cachedGlowTextures[mat2.mainTexture] = mat2;
            }

            newMaterials[i] = mat2;
        }

        return newMaterials; 
    }

    public void ResetGlowHighlight() //goes back to original color
    {
        //first meshrenderer, then skinnedmeshrenderer
        foreach (MeshRenderer renderer in glowMaterialDictionary.Keys)
        {
            foreach (Material item in glowMaterialDictionary[renderer])
            {
                item.SetColor("_GlowColor", glowColor);
            }
        }

        foreach (SkinnedMeshRenderer renderer in glowSkinnedMaterialDictionary.Keys)
        {
            foreach (Material item in glowSkinnedMaterialDictionary[renderer])
            {
                item.SetColor("_GlowColor", glowColor);
            }
        }
    }


    public void EnableHighlight(Color highlightColor)
    {
        if (isGlowing)
            return;

        isGlowing = true;
        ResetGlowHighlight();
        foreach (MeshRenderer renderer in originalMaterialDictionary.Keys)
        {
            renderer.materials = glowMaterialDictionary[renderer];
            foreach (Material item in glowMaterialDictionary[renderer]) //sets the glow color for each item in dictionary
            {
                item.SetColor("_GlowColor", highlightColor);
            }

        }

        foreach (SkinnedMeshRenderer renderer in originalSkinnedMaterialDictionary.Keys)
        {
            renderer.materials = glowSkinnedMaterialDictionary[renderer];
            foreach (Material item in glowSkinnedMaterialDictionary[renderer]) //sets the glow color for each item in dictionary
            {
                item.SetColor("_GlowColor", highlightColor);
            }

        }
    }

    public void DisableHighlight()
    {
        if (!isGlowing)
            return;

        isGlowing = false;
        
        foreach (MeshRenderer renderer in originalMaterialDictionary.Keys)
        {
            renderer.materials = originalMaterialDictionary[renderer];
        }

        foreach (SkinnedMeshRenderer renderer in originalSkinnedMaterialDictionary.Keys)
        {
            renderer.materials = originalSkinnedMaterialDictionary[renderer];
        }
    }
}
