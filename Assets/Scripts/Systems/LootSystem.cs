using UnityEngine;
using Echoes.Core;

namespace Echoes.Systems
{
    public static class LootSystem
    {
        public static void RollDrop(Vector3 position, string table)
        {
            AudioManager.Play("sfx_loot_drop");
            // Lightweight gold-cube placeholder (a primitive, no prefab dependency)
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.position = position + Vector3.up * 0.3f;
            marker.transform.localScale = Vector3.one * 0.4f;
            marker.tag = "Loot";
            var r = marker.GetComponent<Renderer>();
            if (r != null) r.material.color = table == "boss_legendary" ? new Color(1f, 0.8f, 0.2f) : new Color(0.6f, 0.6f, 0.6f);
            Object.Destroy(marker.GetComponent<Collider>());
            Debug.Log($"[Echoes] LootSystem drop @{position} table={table}");
        }
    }
}
