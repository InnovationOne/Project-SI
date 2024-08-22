using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerCameraController : NetworkBehaviour {
    public override void OnNetworkSpawn() {
        if (IsOwner) {
            Camera camera = Camera.main;
            CinemachineCamera cinemachineVirtualCamera = camera.GetComponent<CinemachineCamera>();

            cinemachineVirtualCamera.Follow = transform;
            cinemachineVirtualCamera.LookAt = transform;
        }
    }
}
