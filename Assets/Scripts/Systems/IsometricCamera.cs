using UnityEngine;

namespace Echoes.Systems
{
    /// <summary>
    /// Top-down isometric (Diablo II angle) follow camera.
    /// </summary>
    public class IsometricCamera : MonoBehaviour
    {
        public Transform target;
        public Vector3   offset = new Vector3(0f, 12f, -10f);
        public float     pitch  = 55f;
        public float     yaw    = 45f;
        public float     smooth = 6f;

        private void LateUpdate()
        {
            if (target == null) return;
            var desiredPos = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * smooth);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
