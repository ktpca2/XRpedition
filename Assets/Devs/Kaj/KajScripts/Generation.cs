using System.Collections.Generic;
using UnityEngine;

public class Generation : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject prefab;
        public int amount = 1;
    }

    [Header("Spawner Settings")]
    [SerializeField] private List<SpawnEntry> spawnList = new List<SpawnEntry>();

    [SerializeField] private GameObject spawnSurface;
    [SerializeField] private int totalLoops = 1;

    private List<(Vector3 pos, float radius)> placed = new List<(Vector3, float)>();

    private void Start()
    {
        if (!IsValid())
            return;

        var col = spawnSurface.GetComponent<Collider>();

        for (int loop = 0; loop < totalLoops; loop++)
            SpawnAll(col);
    }

    private bool IsValid()
    {
        if (spawnList == null || spawnList.Count == 0)
        {
            Debug.LogWarning($"{nameof(Generation)}: Spawn list is empty.");
            return false;
        }

        if (!spawnSurface)
        {
            Debug.LogWarning($"{nameof(Generation)}: No spawn surface assigned.");
            return false;
        }

        if (!spawnSurface.GetComponent<Collider>())
        {
            Debug.LogWarning($"{nameof(Generation)}: Spawn surface has no collider.");
            return false;
        }

        return true;
    }

    private void SpawnAll(Collider col)
    {
        foreach (var entry in spawnList)
        {
            if (!entry.prefab)
                continue;

            float radius = GetPrefabRadius(entry.prefab);

            for (int i = 0; i < entry.amount; i++)
            {
                Vector3 pos = FindValidPosition(radius, col);
                placed.Add((pos, radius));
                Instantiate(entry.prefab, pos, Quaternion.identity);
            }
        }
    }

    private float GetPrefabRadius(GameObject prefab)
    {
        var rend = prefab.GetComponentInChildren<Renderer>();
        if (!rend)
            return 0.5f;

        Vector3 size = rend.bounds.size;
        float maxAxis = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        return maxAxis * 0.5f;
    }

    private Vector3 FindValidPosition(float radius, Collider col)
    {
        const int attempts = 50;

        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = RandomPoint(col);

            bool overlaps = false;
            foreach (var p in placed)
            {
                float minDist = radius + p.radius;
                if (Vector3.Distance(candidate, p.pos) < minDist)
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
                return candidate;
        }

        return RandomPoint(col);
    }

    private Vector3 RandomPoint(Collider col)
    {
        var b = col.bounds;

        return new Vector3
        (
            Random.Range(b.min.x, b.max.x),
            b.max.y,
            Random.Range(b.min.z, b.max.z)
        );
    }
}
