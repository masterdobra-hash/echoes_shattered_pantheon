using System.Collections.Generic;
using UnityEngine;

namespace Echoes.Core
{
    /// <summary>
    /// Pre-flight validation. Runs before SceneSetup. If any check fails,
    /// a red diagnostic UGUI panel is shown so the user always sees a reason.
    /// </summary>
    public static class SelfTest
    {
        public static List<string> Errors = new List<string>();

        public static bool Run()
        {
            Errors.Clear();

            // 1. Shader available?
            var sh = MaterialFactory.Resolve();
            if (sh == null) Errors.Add("No shader resolved (tried Standard/Mobile/Unlit/...)");
            else Debug.Log("[SelfTest] shader=" + MaterialFactory.CurrentShaderName);

            // 2. ContentRegistry can load JSON?
            try {
                var ts = Resources.Load<TextAsset>("Data/skills");
                if (ts == null) Errors.Add("Resources/Data/skills.json not found");
                else Debug.Log("[SelfTest] skills.json loaded len=" + ts.text.Length);
            } catch (System.Exception ex) { Errors.Add("Resources load threw: " + ex.Message); }

            // 3. Camera.main? (not required pre-scene-build but logged)
            Debug.Log("[SelfTest] Camera.main = " + (Camera.main != null ? "yes" : "no"));

            return Errors.Count == 0;
        }
    }
}
