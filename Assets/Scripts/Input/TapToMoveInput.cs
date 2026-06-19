using UnityEngine;

namespace Echoes.Input
{
    /// <summary>
    /// Tap/click to move. Uses legacy UnityEngine.Input (always available),
    /// no dependency on the new Input System package.
    /// </summary>
    public class TapToMoveInput : MonoBehaviour
    {
        public Echoes.Entities.PlayerController player;
        public Camera cam;

        private void Awake()
        {
            if (player == null) player = GetComponent<Echoes.Entities.PlayerController>();
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (player == null) return;
            if (cam == null) { cam = Camera.main; if (cam == null) return; }

            bool tapped = false;
            Vector3 screen = Vector3.zero;
            if (UnityEngine.Input.GetMouseButtonDown(0))
            { tapped = true; screen = UnityEngine.Input.mousePosition; }
            else if (UnityEngine.Input.touchCount > 0)
            {
                var t = UnityEngine.Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) { tapped = true; screen = new Vector3(t.position.x, t.position.y, 0f); }
            }
            if (!tapped) return;

            var ray = cam.ScreenPointToRay(screen);
            RaycastHit hit;
            // Hit the Plane: it has MeshCollider added by CreatePrimitive
            if (Physics.Raycast(ray, out hit, 200f))
            {
                player.MoveTo(hit.point);
            }
        }
    }
}
