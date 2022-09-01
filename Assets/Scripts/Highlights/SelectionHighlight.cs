using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionHighlight : MonoBehaviour
{
    Dictionary<MeshRenderer, Material[]> glowMaterialDictionary = new();
    Dictionary<MeshRenderer, Material[]> originalMaterialDictionary = new();
    Dictionary<Color, Material> cachedGlowColors = new();
    Dictionary<Texture, Material> cachedGlowTextures = new();

    [SerializeField]
    private Material glowMaterial;
    //private bool isGlowing;

    private Color glowColor;

    private void Awake()
    {
        PrepareMaterialDictionaries();
        glowColor = glowMaterial.GetColor("_GlowColor");
    }

    private void PrepareMaterialDictionaries() //puts glowing and original materials in dictionaries at the beginning
    {
        foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
        {
            Material[] originalMaterials = renderer.materials;
            originalMaterialDictionary.Add(renderer, originalMaterials);

            Material[] newMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                if (originalMaterials[i].mainTexture == null)
                {
                    //Material mat;
                    if (!cachedGlowColors.TryGetValue(originalMaterials[i].color, out Material mat))
                    {
                        mat = new Material(glowMaterial);
                        //By default, Unity considers a color with the property name "_Color" to be the main color
                        mat.color = originalMaterials[i].color;
                        cachedGlowColors[mat.color] = mat;
                    }

                    newMaterials[i] = mat;
                    //if (!cachedGlowColors.TryGetValue(originalMaterials[i].color, out Material mat))
                    //{

                    //}

                    continue;
                } 
                  
                //Material mat2;
                if (!cachedGlowTextures.TryGetValue(originalMaterials[i].mainTexture, out Material mat2))
                {
                    mat2 = new Material(glowMaterial);
                    //By default, Unity considers a texture with the property name "_MainTex" to be the main texture
                    mat2.mainTexture = originalMaterials[i].mainTexture;
                    cachedGlowTextures[mat2.mainTexture] = mat2;
                }

                newMaterials[i] = mat2;

                //if (!cachedGlowTextures.TryGetValue(originalMaterials[i].mainTexture, out Material mat2))
                //{
                //    mat2 = new Material(glowMaterial);
                //    //By default, Unity considers a texture with the property name "_MainTex" to be the main texture
                //    mat2.mainTexture = originalMaterials[i].mainTexture;
                //    cachedGlowTextures[mat2.mainTexture] = mat2;
                //}

                //newMaterials[i] = mat2;
            }
            glowMaterialDictionary.Add(renderer, newMaterials);
        }
    }

    public void ResetGlowHighlight() //goes back to original color, not from dictionary though
    {
        //if (isGlowing == false)
        //    return;
        foreach (MeshRenderer renderer in glowMaterialDictionary.Keys)
        {
            foreach (Material item in glowMaterialDictionary[renderer])
            {
                item.SetColor("_GlowColor", glowColor);
            }
        }
    }

    //public void ToggleGlow(Color highlightColor) //toggles glow on or off for each item in dictionary, depending on its prevoius state
    //{
    //    if (!isGlowing)
    //    {
    //        ResetGlowHighlight();
    //        foreach (Renderer renderer in originalMaterialDictionary.Keys)
    //        {
    //            renderer.materials = glowMaterialDictionary[renderer];
    //            foreach (Material item in glowMaterialDictionary[renderer]) //sets the glow color for each item in dictionary
    //            {
    //                item.SetColor("_GlowColor", highlightColor);
    //            }

    //        }
    //    }
    //    else
    //    {
    //        foreach (Renderer renderer in originalMaterialDictionary.Keys)
    //        {
    //            renderer.materials = originalMaterialDictionary[renderer];
    //        }
    //    }
    //    isGlowing = !isGlowing;
    //}

    //public void ToggleGlow(bool state, Color highlightColor) //another method that takes a boolean to specifically turn off or on glow
    //{
    //    if (isGlowing == state)
    //        return;
    //    isGlowing = !state;
    //    ToggleGlow(highlightColor);
    //}

    public void EnableHighlight(Color highlightColor) //don't like 'Toggle' using this instead
    {
        ResetGlowHighlight();
        foreach (MeshRenderer renderer in originalMaterialDictionary.Keys)
        {
            renderer.materials = glowMaterialDictionary[renderer];
            foreach (Material item in glowMaterialDictionary[renderer]) //sets the glow color for each item in dictionary
            {
                item.SetColor("_GlowColor", highlightColor);
            }

        }
    }

    public void DisableHighlight()
    {
        foreach (MeshRenderer renderer in originalMaterialDictionary.Keys)
        {
            renderer.materials = originalMaterialDictionary[renderer];
        }
    }
}
