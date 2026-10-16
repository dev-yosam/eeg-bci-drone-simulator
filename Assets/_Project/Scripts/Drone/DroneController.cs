using UnityEngine;
using UnityEngine.InputSystem;

namespace EEGDroneExperiment
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class DroneController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 4f;
        [SerializeField] private Vector3 startPosition = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private ExperimentManager experimentManager;
        private Rigidbody body;
        private Vector2 moveInput;
        private float verticalInput;

        public Vector3 PhysicsPosition => body != null ? body.position : transform.position;

        public void Configure(ExperimentManager manager, bool captureStartPosition = true)
        {
            experimentManager = manager;
            if (captureStartPosition) startPosition = transform.position;
        }

        public void ResetForRun()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = (body.constraints & ~RigidbodyConstraints.FreezePositionY) | RigidbodyConstraints.FreezeRotation;
            body.position = startPosition;
            body.rotation = Quaternion.identity;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(startPosition, Quaternion.identity);
            moveInput = Vector2.zero;
            verticalInput = 0f;
            if (experimentManager == null)
                Debug.LogError("Drone requires an ExperimentManager reference. Rebuild the experiment scene.", this);
        }

        private void Update()
        {
            moveInput = Vector2.zero;
            verticalInput = 0f;
            var keyboard = Keyboard.current;
            if (keyboard == null || !Application.isFocused || experimentManager == null || !experimentManager.CanMove)
                return;

            moveInput.x = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            moveInput.y = (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f);
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            verticalInput = (keyboard.spaceKey.isPressed ? 1f : 0f) - (keyboard.leftCtrlKey.isPressed ? 1f : 0f);
        }

        private void FixedUpdate()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
            if (!Application.isFocused || experimentManager == null || !experimentManager.CanMove)
            {
                StopMotion();
                return;
            }
            Vector3 direction = new Vector3(moveInput.x, verticalInput, moveInput.y);
            body.linearVelocity = Vector3.ClampMagnitude(direction, 1f) * moveSpeed;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (experimentManager == null || !experimentManager.IsTarget(collision.collider))
                return;

            // Capture at the collision callback before logging or formatting.
            double timestamp = Time.realtimeSinceStartupAsDouble;
            if (experimentManager.RecordBusCollision(timestamp))
            {
                Vector3 effectPosition = transform.position;
                if (collision.contactCount > 0)
                {
                    ContactPoint contact = collision.GetContact(0);
                    effectPosition = contact.point + contact.normal * 0.2f;
                }
                if (experimentManager.PlayExplosion) CollisionExplosion.Play(effectPosition);
            }
        }

        public void StopMotion()
        {
            moveInput = Vector2.zero;
            verticalInput = 0f;
            if (body == null) body = GetComponent<Rigidbody>();
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
