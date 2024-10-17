using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls raindrop particles and handles splash effects upon raindrop expiration.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class RaindropController : MonoBehaviour {
    [Header("Splash Settings")]
    [SerializeField] private GameObject _raindropSplashPrefab;
    [SerializeField] private int _poolSize = 100;

    private ParticleSystem _particleSystem;
    private ParticleSystem.EmissionModule _emissionModule;

    // Object pool for splash effects
    private Queue<GameObject> _splashPool = new Queue<GameObject>();
    private float _splashAnimationLength;

    // Reusable particle array to minimize allocations
    private ParticleSystem.Particle[] _particles;


    private void Awake() {
        _particleSystem = GetComponent<ParticleSystem>();
        _emissionModule = _particleSystem.emission;

        InitializeSplashPool();
        CacheSplashAnimationLength();
    }

    private void Start() {
        WeatherManager.Instance.OnChangeRainIntensity += OnChangeRainIntensity;
    }

    private void OnDestroy() {
        WeatherManager.Instance.OnChangeRainIntensity -= OnChangeRainIntensity;
    }

    /// <summary>
    /// Initializes the splash object pool.
    /// </summary>
    private void InitializeSplashPool() {
        for (int i = 0; i < _poolSize; i++) {
            GameObject splash = Instantiate(_raindropSplashPrefab);
            splash.SetActive(false);
            _splashPool.Enqueue(splash);
        }
    }

    /// <summary>
    /// Caches the animation length of the splash prefab to avoid repeated calculations.
    /// </summary>
    private void CacheSplashAnimationLength() {
        Animator animator = _raindropSplashPrefab.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.animationClips.Length > 0) {
            _splashAnimationLength = animator.runtimeAnimatorController.animationClips[0].length;
        } else {
            _splashAnimationLength = 1f; // Default duration if not set
            Debug.LogWarning("Splash prefab Animator or animation clips are missing. Using default animation length.");
        }
    }

    /// <summary>
    /// Updates the rain intensity based on the WeatherManager's event.
    /// </summary>
    /// <param name="intensity">New rain intensity value.</param>
    private void OnChangeRainIntensity(int intensity) {
        _emissionModule.rateOverTime = intensity;
    }

    private void Update() {
        int particleCount = _particleSystem.particleCount;

        // Initialize or resize the particles array if necessary
        if (_particles == null || _particles.Length < particleCount) {
            _particles = new ParticleSystem.Particle[particleCount];
        }

        // Retrieve current particles
        int retrievedParticles = _particleSystem.GetParticles(_particles);

        for (int i = 0; i < retrievedParticles; i++) {
            if (IsRaindropExpired(_particles[i])) {
                SpawnSplash(_particles[i].position);
                _particles[i].remainingLifetime = -1f; // Kill the particle
            }
        }

        // Apply the updated particles back to the system
        _particleSystem.SetParticles(_particles, retrievedParticles);
    }

    /// <summary>
    /// Determines if a raindrop particle has expired.
    /// </summary>
    /// <param name="particle">The particle to check.</param>
    /// <returns>True if the particle is expired; otherwise, false.</returns>
    private bool IsRaindropExpired(ParticleSystem.Particle particle) {
        return particle.remainingLifetime <= 0.1f;
    }

    /// <summary>
    /// Spawns a splash effect at the specified position using the object pool.
    /// </summary>
    /// <param name="position">Position to spawn the splash.</param>
    private void SpawnSplash(Vector3 position) {
        if (_splashPool.Count > 0) {
            GameObject splash = _splashPool.Dequeue();
            splash.transform.position = position;
            splash.SetActive(true);

            // Optionally, reset the animator to play the animation from the start
            if (splash.TryGetComponent<Animator>(out var animator)) {
                animator.Play(0, -1, 0f);
            }

            // Start coroutine to return the splash to the pool after the animation completes
            StartCoroutine(ReturnSplashToPoolCoroutine(splash, _splashAnimationLength));
        } else {
            // Pool exhausted; instantiate a new splash if necessary
            GameObject splash = Instantiate(_raindropSplashPrefab, position, Quaternion.identity);
            Animator animator = splash.GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.animationClips.Length > 0) {
                _splashAnimationLength = animator.runtimeAnimatorController.animationClips[0].length;
            } else {
                _splashAnimationLength = 1f; // Default duration if not set
                Debug.LogWarning("Splash prefab Animator or animation clips are missing. Using default animation length.");
            }
            Destroy(splash, _splashAnimationLength + 0.6f);
        }
    }

    /// <summary>
    /// Coroutine to return a splash GameObject back to the object pool after a delay.
    /// </summary>
    /// <param name="splash">The splash GameObject to return.</param>
    /// <param name="delay">Delay in seconds before returning the splash.</param>
    /// <returns></returns>
    private IEnumerator ReturnSplashToPoolCoroutine(GameObject splash, float delay) {
        yield return new WaitForSeconds(delay + 0.6f); // Adding extra time to ensure animation completes
        ReturnSplashToPool(splash);
    }

    /// <summary>
    /// Returns a splash GameObject back to the object pool.
    /// </summary>
    /// <param name="splash">The splash GameObject to return.</param>
    private void ReturnSplashToPool(GameObject splash) {
        splash.SetActive(false);
        _splashPool.Enqueue(splash);
    }
}
