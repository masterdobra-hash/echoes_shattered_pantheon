using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Core
{
    [System.Serializable] public class SkillDef {
        public string id, name, type;
        public float damage, cooldown, manaCost, radius;
    }
    [System.Serializable] public class PassiveDef { public string id, name, type; public float value; }
    [System.Serializable] public class SkillsFile { public List<SkillDef> active; public List<PassiveDef> passive; }
    [System.Serializable] public class ShardDef { public string id, name, bonus, rarity; public float value; }
    [System.Serializable] public class ShardsFile { public List<ShardDef> shards; }
    [System.Serializable] public class EnemyDef {
        public string id, name, lootTable;
        public float hp, damage, speed, attackRange;
        public int xp;
    }
    [System.Serializable] public class BossPhase { public string id; public float hpThreshold, telegraph; public List<string> abilities; }
    [System.Serializable] public class BossDef {
        public string id, name, lootTable;
        public float hp, damage, speed;
        public List<BossPhase> phases;
    }
    [System.Serializable] public class EnemiesFile { public List<EnemyDef> enemies; public BossDef boss; }
    [System.Serializable] public class ItemStats { public float maxHp, damageReduction, damage, critChance, maxMana, thunderDamage; }
    [System.Serializable] public class ItemDef { public string id, name, slot, rarity; public ItemStats stats; }
    [System.Serializable] public class ItemsFile { public List<ItemDef> items; }

    public class ContentRegistry
    {
        private static ContentRegistry _i;
        public static ContentRegistry Instance { get { if (_i == null) _i = new ContentRegistry(); return _i; } }

        public List<SkillDef>   Skills   = new List<SkillDef>();
        public List<PassiveDef> Passives = new List<PassiveDef>();
        public List<ShardDef>   Shards   = new List<ShardDef>();
        public List<EnemyDef>   Enemies  = new List<EnemyDef>();
        public BossDef          Boss;
        public List<ItemDef>    Items    = new List<ItemDef>();

        public void LoadAllSync()
        {
            try {
                var s = Resources.Load<TextAsset>("Data/skills");
                if (s != null) {
                    var f = JsonUtility.FromJson<SkillsFile>(s.text);
                    if (f != null) { Skills = f.active ?? Skills; Passives = f.passive ?? Passives; }
                }
                var sh = Resources.Load<TextAsset>("Data/shards");
                if (sh != null) { var f = JsonUtility.FromJson<ShardsFile>(sh.text); if (f != null) Shards = f.shards ?? Shards; }
                var e = Resources.Load<TextAsset>("Data/enemies");
                if (e != null) {
                    var f = JsonUtility.FromJson<EnemiesFile>(e.text);
                    if (f != null) { Enemies = f.enemies ?? Enemies; Boss = f.boss; }
                }
                var it = Resources.Load<TextAsset>("Data/items");
                if (it != null) { var f = JsonUtility.FromJson<ItemsFile>(it.text); if (f != null) Items = f.items ?? Items; }
                Debug.Log("[ContentRegistry] skills=" + Skills.Count + " enemies=" + Enemies.Count + " items=" + Items.Count);
            } catch (System.Exception ex) {
                Debug.LogError("[ContentRegistry] load failed: " + ex);
            }
        }

        public SkillDef FindSkill(string id) { foreach (var s in Skills) if (s.id == id) return s; return null; }
        public EnemyDef FindEnemy(string id) { foreach (var e in Enemies) if (e.id == id) return e; return null; }
    }
}
