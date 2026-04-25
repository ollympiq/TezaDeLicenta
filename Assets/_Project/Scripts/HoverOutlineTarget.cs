using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class HoverOutlineTarget : MonoBehaviour
{
    [Header("Outline")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private float outlineScale = 1.04f;

    [Header("Source Renderers")]
    [SerializeField] private Renderer[] sourceRenderers;

    private readonly List<GameObject> outlineObjects = new List<GameObject>();
    private bool built;

    private void Awake()
    {
        BuildIfNeeded();
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        BuildIfNeeded();

        for (int i = 0; i < outlineObjects.Count; i++)
        {
            if (outlineObjects[i] != null)
                outlineObjects[i].SetActive(highlighted);
        }
    }

    private void BuildIfNeeded()
    {
        if (built)
            return;

        built = true;

        if (outlineMaterial == null)
        {
            Debug.LogWarning($"[{name}] HoverOutlineTarget nu are outline material setat.");
            return;
        }

        if (sourceRenderers == null || sourceRenderers.Length == 0)
            sourceRenderers = GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < sourceRenderers.Length; i++)
        {
            Renderer source = sourceRenderers[i];
            if (source == null)
                continue;

            if (source.GetComponent<HoverOutlineProxyMarker>() != null)
                continue;

            if (source is SkinnedMeshRenderer skinned)
            {
                if (skinned.sharedMesh == null)
                    continue;

                GameObject proxy = new GameObject(skinned.name + "_OutlineProxy");
                proxy.layer = LayerMask.NameToLayer("Ignore Raycast");
                proxy.transform.SetParent(skinned.transform, false);
                proxy.transform.localPosition = Vector3.zero;
                proxy.transform.localRotation = Quaternion.identity;
                proxy.transform.localScale = Vector3.one * outlineScale;

                proxy.AddComponent<HoverOutlineProxyMarker>();

                SkinnedMeshRenderer proxyRenderer = proxy.AddComponent<SkinnedMeshRenderer>();
                proxyRenderer.sharedMesh = skinned.sharedMesh;
                proxyRenderer.rootBone = skinned.rootBone;
                proxyRenderer.bones = skinned.bones;
                proxyRenderer.localBounds = skinned.localBounds;
                proxyRenderer.updateWhenOffscreen = true;
                proxyRenderer.shadowCastingMode = ShadowCastingMode.Off;
                proxyRenderer.receiveShadows = false;
                proxyRenderer.lightProbeUsage = LightProbeUsage.Off;
                proxyRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                proxyRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                proxyRenderer.sharedMaterial = outlineMaterial;

                proxy.SetActive(false);
                outlineObjects.Add(proxy);
            }
            else if (source is MeshRenderer meshRenderer)
            {
                MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;

                GameObject proxy = new GameObject(meshRenderer.name + "_OutlineProxy");
                proxy.layer = LayerMask.NameToLayer("Ignore Raycast");
                proxy.transform.SetParent(meshRenderer.transform, false);
                proxy.transform.localPosition = Vector3.zero;
                proxy.transform.localRotation = Quaternion.identity;
                proxy.transform.localScale = Vector3.one * outlineScale;

                proxy.AddComponent<HoverOutlineProxyMarker>();

                MeshFilter proxyFilter = proxy.AddComponent<MeshFilter>();
                proxyFilter.sharedMesh = filter.sharedMesh;

                MeshRenderer proxyRenderer = proxy.AddComponent<MeshRenderer>();
                proxyRenderer.shadowCastingMode = ShadowCastingMode.Off;
                proxyRenderer.receiveShadows = false;
                proxyRenderer.lightProbeUsage = LightProbeUsage.Off;
                proxyRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                proxyRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                proxyRenderer.sharedMaterial = outlineMaterial;

                proxy.SetActive(false);
                outlineObjects.Add(proxy);
            }
        }
    }
}

