using UnityEngine;

public static class LayerMaskCheck 
{
    public static bool ContainsLayer(LayerMask layerMask, int layer) =>
        (layerMask.value & 1 << layer) > 0;
}
