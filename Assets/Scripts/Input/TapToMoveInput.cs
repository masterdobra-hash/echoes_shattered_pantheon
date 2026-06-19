using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Echoes.Input
{
    /// <summary>
    /// Tap-to-move (Diablo II): raycast from screen point onto Ground layer,
    /// send destination to the PlayerController.
    /// </summary>
    public class TapToMoveInput : MonoBehaviour
    {
        public Camera     cam;
        public LayerMask  groundMask;
        public Echoes.Entities.PlayerController player;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (player == null || cam == null) return;
            bool tapped = false;
            Vector2 screen = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            { tapped = true; screen = Mouse.current.position.ReadValue(); }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            { tapped = true; screen = Touchscreen.current.primaryTouch.position.ReadValue(); }
#else
            if (UnityEngine.Input.GetMouseButtonDown(0))
            { tapped = true; screen = UnityEngine.Input.mousePosition; }
#endif
            if (!tapped) return;

            var ray = cam.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out var hit, 200f, groundMask))
                player.MoveTo(hit.point);
        }
    }
}
