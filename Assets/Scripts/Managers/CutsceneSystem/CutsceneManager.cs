using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using static CutsceneSegmentContainer;

public class CutsceneManager : NetworkBehaviour {
    public static CutsceneManager Instance { get; private set; }

    private void Awake() {
        // Singleton setup.
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayCutscene(CutsceneInfo cutscene) {
        if (IsServer) {
            Debug.Log("Server starting cutscene: " + cutscene.CutsceneId);
            StartCoroutine(PlayCutsceneSequence(cutscene));
            PlayCutsceneClientRpc(); // Notify clients to run the same sequence.
        } else {
            Debug.LogWarning("Only the server can initiate cutscenes.");
        }
    }

    private IEnumerator PlayCutsceneSequence(CutsceneInfo cutscene) {
        foreach (CutsceneSegmentContainer segment in cutscene.Segments) {
            // Wait the segment's start delay.
            yield return new WaitForSeconds(segment.SegmentStartDelay);

            Debug.Log("Executing segment: " + segment.SegmentDescription + " [" + segment.SegmentType + "]");
            yield return StartCoroutine(ExecuteSegment(segment));

            // Handle additional waiting based on the complete type.
            switch (segment.CompleteType) {
                case SegmentCompleteTypes.Timer:
                    yield return new WaitForSeconds(segment.SegmentTimer);
                    break;
                case SegmentCompleteTypes.Chat:
                    // For chat, the waiting is handled inside the segment execution.
                    break;
                case SegmentCompleteTypes.Movement:
                    break;
            }
        }
        Debug.Log("Cutscene sequence completed.");
    }

    /// <summary>
    /// Executes the logic for a single segment based on its SegmentType.
    /// </summary>
    private IEnumerator ExecuteSegment(CutsceneSegmentContainer segment) {
        switch (segment.SegmentType) {
            // Immediately terminates the cutscene sequence when reached.
            case SegmentTypes.End:
                Debug.Log("End segment reached. Terminating cutscene.");
                yield break;

            // Initiates a conversation with a main NPC using dialogue.
            case SegmentTypes.MainNPCChat: {
                    bool dialogueFinished = false;
                    void dialogueHandler() {
                        dialogueFinished = true;
                        GameManager.Instance.DialogueManager.DialogueComplete -= dialogueHandler;
                    }

                    GameManager.Instance.DialogueManager.DialogueComplete += dialogueHandler;
                    if (segment.InkJSON != null) {
                        GameManager.Instance.DialogueManager.EnterDialogueMode(segment.InkJSON);
                        Debug.Log("MainNPCChat: Starting Ink dialogue " + segment.InkJSON.name);
                    }
                    yield return new WaitUntil(() => dialogueFinished);
                }
                break;

            // Displays dialogue in the form of text bubbles (often over a character’s head).
            case SegmentTypes.TextBubbleChat: {
                    bool bubbleFinished = false;
                    void bubbleHandler() {
                        bubbleFinished = true;
                        GameManager.Instance.DialogueManager.DialogueComplete -= bubbleHandler;
                    }

                    GameManager.Instance.DialogueManager.DialogueComplete += bubbleHandler;
                    GameManager.Instance.DialogueManager.StartTextBubble(segment.DialogueText, segment.PrimaryTarget);
                    Debug.Log("TextBubbleChat: " + segment.DialogueText);
                    yield return new WaitUntil(() => bubbleFinished);
                }
                break;

            // Instantiates a character prefab at a designated spawn point.
            case SegmentTypes.SpawnCharacter: {
                    if (segment.CharacterPrefab != null && segment.SpawnPoint != null) {
                        Instantiate(segment.CharacterPrefab, segment.SpawnPoint.position, segment.SpawnPoint.rotation);
                        Debug.Log("SpawnCharacter: Spawned character at " + segment.SpawnPoint.position);
                    } else {
                        Debug.LogWarning("SpawnCharacter: Missing prefab or spawn point.");
                    }
                }
                break;

            // Destroys or deactivates a character game object.
            case SegmentTypes.RemoveCharacter: {
                    if (segment.PrimaryTarget != null) {
                        Destroy(segment.PrimaryTarget);
                        Debug.Log("RemoveCharacter: Removed character.");
                    }
                }
                break;

            // Triggers an animation on a character.
            case SegmentTypes.CharacterAnimate: {
                    if (segment.PrimaryTarget != null) {
                        if (segment.PrimaryTarget.TryGetComponent<Animator>(out var anim)) {
                            //anim.SetTrigger("Animate");
                            Debug.Log("CharacterAnimate: Triggered animation.");
                        }
                    }
                }
                break;
                /*
            // Commands a character to move to a target position using a NavMeshAgent.
            case SegmentTypes.CharacterMove: {
                    if (segment.PrimaryTarget != null) {
                        Vector3 startPos = segment.PrimaryTarget.transform.position;
                        // Umwandlung des Vector2-Ziels in einen Vector3 (Y bleibt unverändert)
                        Vector3 endPos = new Vector3(segment.MoveDestination.x, startPos.y, segment.MoveDestination.y);
                        bool pathCompleted = false;

                        void callback(Vector3[] path, bool success) {
                            if (success) {
                                // Starte das Path-Following – nach Beendigung setzt der Callback pathCompleted auf true.
                                StartCoroutine(FollowPathCoroutine(segment.PrimaryTarget, path, segment.ArrivalThreshold, () => {
                                    pathCompleted = true;
                                }));
                            } else {
                                Debug.LogWarning("CharacterMove: Pathfinding fehlgeschlagen.");
                                pathCompleted = true;
                            }
                        }

                        PathRequestManager.RequestPath(new PathRequest(startPos, endPos, callback));
                        yield return new WaitUntil(() => pathCompleted);
                    }
                }
                break;

            // Similar to CharacterMove, but semantically indicates the character is moving toward furniture (or an object of interaction).
            case SegmentTypes.CharacterTargetFurniture: {
                    if (segment.PrimaryTarget != null && segment.TargetFurniture != null) {
                        Vector3 startPos = segment.PrimaryTarget.transform.position;
                        Vector3 endPos = segment.TargetFurniture.position;
                        bool pathCompleted = false;

                        void callback(Vector3[] path, bool success) {
                            if (success) {
                                StartCoroutine(FollowPathCoroutine(segment.PrimaryTarget, path, segment.ArrivalThreshold, () => {
                                    pathCompleted = true;
                                }));
                            } else {
                                Debug.LogWarning("CharacterTargetFurniture: Pathfinding fehlgeschlagen.");
                                pathCompleted = true;
                            }
                        }

                        PathRequestManager.RequestPath(new PathRequest(startPos, endPos, callback));
                        yield return new WaitUntil(() => pathCompleted);
                    }
                }
                break;

            // Directs a character toward an exit point (e.g., leaving a room or area).
            case SegmentTypes.CharacterTargetExit: {
                    if (segment.PrimaryTarget != null && segment.ExitPoint != null) {
                        Vector3 startPos = segment.PrimaryTarget.transform.position;
                        Vector3 endPos = segment.ExitPoint.position;
                        bool pathCompleted = false;

                        void callback(Vector3[] path, bool success) {
                            if (success) {
                                StartCoroutine(FollowPathCoroutine(segment.PrimaryTarget, path, segment.ArrivalThreshold, () => {
                                    pathCompleted = true;
                                }));
                            } else {
                                Debug.LogWarning("CharacterTargetExit: Pathfinding fehlgeschlagen.");
                                pathCompleted = true;
                            }
                        }

                        PathRequestManager.RequestPath(new PathRequest(startPos, endPos, callback));
                        yield return new WaitUntil(() => pathCompleted);

                        // Nach dem Erreichen des Exit-Punktes: Falls eine Exit-ID angegeben wurde, wird die Szenenlogik getriggert.
                        if (!string.IsNullOrEmpty(segment.ExitPointID)) {
                            SceneTransitionManager.Instance.TransitionToExit(segment.ExitPointID);
                        }
                    }
                }
                break;
                */
            // Instructs the camera to move to a target position.
            case SegmentTypes.CameraMove: {
                    if (Camera.main.TryGetComponent<CinemachineCamera>(out var cam)) {
                        Vector3 startPos = cam.transform.position;
                        Vector3 targetPos = segment.TargetPosition;
                        float elapsed = 0f;
                        float duration = segment.SegmentTimer; // Nutzt segmentTimer als Bewegungsdauer

                        while (elapsed < duration) {
                            elapsed += Time.deltaTime;
                            cam.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
                            yield return null;
                        }
                        cam.transform.position = targetPos;
                        Debug.Log("CameraMove: Kamera sanft bewegt zu " + targetPos);
                    }
                }
                break;

            // Instructs the camera to follow a target (e.g. a NPC, player or object).
            case SegmentTypes.CameraFollow: {
                    if (Camera.main.TryGetComponent<CinemachineCamera>(out var cam) && segment.PrimaryTarget != null) {
                        cam.transform.position = segment.PrimaryTarget.transform.position;
                        cam.Follow = segment.PrimaryTarget.transform;
                        Debug.Log("CameraFollow: Kamera folgt " + segment.PrimaryTarget.name);
                    }
                }
                break;


            // Commands a door object to open.
            case SegmentTypes.OpenDoor: {
                    if (segment.PrimaryTarget != null && segment.PrimaryTarget.TryGetComponent<DoorController>(out var door)) {
                        door.Open();
                        Debug.Log("OpenDoor: Door opened.");
                    }
                }
                break;

            // Similar to OpenDoor, but triggers a door-closing animation.
            case SegmentTypes.CloseDoor: {
                    if (segment.PrimaryTarget != null) {
                        if (segment.PrimaryTarget.TryGetComponent<DoorController>(out var door)) {
                            door.Close();
                            Debug.Log("CloseDoor: Door closed.");
                        }
                    }
                }
                break;

            // Activates a wall object (usually via SetActive(true)).
            case SegmentTypes.ShowWall: {
                    if (segment.PrimaryTarget != null) {
                        segment.PrimaryTarget.SetActive(true);
                        Debug.Log("ShowWall: Wall shown.");
                    }
                }
                break;

            // Deactivates a wall object (SetActive(false)).
            case SegmentTypes.HideWall: {
                    if (segment.PrimaryTarget != null) {
                        segment.PrimaryTarget.SetActive(false);
                        Debug.Log("HideWall: Wall hidden.");
                    }
                }
                break;

            // Deactivates any game object (typically using SetActive(false)).
            case SegmentTypes.HideObject: {
                    if (segment.PrimaryTarget != null) {
                        segment.PrimaryTarget.SetActive(false);
                        Debug.Log("HideObject: Object hidden.");
                    }
                }
                break;

            // Plays an audio clip on a specified game object that has an AudioSource component.
            case SegmentTypes.PlayAudio: {
                    if (segment.PrimaryTarget != null) {
                        GameManager.Instance.AudioManager.PlayOneShot(segment.AudioEvent, segment.TargetPosition);
                        Debug.Log($"PlayAudio: Sound abgespielt ({segment.AudioEvent.Guid}).");
                    }
                }
                break;

            // Changes the player’s spawn location.
            case SegmentTypes.RedirectPlayerSpawn: {
                    if (PlayerController.LocalInstance != null && PlayerController.LocalInstance.PlayerHealthAndEnergyController != null) {
                        PlayerController.LocalInstance.PlayerHealthAndEnergyController.SetRespawnPoint(segment.TargetPosition);
                        Debug.Log("RedirectPlayerSpawn: Player spawn set to " + segment.TargetPosition);
                    }
                }
                break;

            // Triggers an NPC to give a gift.
            case SegmentTypes.NPCGiveGift: {
                    if (PlayerController.LocalInstance != null && PlayerController.LocalInstance.PlayerInventoryController != null) {
                        PlayerController.LocalInstance.PlayerInventoryController.InventoryContainer.AddItem(segment.ItemSlot, false);
                        Debug.Log("NPCGiveGift: Gift given by NPC.");
                    }
                }
                break;

            // Updates the players money.
            case SegmentTypes.MoneyUpdate: {
                    GameManager.Instance.FinanceManager.AddMoneyServerRpc(segment.MoneyAmount, true);
                    Debug.Log("CreditsUpdate: Credits updated.");
                }
                break;

            // Displays an emoji (or icon) above a character.
            case SegmentTypes.CharacterEmoji: {
                    if (segment.PrimaryTarget != null) {
                        GameManager.Instance.DialogueManager.SetEmoji(segment.PrimaryTarget, segment.DialogueText);
                        Debug.Log("CharacterEmoji: Displaying emoji " + segment.DialogueText);
                    }
                }
                break;

            // Re-enables the main game UI (canvas).
            case SegmentTypes.ShowGameCanvas: {
                    //GameUIManager.Instance.ShowCanvas();
                    Debug.Log("ShowGameCanvas: Game canvas shown.");
                }
                break;

            // Hides the main game UI (canvas).
            case SegmentTypes.HideGameCanvas: {
                    //GameUIManager.Instance.HideCanvas();
                    Debug.Log("HideGameCanvas: Game canvas hidden.");
                }
                break;

            // Rotates a character to face a specified target direction.
            case SegmentTypes.CharacterDirection: {
                    if (PlayerController.LocalInstance != null && PlayerController.LocalInstance.PlayerAnimationController != null) {
                        Vector2 dir = segment.Direction;
                        PlayerController.LocalInstance.PlayerAnimationController.SetAnimatorDirection(dir);
                        PlayerController.LocalInstance.PlayerAnimationController.SetAnimatorLastDirection(dir);
                        Debug.Log("CharacterDirection: Spieler richtet sich aus in Richtung " + dir);
                    }
                }
                break;

            // Toggles a letterboxing effect (black bars on the top and bottom of the screen) to create a cinematic look.
            case SegmentTypes.Letterboxing: {
                    if (segment.LetterboxElements != null && segment.LetterboxElements.Length >= 2) {
                        float targetHeight = 64f;
                        RectTransform topRect = segment.LetterboxElements[0].rectTransform;
                        RectTransform bottomRect = segment.LetterboxElements[1].rectTransform;

                        // Annahme: Die Ausgangspositionen sind so gewählt, dass die Letterboxen außerhalb des sichtbaren Bereichs liegen.
                        Vector2 topStart = topRect.anchoredPosition;
                        Vector2 bottomStart = bottomRect.anchoredPosition;

                        Vector2 topTarget, bottomTarget;
                        if (segment.EnableLetterboxing) {
                            // Beim Aktivieren fahren die Letterboxen in den Bildschirm (z. B. y=0)
                            topTarget = new Vector2(topStart.x, 0);
                            bottomTarget = new Vector2(bottomStart.x, 0);
                        } else {
                            // Beim Deaktivieren fahren sie wieder außerhalb – top nach unten (+targetHeight), bottom nach oben (-targetHeight)
                            topTarget = new Vector2(topStart.x, targetHeight);
                            bottomTarget = new Vector2(bottomStart.x, -targetHeight);
                        }

                        float elapsed = 0f;
                        float duration = segment.LetterboxingDuration;
                        while (elapsed < duration) {
                            elapsed += Time.deltaTime;
                            float t = elapsed / duration;
                            topRect.anchoredPosition = Vector2.Lerp(topStart, topTarget, t);
                            bottomRect.anchoredPosition = Vector2.Lerp(bottomStart, bottomTarget, t);
                            yield return null;
                        }
                        topRect.anchoredPosition = topTarget;
                        bottomRect.anchoredPosition = bottomTarget;
                        Debug.Log("Letterboxing: Letterboxing-Effekt " + (segment.EnableLetterboxing ? "aktiviert" : "deaktiviert") + ".");
                    }
                    break;
                }

            // Transitions to a new scene.
            case SegmentTypes.ChangeScene: {
                    if (!string.IsNullOrEmpty(segment.SceneName)) {
                        Debug.Log("ChangeScene: Szene wird gewechselt zu " + segment.SceneName);
                        SceneManager.LoadScene(segment.SceneName);
                    } else {
                        Debug.LogWarning("ChangeScene: Kein Szenenname angegeben.");
                    }
                }
                break;

            default: {
                    Debug.LogWarning("Segment type not implemented: " + segment.SegmentType);
                }
                break;
        }

        yield return null;
    }

    [ClientRpc]
    private void PlayCutsceneClientRpc() {
        // On clients, you would typically start the same sequence.
        if (!IsServer) {
            Debug.Log("Client starting cutscene sequence.");
            // Clients can either call PlayCutsceneSequence() with a known asset
            // or use another method to sync the cutscene.
        }
    }
}
