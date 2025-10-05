using System.Collections.Generic;
using UnityEngine;

public class MaterialSwitcher : MonoBehaviour
{
    public Renderer targetRenderer;

    public enum Mode { BySlot, ByMaterialMatch, AllSlots }
    public Mode mode = Mode.ByMaterialMatch;

    [Header("BySlot")]
    public int[] slotIndices;                 // какие слоты заменить

    [Header("ByMaterialMatch")]
    public Material[] sourcesA;               // любые из этих заменить…

    [Header("Common")]
    public Material materialB;                // …на этот
    public bool useSharedMaterials = true;    // true: без инстансов

    // кэш для отката
    Material[] _original;
    readonly HashSet<int> _modifiedSlots = new HashSet<int>();

    void Awake()
    {
        if (!targetRenderer) targetRenderer = GetComponent<Renderer>();
    }

    public void SwitchToB()
    {
        if (!targetRenderer || !materialB) return;

        var mats = GetArray();
        if (_original == null) _original = (Material[])mats.Clone();
        _modifiedSlots.Clear();

        switch (mode)
        {
            case Mode.BySlot:
                foreach (var idx in slotIndices)
                {
                    if (idx < 0 || idx >= mats.Length) continue;
                    mats[idx] = materialB;
                    _modifiedSlots.Add(idx);
                }
                break;

            case Mode.ByMaterialMatch:
                for (int i = 0; i < mats.Length; i++)
                {
                    foreach (var src in sourcesA)
                    {
                        if (src && mats[i] == src)
                        {
                            mats[i] = materialB;
                            _modifiedSlots.Add(i);
                            break;
                        }
                    }
                }
                break;

            case Mode.AllSlots:
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = materialB;
                    _modifiedSlots.Add(i);
                }
                break;
        }

        SetArray(mats);
    }

    public void RestoreOriginals()
    {
        if (!targetRenderer || _original == null) return;

        var mats = GetArray();
        foreach (var idx in _modifiedSlots)
        {
            if (idx < 0 || idx >= mats.Length) continue;
            mats[idx] = _original[idx];
        }
        SetArray(mats);
        _modifiedSlots.Clear();
    }

    Material[] GetArray()
        => useSharedMaterials ? targetRenderer.sharedMaterials : targetRenderer.materials;

    void SetArray(Material[] arr)
    {
        if (useSharedMaterials) targetRenderer.sharedMaterials = arr;
        else                    targetRenderer.materials       = arr;
    }
}
