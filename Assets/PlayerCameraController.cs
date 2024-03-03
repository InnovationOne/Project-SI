using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerCameraController : NetworkBehaviour
{
    public override void OnNetworkSpawn() {
        if (IsOwner) {
            Camera camera = Camera.main;
            CinemachineVirtualCamera cinemachineVirtualCamera = camera.GetComponent<CinemachineVirtualCamera>();

            cinemachineVirtualCamera.Follow = transform;
            cinemachineVirtualCamera.LookAt = transform;
        }
    }
}
