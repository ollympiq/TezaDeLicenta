using System.Collections;
using UnityEngine;

public class LobbyDisableMoveRangeMesh : MonoBehaviour
{
    [SerializeField] private bool disableGameObject = true;

    private IEnumerator Start()
    {
        yield return null;

        GameObject player = PlayerRuntimeRegistry.ResolvePlayerRoot();

        if (player == null)
        {
            Debug.LogWarning("LobbyDisableMoveRangeMesh: nu a fost gasit playerul activ.");
            yield break;
        }

        MoveRangeGridVisualizer[] visualizers =
            player.GetComponentsInChildren<MoveRangeGridVisualizer>(true);

        for (int i = 0; i < visualizers.Length; i++)
        {
            if (visualizers[i] == null)
                continue;

            visualizers[i].enabled = false;

            MeshRenderer meshRenderer = visualizers[i].GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            MeshFilter meshFilter = visualizers[i].GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
                meshFilter.sharedMesh.Clear();

            if (disableGameObject)
                visualizers[i].gameObject.SetActive(false);
        }
    }
}