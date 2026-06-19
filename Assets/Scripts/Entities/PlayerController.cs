using System.Collections.Generic;
using UnityEngine;
using Echoes.Core;

namespace Echoes.Entities
{
    public class PlayerController : MonoBehaviour
    {
        public float maxHp   = 500f, hp;
        public float maxMana = 200f, mana;
        public float moveSpeed = 5.0f;
        public float manaRegen = 6f, hpRegen = 1f;

        private Vector3 _dest;
        private bool _hasDest;
        private readonly Dictionary<string,float> _cd = new Dictionary<string,float>();

        private void Awake() { hp = maxHp; mana = maxMana; _dest = transform.position; }

        private void Update()
        {
            if (_hasDest)
            {
                var flat = new Vector3(_dest.x, transform.position.y, _dest.z);
                transform.position = Vector3.MoveTowards(transform.position, flat, moveSpeed * Time.deltaTime);
                if ((transform.position - flat).sqrMagnitude < 0.04f) _hasDest = false;
            }
            mana = Mathf.Min(maxMana, mana + manaRegen * Time.deltaTime);
            hp   = Mathf.Min(maxHp,   hp   + hpRegen   * Time.deltaTime);
            var keys = new List<string>(_cd.Keys);
            foreach (var k in keys) _cd[k] = Mathf.Max(0f, _cd[k] - Time.deltaTime);
        }

        public void MoveTo(Vector3 p) { _dest = p; _hasDest = true; }
        public float CooldownOf(string id) { float v; return _cd.TryGetValue(id, out v) ? v : 0f; }

        public bool TryCast(string skillId)
        {
            var def = ContentRegistry.Instance.FindSkill(skillId);
            if (def == null) { Debug.Log("[Player] skill not found: " + skillId); return false; }
            if (CooldownOf(skillId) > 0f) return false;
            if (mana < def.manaCost)      return false;
            mana -= def.manaCost;
            _cd[skillId] = def.cooldown;
            Systems.CombatSystem.Cast(def, transform);
            return true;
        }

        public void TakeDamage(float dmg)
        {
            hp = Mathf.Max(0f, hp - dmg);
        }
    }
}
