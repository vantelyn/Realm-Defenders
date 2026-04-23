using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visual del preview de construcción. Pinta todos los SpriteRenderer hijos
/// con tinte verde o rojo según si la colocación es válida.
/// </summary>
public class BuildGhost : MonoBehaviour
{
    [SerializeField] private Color validColor = new Color(0.4f, 1f, 0.4f, 0.6f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();

    private void Awake()
    {
        GetComponentsInChildren(true, renderers);
    }

    public void SetValid(bool valid)
    {
        Color c = valid ? validColor : invalidColor;
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null) renderers[i].color = c;
        }
    }
}