using UnityEngine;
using UnityEngine.UI;
using Echoes.Entities;

namespace Echoes.UI
{
    /// <summary>
    /// Minimal Diablo-style HUD: HP/Mana bars + skill buttons (T/I/A keys + on-screen).
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        public PlayerController player;
        private Texture2D _whiteTex;
        private GUIStyle  _btnStyle;

        private void Start()
        {
            _whiteTex = new Texture2D(1,1); _whiteTex.SetPixel(0,0,Color.white); _whiteTex.Apply();
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.GetComponent<PlayerController>();
            }
        }

        private void OnGUI()
        {
            if (_btnStyle == null)
            {
                _btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
            }
            if (player == null)
            {
                GUI.Label(new Rect(20,20,400,40), "Loading...");
                return;
            }
            // HP orb
            DrawBar(new Rect(20, Screen.height - 80, 240, 26), player.hp / player.maxHp,
                    new Color(0.65f, 0.05f, 0.05f), $"HP {(int)player.hp}/{(int)player.maxHp}");
            // Mana orb
            DrawBar(new Rect(Screen.width - 260, Screen.height - 80, 240, 26), player.mana / player.maxMana,
                    new Color(0.10f, 0.20f, 0.85f), $"MP {(int)player.mana}/{(int)player.maxMana}");

            // Skill buttons
            float bw = 80, bh = 80, gap = 10;
            float x0 = Screen.width / 2 - (bw * 3 + gap * 2) / 2;
            float y  = Screen.height - bh - 10;
            DrawSkillButton(new Rect(x0,              y, bw, bh), "Slam",  "titan_slam");
            DrawSkillButton(new Rect(x0 + bw + gap,   y, bw, bh), "Inferno","inferno_strike");
            DrawSkillButton(new Rect(x0 + 2*(bw+gap), y, bw, bh), "Aegis", "aegis_ward");
        }

        private void DrawBar(Rect rect, float pct, Color color, string label)
        {
            var bg = GUI.color; GUI.color = new Color(0,0,0,0.7f);
            GUI.DrawTexture(rect, _whiteTex);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x+2, rect.y+2, (rect.width-4)*Mathf.Clamp01(pct), rect.height-4), _whiteTex);
            GUI.color = Color.white;
            GUI.Label(new Rect(rect.x+8, rect.y+3, rect.width, rect.height), label);
            GUI.color = bg;
        }

        private void DrawSkillButton(Rect r, string label, string skillId)
        {
            float cd = player.CooldownOf(skillId);
            string text = cd > 0f ? $"{label}\n{cd:F1}s" : label;
            if (GUI.Button(r, text, _btnStyle)) player.TryCast(skillId);
        }
    }
}
