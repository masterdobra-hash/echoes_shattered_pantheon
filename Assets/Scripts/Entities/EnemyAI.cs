using UnityEngine;
using Echoes.Core;

namespace Echoes.Entities
{
    public class EnemyAI : MonoBehaviour
    {
        public string  defId = "broken_hoplite";
        public Transform target;
        public float   hp, damage, speed, attackRange;
        public float   attackCooldown = 1.2f;
        private float  _atkTimer;

        private void Start()
        {
            var def = ContentRegistry.Instance.FindEnemy(defId);
            if (def != null)
            {
                hp = def.hp; damage = def.damage; speed = def.speed; attackRange = def.attackRange;
            }
            else { hp = 100; damage = 10; speed = 2; attackRange = 1.4f; }

            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
        }

        private void Update()
        {
            if (target == null || hp <= 0f) return;
            var d = Vector3.Distance(transform.position, target.position);
            if (d > attackRange)
            {
                var dir = (target.position - transform.position).normalized;
                transform.position += dir * speed * Time.deltaTime;
            }
            else
            {
                _atkTimer -= Time.deltaTime;
                if (_atkTimer <= 0f)
                {
                    var pc = target.GetComponent<PlayerController>();
                    if (pc != null) pc.TakeDamage(damage);
                    _atkTimer = attackCooldown;
                }
            }
        }

        public void TakeDamage(float dmg)
        {
            hp -= dmg;
            if (hp <= 0f)
            {
                Systems.LootSystem.RollDrop(transform.position, "common");
                Systems.AudioManager.Play("sfx_enemy_death");
                Destroy(gameObject);
            }
        }
    }
}
