using UnityEngine;
using Unity.Netcode;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;

/// <summary>
/// Adjusts the z-position of the GameObject based on its y-position for proper layering.
/// Supports both stationary and moving objects, optimized for performance and multiplayer synchronization.
/// </summary>
[RequireComponent(typeof(Collider2D), typeof(NetworkObject))]
public class ZDepth : NetworkBehaviour {
    [SerializeField] private bool _isObjectStationary = true;

    [SerializeField] private Collider2D _objectCollider2D;

    // NetworkVariable to synchronize z-position across clients
    private NetworkVariable<float> _networkZPosition = new NetworkVariable<float>();

    private void Awake() {
        // Ensure the Collider2D component is assigned
        if (_objectCollider2D == null) {
            if (!TryGetComponent(out _objectCollider2D)) {
                Debug.LogError($"{nameof(ZDepth)} requires a Collider2D component.");
                enabled = false;
                return;
            }
        }

        if (_isObjectStationary) {
            AdjustZDepthServerRpc();
            // Disable the script after adjusting for stationary objects
            enabled = false;
        }
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) { 
            return; 
        }

        // Initialize the networked z-position
        float colliderOffsetY = _objectCollider2D.offset.y;
        float initialZ = CalculateZPosition(transform.position.y, colliderOffsetY);
        _networkZPosition.Value = initialZ;

        // Update the local z-position
        UpdateLocalZPosition(initialZ);

        // Subscribe to changes in networkZPosition to update clients
        _networkZPosition.OnValueChanged += OnZPositionChanged;

        if (!_isObjectStationary) {
            // Schedule the z-depth adjustment job for moving objects
            ScheduleZDepthJob();
        }
    }

    private void OnDestroy() {
        if (IsServer) {
            _networkZPosition.OnValueChanged -= OnZPositionChanged;
        }
    }

    /// <summary>
    /// Adjusts the z-position based on the y-position and collider offset.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void AdjustZDepthServerRpc() {
        float newZ = CalculateZPosition(transform.position.y, _objectCollider2D.offset.y);
        transform.position = new Vector3(transform.position.x, transform.position.y, newZ);
        _networkZPosition.Value = newZ;
    }

    /// <summary>
    /// Calculates the z-position based on y-position and collider offset.
    /// </summary>
    /// <param name="y">The y-position of the object.</param>
    /// <param name="colliderOffsetY">The y-offset of the collider.</param>
    /// <returns>The calculated z-position.</returns>
    private static float CalculateZPosition(float y, float colliderOffsetY) => (y + colliderOffsetY) * 0.0001f;

    /// <summary>
    /// Callback for when the networked z-position changes.
    /// </summary>
    /// <param name="previousValue">The previous z-position value.</param>
    /// <param name="newValue">The new z-position value.</param>
    private void OnZPositionChanged(float previousValue, float newValue) {
        UpdateLocalZPosition(newValue);
    }

    /// <summary>
    /// Updates the local transform's z-position.
    /// </summary>
    /// <param name="newZ">The new z-position value.</param>
    private void UpdateLocalZPosition(float newZ) {
        Vector3 pos = transform.position;
        pos.z = newZ;
        transform.position = pos;
    }

    /// <summary>
    /// Schedules a Burst-compiled job to adjust z-position for moving objects.
    /// </summary>
    private void ScheduleZDepthJob() {
        // Allocate a NativeArray to store the result
        NativeArray<float> outputZ = new NativeArray<float>(1, Allocator.TempJob);

        // Create the job
        var job = new ZDepthJob {
            yPosition = transform.position.y,
            colliderOffsetY = _objectCollider2D.offset.y,
            outputZ = outputZ
        }.Schedule();

        // Complete the job
        job.Complete();

        // Retrieve the result and update the NetworkVariable
        float newZ = outputZ[0];
        _networkZPosition.Value = newZ;

        // Dispose of the NativeArray
        outputZ.Dispose();
    }

    /// <summary>
    /// Burst-compiled job for calculating z-position.
    /// </summary>
    [BurstCompile]
    private struct ZDepthJob : IJob {
        public float yPosition;
        public float colliderOffsetY;
        public NativeArray<float> outputZ;

        public void Execute() {
            float newZ = (yPosition + colliderOffsetY) * 0.0001f;
            outputZ[0] = newZ;
        }
    }
}
