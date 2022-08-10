using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionHighlight : MonoBehaviour
{
    Dictionary<Renderer, Material[]> glowMaterialDictionary = new();
    Dictionary<Renderer, Material[]> originalMaterialDictionary = new();
    Dictionary<Color, Material> cachedGlowMaterials = new();

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
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            Material[] originalMaterials = renderer.materials;
            originalMaterialDictionary.Add(renderer, originalMaterials);

            Material[] newMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < renderer.materials.Length; i++)
            {
                //Material mat;
                if (!cachedGlowMaterials.TryGetValue(originalMaterials[i].color, out Material mat))
                {
                    mat = new Material(glowMaterial);
                    //By default, Unity considers a color with the property name "_Color" to be the main color
                    mat.color = originalMaterials[i].color;
                    cachedGlowMaterials[mat.color] = mat;
                }
                newMaterials[i] = mat;
            }
            glowMaterialDictionary.Add(renderer, newMaterials);
        }
    }

    //internal void HighlightValidPath()
    //{
    //    //if (isGlowing == false) //checks if it's highlighted first, won't work otherwise //turned off for AStar method
    //    //    return;
    //    foreach (Renderer renderer in glowMaterialDictionary.Keys)
    //    {
    //        foreach (Material item in glowMaterialDictionary[renderer]) //sets the glow color for each item in dictionary
    //        {
    //            item.SetColor("_GlowColor", validSpaceColor);
    //        }
    //    }
    //}

    public void ResetGlowHighlight() //goes back to original color, not from dictionary though
    {
        //if (isGlowing == false)
        //    return;
        foreach (Renderer renderer in glowMaterialDictionary.Keys)
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
        foreach (Renderer renderer in originalMaterialDictionary.Keys)
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
        foreach (Renderer renderer in originalMaterialDictionary.Keys)
        {
            renderer.materials = originalMaterialDictionary[renderer];
        }
    }
}
