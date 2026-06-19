using System.Collections;
using UnityEngine;
using Echoes.Core;

namespace Echoes.Entities
{
    public class BossAI : MonoBehaviour
    {
        public Transform target;
        public float hp, maxHp, damage, speed;
        public int currentPhase = 0;
        private float _abilityTimer = 2f;
        private BossDef _def;

        private void Start()
        {
            _def = ContentRegistry.Instance.Boss;
            if (_def != null) { maxHp = _def.hp; hp = maxHp; damage = _def.damage; speed = _def.speed; }
            else              { maxHp = 1400; hp = maxHp; damage = 35; speed = 2.4f; }
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        private void Update()
        {
            if (target == null || hp <= 0f) return;
            float r = hp / maxHp;
            if      (r <= 0.33f && currentPhase < 2) { currentPhase = 2; }
            else if (r <= 0.66f && currentPhase < 1) { currentPhase = 1; }
            var d = Vector3.Distance(transform.position, target.position);
            if (d > 2.0f)
            {
                var dir = (target.position - transform.position).normalized;
                float spd = currentPhase == 2 ? speed * 1.4f : speed;
                transform.position += dir * spd * Time.deltaTime;
            }
            _abilityTimer -= Time.deltaTime;
            if (_abilityTimer <= 0f)
            {
                StartCoroutine(TelegraphedAttack());
                _abilityTimer = currentPhase == 2 ? 2.0f : 3.5f;
            }
        }

        private IEnumerator TelegraphedAttack()
        {
            float warn = (_def != null && _def.phases != null && currentPhase < _def.phases.Count)
                         ? _def.phases[currentPhase].telegraph : 0.7f;
            yield return new WaitForSeconds(warn);
            if (target == null || hp <= 0f) yield break;
            if (Vector3.Distance(transform.position, target.position) < 3.5f)
            {
                float dmg = damage * (currentPhase == 2 ? 1.5f : 1.0f);
                var pc = target.GetComponent<PlayerController>();
                if (pc != null) pc.TakeDamage(dmg);
            }
        }

        public void TakeDamage(float dmg)
        {
            hp -= dmg;
            if (hp <= 0f) Destroy(gameObject);
        }
    }
}
