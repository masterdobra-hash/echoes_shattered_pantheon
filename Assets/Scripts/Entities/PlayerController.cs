using UnityEngine;
using Echoes.Core;

namespace Echoes.Entities
{
    /// <summary>
    /// Titan Warrior — tap-to-move + cast active skills.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public float maxHp   = 500f, hp;
        public float maxMana = 200f, mana;
        public float moveSpeed = 4.5f;
        public float manaRegen = 4f, hpRegen = 0.5f;

        private Vector3 _dest;
        private bool    _hasDest;

        private readonly System.Collections.Generic.Dictionary<string,float> _cooldowns
            = new System.Collections.Generic.Dictionary<string,float>();

        private void Awake() { hp = maxHp; mana = maxMana; _dest = transform.position; }

        private void Update()
        {
            // Move
            if (_hasDest)
            {
                var flat = new Vector3(_dest.x, transform.position.y, _dest.z);
                transform.position = Vector3.MoveTowards(transform.position, flat, moveSpeed * Time.deltaTime);
                if ((transform.position - flat).sqrMagnitude < 0.04f) _hasDest = false;
            }
            // Regen
            mana = Mathf.Min(maxMana, mana + manaRegen * Time.deltaTime);
            hp   = Mathf.Min(maxHp,   hp   + hpRegen   * Time.deltaTime);
            // Cooldown tick
            var keys = new System.Collections.Generic.List<string>(_cooldowns.Keys);
            foreach (var k in keys) _cooldowns[k] = Mathf.Max(0f, _cooldowns[k] - Time.deltaTime);
        }

        public void MoveTo(Vector3 p) { _dest = p; _hasDest = true; }

        public float CooldownOf(string id) => _cooldowns.TryGetValue(id, out var v) ? v : 0f;

        public bool TryCast(string skillId)
        {
            var def = ContentRegistry.Instance.FindSkill(skillId);
            if (def == null) return false;
            if (CooldownOf(skillId) > 0f) return false;
            if (mana < def.manaCost)      return false;

            mana -= def.manaCost;
            _cooldowns[skillId] = def.cooldown;
            Systems.CombatSystem.Cast(def, transform);
            Debug.Log($"[Echoes] cast {def.name} dmg={def.damage} cd={def.cooldown}");
            return true;
        }

        public void TakeDamage(float dmg)
        {
            hp = Mathf.Max(0f, hp - dmg);
            if (hp <= 0f) Debug.Log("[Echoes] Player died");
        }
    }
}
