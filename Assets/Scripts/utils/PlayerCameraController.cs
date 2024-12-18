using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerCameraController : NetworkBehaviour {
    public override void OnNetworkSpawn() {
        if (!IsOwner) return;

        var mainCamera = Camera.main;
        var cinemachineVirtualCamera = mainCamera.GetComponent<CinemachineCamera>();
        cinemachineVirtualCamera.Follow = transform;
        cinemachineVirtualCamera.LookAt = transform;
    }
}
