using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(BoxCollider2D))]
public class ResourceNode : NetworkBehaviour {
    [Header("Node Settings")]
    [SerializeField] private ResourceNodeType _nodeType;
    [SerializeField] private ItemSpawnManager.SpreadType _spreadType;
    [SerializeField] private int _startingHP;
    [SerializeField] private int _minimumToolRarity;

    [Header("Item Slot Settings")]
    [SerializeField] private ItemSO _itemSO;
    [SerializeField] private int _minDropCount;
    [SerializeField] private int _maxDropCount;
    [SerializeField] private int _rarityID;

    [Header("Shake Settings")]
    [SerializeField] private float _shakeAmountX = 0.05f;
    [SerializeField] private float _shakeAmountY = 0.01f;
    [SerializeField] private float _timeBetweenShakes = 0.03f;

    [Header("Visual Settings")]
    [SerializeField] private SpriteRenderer _resourceNodeHighlight;

    private int _currentHp;
    private BoxCollider2D _boxCollider2D;


    private void Awake() {
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _resourceNodeHighlight.gameObject.SetActive(false);
    }

    private void Start() {
        _currentHp = _startingHP;

        TimeAndWeatherManager.Instance.OnNextDayStarted += TimeAndWeatherManager_OnNextDayStarted;
    }

    private void TimeAndWeatherManager_OnNextDayStarted() {
        _currentHp = _startingHP;
    }

    public void HitResourceNode(int damage) {
        if (PlayerToolbeltController.LocalInstance.GetCurrentlySelectedToolbeltItemSlot().RarityId > _minimumToolRarity) {
            Debug.Log("Tool Rarity to low");
            // Play bounce back animation

            return;

        }

        HitResourceNodeServerRpc(damage);

        if (_currentHp > 0) {
            return;
        }
        
        // Play destroy animation

        int dropCount = Random.Range(_minDropCount, _maxDropCount);
        Vector3 position = new(transform.position.x + _boxCollider2D.offset.x, transform.position.y + _boxCollider2D.offset.y);
        ItemSpawnManager.Instance.SpawnItemServerRpc(
            itemSlot: new ItemSlot(_itemSO.ItemId, dropCount, _rarityID),
            initialPosition: position, 
            motionDirection: PlayerMovementController.LocalInstance.LastMotionDirection, 
            spreadType: _spreadType);


        DestroyGameObjectServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void HitResourceNodeServerRpc(int damage) {


        HitResourceNodeClientRpc(damage);
    }

    [ClientRpc]
    private void HitResourceNodeClientRpc(int damage) {
        _currentHp -= damage;

        PlaySound();

        if (_currentHp > 0) {
            StartCoroutine(ShakeAfterHit());

            if (_nodeType == ResourceNodeType.Tree) {
                // ITEM OBEN AM BAUM SPAWNEN UND AUF DEN BODEN FALLEN LASSEN, DABEI SOLL DAS ITEM NOCH EINMAL HOCHSPRINGEN
                // Z.B. AUCH BIENENNEST
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyGameObjectServerRpc() {
        DestroyGameObjectClientRpc();
    }

    [ClientRpc]
    private void DestroyGameObjectClientRpc() {
        Destroy(gameObject);
    }

    IEnumerator ShakeAfterHit() {
        for (int i = 0; i < 3; i++) {
            transform.localPosition += new Vector3(_shakeAmountX, _shakeAmountY);
            yield return new WaitForSeconds(_timeBetweenShakes);
            transform.localPosition -= new Vector3(_shakeAmountX, _shakeAmountY);
            yield return new WaitForSeconds(_timeBetweenShakes);
        }
    }

    public bool CanHitResourceNodeType(List<ResourceNodeType> canBeHit) {
        return canBeHit.Contains(_nodeType);
    }

    public void ShowPossibleInteraction(bool show) {
        _resourceNodeHighlight.gameObject.SetActive(show);
    }

    private void PlaySound() {
        switch (_nodeType) {
            case ResourceNodeType.Tree:
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.HitTreeSFX, transform.position);
                break;
            case ResourceNodeType.Ore:
                // Play ore sound
                break;
            default:
                break;
        }

        
    }
}
