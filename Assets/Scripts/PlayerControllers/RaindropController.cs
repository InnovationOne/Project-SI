using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls raindrop particles and handles splash effects upon raindrop expiration.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class RaindropController : MonoBehaviour {
    [Header("Splash Settings")]
    [SerializeField] GameObject _raindropSplashPrefab;
    [SerializeField] int _poolSize = 100;

    ParticleSystem _particleSystem;
    ParticleSystem.EmissionModule _emissionModule;
    Queue<GameObject> _splashPool = new();
    float _splashAnimationLength;
    ParticleSystem.Particle[] _particles;
    WeatherManager _weatherManager;

    void Awake() {
        _particleSystem = GetComponent<ParticleSystem>();
        _emissionModule = _particleSystem.emission;

        InitializeSplashPool();
        CacheSplashAnimationLength();
    }

    void Start() {
        _weatherManager = WeatherManager.Instance;
        _weatherManager.OnChangeRainIntensity += OnChangeRainIntensity;
    }

    void OnDestroy() {
        _weatherManager.OnChangeRainIntensity -= OnChangeRainIntensity;
    }

    // Preloads a pool of splash objects for reuse
    void InitializeSplashPool() {
        for (int i = 0; i < _poolSize; i++) {
            var splash = Instantiate(_raindropSplashPrefab);
            splash.SetActive(false);
            _splashPool.Enqueue(splash);
        }
    }

    // Caches the splash animation duration to reduce overhead
    void CacheSplashAnimationLength() {
        var animator = _raindropSplashPrefab.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.animationClips.Length > 0) {
            _splashAnimationLength = animator.runtimeAnimatorController.animationClips[0].length;
        } else {
            _splashAnimationLength = 1f;
            Debug.LogWarning("Missing splash animation or animator. Using default animation length.");
        }
    }

    // Adjusts particle emission based on rain intensity changes
    void OnChangeRainIntensity(int intensity) => _emissionModule.rateOverTime = intensity;

    // Checks particles each frame, spawns splashes where needed
    void Update() {
        int particleCount = _particleSystem.particleCount;
        if (particleCount == 0) return;

        // Resize array if needed
        if (_particles == null || _particles.Length < particleCount) {
            _particles = new ParticleSystem.Particle[particleCount];
        }

        // Get current particles and process them
        int retrievedParticles = _particleSystem.GetParticles(_particles);
        for (int i = 0; i < retrievedParticles; i++) {
            if (_particles[i].remainingLifetime <= 0.1f) {
                SpawnSplash(_particles[i].position);
                _particles[i].remainingLifetime = -1f;
            }
        }

        // Apply updated particle states
        _particleSystem.SetParticles(_particles, retrievedParticles);
    }

    // Spawns or reuses a splash object at the given position
    void SpawnSplash(Vector3 position) {
        // Use pooled object if available; otherwise, create a new splash and destroy it later
        if (_splashPool.Count > 0) {
            var splash = _splashPool.Dequeue();
            splash.transform.position = position;
            splash.SetActive(true);

            if (splash.TryGetComponent<Animator>(out var animator)) {
                animator.Play(0, -1, 0f);
            }

            StartCoroutine(ReturnSplashToPoolCoroutine(splash, _splashAnimationLength));
        } else {
            // Pool exhausted; fallback to a temporary instantiated object
            var splash = Instantiate(_raindropSplashPrefab, position, Quaternion.identity);
            var animator = splash.GetComponent<Animator>();
            float length = (animator != null && animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.animationClips.Length > 0)
                            ? animator.runtimeAnimatorController.animationClips[0].length
                            : 1f;

            Destroy(splash, length + 0.6f);
        }
    }

    // Returns a splash object to the pool after its animation finishes
    IEnumerator ReturnSplashToPoolCoroutine(GameObject splash, float delay) {
        yield return new WaitForSeconds(delay + 0.6f);
        ReturnSplashToPool(splash);
    }

    // Re-enqueues a used splash object into the pool
    void ReturnSplashToPool(GameObject splash) {
        splash.SetActive(false);
        _splashPool.Enqueue(splash);
    }
}
