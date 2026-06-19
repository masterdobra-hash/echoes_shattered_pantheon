using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// Bullet-proof material creator. Tries Standard (BiRP) first, falls back
    /// through every known shader name until one resolves. NEVER returns null.
    /// </summary>
    public static class MaterialFactory
    {
        private static Shader _cached;
        private static string _cachedName = "<none>";

        public static string CurrentShaderName => _cachedName;

        public static Shader Resolve()
        {
            if (_cached != null) return _cached;
            string[] candidates = {
                "Standard",
                "Mobile/Diffuse",
                "Legacy Shaders/Diffuse",
                "Legacy Shaders/VertexLit",
                "Unlit/Color",
                "Sprites/Default",
                "Hidden/InternalErrorShader"
            };
            foreach (var n in candidates)
            {
                var s = Shader.Find(n);
                if (s != null) { _cached = s; _cachedName = n; break; }
            }
            Debug.Log("[Echoes] MaterialFactory shader = " + _cachedName);
            return _cached;
        }

        public static Material Make(Color color)
        {
            var sh = Resolve();
            var m = new Material(sh != null ? sh : Shader.Find("Hidden/InternalErrorShader"));
            if (m.HasProperty("_Color"))      m.SetColor("_Color", color);
            if (m.HasProperty("_BaseColor"))  m.SetColor("_BaseColor", color);
            if (m.HasProperty("_MainColor"))  m.SetColor("_MainColor", color);
            return m;
        }
    }
}
