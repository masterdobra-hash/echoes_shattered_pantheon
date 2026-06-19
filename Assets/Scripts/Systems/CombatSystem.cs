using UnityEngine;
using Echoes.Core;
using Echoes.Entities;

namespace Echoes.Systems
{
    public static class CombatSystem
    {
        public static void Cast(SkillDef skill, Transform caster)
        {
            if (skill == null || caster == null) return;
            float radius = skill.radius > 0f ? skill.radius : 3f;
            var hits = Physics.OverlapSphere(caster.position, radius);
            if (hits == null) return;
            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i];
                if (c == null) continue;
                var e = c.GetComponent<EnemyAI>();
                if (e != null) { e.TakeDamage(skill.damage); continue; }
                var b = c.GetComponent<BossAI>();
                if (b != null) b.TakeDamage(skill.damage);
            }
        }
    }
}
