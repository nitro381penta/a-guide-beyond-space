using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlefKeplerVisibilityFader : MonoBehaviour
{
    private class MaterialEntry
    {
        public Material material;
        public Color originalColor;
        public string colorProperty;
    }

    private readonly List<MaterialEntry> entries = new();

    private void Awake()
    {
        CacheMaterials();
    }

    private void CacheMaterials()
    {
        entries.Clear();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            foreach (Material mat in materials)
            {
                if (mat == null) continue;

                if (mat.HasProperty("_BaseColor"))
                {
                    entries.Add(new MaterialEntry
                    {
                        material = mat,
                        originalColor = mat.GetColor("_BaseColor"),
                        colorProperty = "_BaseColor"
                    });
                }
                else if (mat.HasProperty("_Color"))
                {
                    entries.Add(new MaterialEntry
                    {
                        material = mat,
                        originalColor = mat.GetColor("_Color"),
                        colorProperty = "_Color"
                    });
                }
            }
        }
    }

    public void SetVisibilityImmediate(float alpha01)
    {
        alpha01 = Mathf.Clamp01(alpha01);

        foreach (MaterialEntry entry in entries)
        {
            Color c = entry.originalColor;
            c.a = alpha01;
            entry.material.SetColor(entry.colorProperty, c);
        }
    }

    public Coroutine FadeIn(MonoBehaviour runner, float duration)
    {
        return runner.StartCoroutine(FadeRoutine(0f, 1f, duration));
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        float t = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            SetVisibilityImmediate(a);
            yield return null;
        }

        SetVisibilityImmediate(to);
    }
}