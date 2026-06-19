using UnityEngine;
using UnityEngine.UI;

namespace Echoes.Core
{
    public enum AnchorPreset { TopLeft, TopRight, BottomLeft, BottomRight, BottomCenter, Center }

    /// <summary>Helpers that build UGUI elements with sensible anchors.</summary>
    public static class HudFactory
    {
        private static void ApplyAnchor(RectTransform rt, AnchorPreset p, Vector2 pos, Vector2 size)
        {
            Vector2 amin = Vector2.zero, amax = Vector2.zero, pivot = Vector2.zero;
            switch (p)
            {
                case AnchorPreset.TopLeft:     amin = new Vector2(0,1); amax = new Vector2(0,1); pivot = new Vector2(0,1); break;
                case AnchorPreset.TopRight:    amin = new Vector2(1,1); amax = new Vector2(1,1); pivot = new Vector2(1,1); break;
                case AnchorPreset.BottomLeft:  amin = new Vector2(0,0); amax = new Vector2(0,0); pivot = new Vector2(0,0); break;
                case AnchorPreset.BottomRight: amin = new Vector2(1,0); amax = new Vector2(1,0); pivot = new Vector2(1,0); break;
                case AnchorPreset.BottomCenter:amin = new Vector2(0.5f,0); amax = new Vector2(0.5f,0); pivot = new Vector2(0.5f,0); break;
                case AnchorPreset.Center:      amin = new Vector2(0.5f,0.5f); amax = new Vector2(0.5f,0.5f); pivot = new Vector2(0.5f,0.5f); break;
            }
            rt.anchorMin = amin; rt.anchorMax = amax; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        public static Image Panel(Transform parent, string name, Vector2 pos, Vector2 size,
                                  AnchorPreset preset, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            ApplyAnchor(go.GetComponent<RectTransform>(), preset, pos, size);
            return img;
        }

        public static Image Image(Transform parent, string name, Vector2 pos, Vector2 size,
                                  AnchorPreset preset, Color color)
        {
            var img = Panel(parent, name, pos, size, preset, color);
            img.type = UnityEngine.UI.Image.Type.Filled;
            img.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            img.fillAmount = 1f;
            return img;
        }

        public static Text Text(Transform parent, string name, Vector2 pos, Vector2 size,
                                AnchorPreset preset, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = "";
            t.color = color;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleLeft;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            ApplyAnchor(go.GetComponent<RectTransform>(), preset, pos, size);
            return t;
        }

        public static Button Button(Transform parent, string name, Vector2 pos, Vector2 size,
                                    AnchorPreset preset, string label)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var bgImg = go.AddComponent<Image>();
            bgImg.color = new Color(0.18f, 0.18f, 0.22f, 0.95f);
            var btn = go.AddComponent<Button>();
            ApplyAnchor(go.GetComponent<RectTransform>(), preset, pos, size);

            var txt = Text(go.transform, "Label", Vector2.zero, size, AnchorPreset.Center, 22, Color.white);
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            return btn;
        }
    }
}
