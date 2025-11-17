using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnEntry
{
    public GameObject Prefab;
    public int Amount = 1;

    [Header("Spawn Restrictions")]
    public float MinDistanceFromPlayer = 0f;
}

public class Generation : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private List<SpawnEntry> _SpawnList = new List<SpawnEntry>();
    [SerializeField] private GameObject _SpawnSurface;
    [SerializeField] private int _TotalLoops = 1;

    [Header("Player Reference")]
    [SerializeField] private Transform player;

    private List<(Vector3 pos, float radius)> placed = new List<(Vector3, float)>();

    private void Start()
    {
        if (!IsValid())
            return;

        var col = _SpawnSurface.GetComponent<Collider>();

        for (int loop = 0; loop < _TotalLoops; loop++)
            SpawnAll(col);
    }

    private bool IsValid()
    {
        if (_SpawnList == null || _SpawnList.Count == 0)
        {
            Debug.LogWarning($"{nameof(Generation)}: Spawn list is empty.");
            return false;
        }

        if (!_SpawnSurface)
        {
            Debug.LogWarning($"{nameof(Generation)}: No spawn surface assigned.");
            return false;
        }

        if (!_SpawnSurface.GetComponent<Collider>())
        {
            Debug.LogWarning($"{nameof(Generation)}: Spawn surface has no collider.");
            return false;
        }

        return true;
    }

    private void SpawnAll(Collider col)
    {
        foreach (var entry in _SpawnList)
        {
            if (!entry.Prefab)
                continue;

            float radius = GetPrefabRadius(entry.Prefab);

            for (int i = 0; i < entry.Amount; i++)
            {
                Vector3 pos = FindValidPosition(radius, col, entry.MinDistanceFromPlayer);
                placed.Add((pos, radius));
                Instantiate(entry.Prefab, pos, Quaternion.identity);
            }
        }
    }

    /// <summary>
    /// Gets a radius using Collider first, then Renderer fallback.
    /// Ensures all prefabs (tree/rock/etc.) get correct size.
    /// </summary>
    private float GetPrefabRadius(GameObject prefab)
    {
        // Prefer COLLIDER (best for trees/rocks)
        var col = prefab.GetComponentInChildren<Collider>();
        if (col)
        {
            Vector3 ext = col.bounds.extents;
            return Mathf.Max(ext.x, ext.z);
        }

        // Fallback to Renderer
        var rend = prefab.GetComponentInChildren<Renderer>();
        if (rend)
        {
            Vector3 ext = rend.bounds.extents;
            return Mathf.Max(ext.x, ext.z);
        }

        // Safe fallback
        return 1f;
    }

    private Vector3 FindValidPosition(float radius, Collider col, float minPlayerDist)
    {
        const int attempts = 50;

        for (int i = 0; i < attempts; i++)
        {
            Vector3 candidate = RandomPointOnGround(col);

            // Horizontal distance check (XZ only)
            if (player && minPlayerDist > 0f)
            {
                Vector2 p = new Vector2(player.position.x, player.position.z);
                Vector2 c = new Vector2(candidate.x, candidate.z);

                if (Vector2.Distance(c, p) < minPlayerDist)
                    continue;
            }

            bool overlaps = false;
            foreach (var p in placed)
            {
                float minDist = radius + p.radius;

                // ONLY check in XZ
                Vector2 a2d = new Vector2(candidate.x, candidate.z);
                Vector2 b2d = new Vector2(p.pos.x, p.pos.z);

                if (Vector2.Distance(a2d, b2d) < minDist)
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
                return candidate;
        }

        // fallback
        return RandomPointOnGround(col);
    }

    /// <summary>
    /// Picks a random XZ point and raycasts down to the actual ground surface.
    /// Fixes incorrect spawn height.
    /// </summary>
    private Vector3 RandomPointOnGround(Collider col)
    {
        var b = col.bounds;

        float x = Random.Range(b.min.x, b.max.x);
        float z = Random.Range(b.min.z, b.max.z);

        // position ABOVE the surface
        Vector3 origin = new Vector3(x, b.max.y + 5f, z);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 50f))
            return hit.point;

        // fallback (surface center)
        return new Vector3(x, b.center.y, z);
    }
}
