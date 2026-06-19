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
            AudioManager.Play(SfxFor(skill.id));

            float radius = skill.radius > 0f ? skill.radius : 2.5f;
            var hits = Physics.OverlapSphere(caster.position, radius);
            foreach (var c in hits)
            {
                if (c == null) continue;
                var e = c.GetComponent<EnemyAI>();
                if (e != null) { e.TakeDamage(skill.damage); continue; }
                var b = c.GetComponent<BossAI>();
                if (b != null) { b.TakeDamage(skill.damage); }
            }
        }

        private static string SfxFor(string skillId)
        {
            switch (skillId)
            {
                case "titan_slam":     return "sfx_titan_slam";
                case "inferno_strike": return "sfx_inferno_strike";
                case "aegis_ward":     return "sfx_aegis_ward";
                default:               return "sfx_titan_slam";
            }
        }
    }
}
