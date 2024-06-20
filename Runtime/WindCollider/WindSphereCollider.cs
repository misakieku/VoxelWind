using UnityEngine;

namespace VoxelWind
{
    public class WindSphereCollider : MonoBehaviour
    {
        public float Radius = 1.0f;
        public float PushStrength = 0.05f;
        public float ShadowStrength = 1.0f;
        public float ShadowDistance = 1.0f;

        private Vector3 _previousPosition;

        private WindColliderData _windCollider = new();
        public WindColliderData WindCollider => _windCollider;

        private void Update()
        {
            UpdateWindCollider();
        }

        private void UpdateWindCollider()
        {
            _windCollider.Position.xyz = transform.position;
            _windCollider.Velocity.xyz = (transform.position - _previousPosition) / Time.deltaTime;
            _windCollider.Radius = Radius;
            _windCollider.PushStrength = PushStrength;
            _windCollider.ShadowStrength = ShadowStrength;
            _windCollider.ShadowDistance = ShadowDistance;

            _previousPosition = transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1,0,1);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}