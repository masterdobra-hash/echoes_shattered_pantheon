using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Core
{
    [System.Serializable] public class SkillDef
    {
        public string id, name, type;
        public float damage, cooldown, manaCost, radius, duration;
        public float burnDps, shieldHp, pushForce, atkBonus, healPercent, hpCost;
        public int jumps;
        public float tickDps;
        public string value;
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

    [System.Serializable] public class ItemStats {
        public float maxHp, damageReduction, damage, critChance, maxMana, thunderDamage;
    }
    [System.Serializable] public class ItemDef { public string id, name, slot, rarity; public ItemStats stats; }
    [System.Serializable] public class ItemsFile { public List<ItemDef> items; }

    /// <summary>
    /// Loads content JSONs from Resources/Data once at startup.
    /// </summary>
    public class ContentRegistry
    {
        private static ContentRegistry _i;
        public static ContentRegistry Instance => _i ?? (_i = new ContentRegistry());

        public List<SkillDef>   Skills   { get; private set; } = new List<SkillDef>();
        public List<PassiveDef> Passives { get; private set; } = new List<PassiveDef>();
        public List<ShardDef>   Shards   { get; private set; } = new List<ShardDef>();
        public List<EnemyDef>   Enemies  { get; private set; } = new List<EnemyDef>();
        public BossDef          Boss     { get; private set; }
        public List<ItemDef>    Items    { get; private set; } = new List<ItemDef>();

        public IEnumerator LoadAllAsync()
        {
            yield return null;
            try
            {
                var s = Resources.Load<TextAsset>("Data/skills");
                if (s != null) {
                    var f = JsonUtility.FromJson<SkillsFile>(s.text);
                    Skills = f.active ?? Skills;
                    Passives = f.passive ?? Passives;
                }
                var sh = Resources.Load<TextAsset>("Data/shards");
                if (sh != null) Shards = JsonUtility.FromJson<ShardsFile>(sh.text).shards ?? Shards;

                var e = Resources.Load<TextAsset>("Data/enemies");
                if (e != null) {
                    var f = JsonUtility.FromJson<EnemiesFile>(e.text);
                    Enemies = f.enemies ?? Enemies;
                    Boss = f.boss;
                }
                var it = Resources.Load<TextAsset>("Data/items");
                if (it != null) Items = JsonUtility.FromJson<ItemsFile>(it.text).items ?? Items;
            }
            catch (System.Exception ex) { Debug.LogError($"[Echoes] ContentRegistry load failed: {ex}"); }
        }

        public SkillDef FindSkill(string id) => Skills.Find(x => x.id == id);
        public EnemyDef FindEnemy(string id) => Enemies.Find(x => x.id == id);
    }
}
