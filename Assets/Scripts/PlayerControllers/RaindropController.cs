using UnityEngine;

public class RaindropController : MonoBehaviour {
    [SerializeField] private GameObject _raindropSplashPrefab;
    private ParticleSystem _particleSystem;

    private void Start() {
        TimeAndWeatherManager.Instance.OnChangeRainIntensity += OnChangeRainIntensity;
        _particleSystem = GetComponent<ParticleSystem>();
    }

    private void OnChangeRainIntensity(int intensity) {
        var emission = _particleSystem.emission;
        emission.rateOverTime = intensity;
    }

    private void Update() {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[_particleSystem.particleCount];
        _particleSystem.GetParticles(particles);

        for (int i = 0; i < particles.Length; i++) {
            if (IsRaindropExpired(particles[i])) {
                Vector3 position = particles[i].position;
                GameObject splashInstance = Instantiate(_raindropSplashPrefab, position, Quaternion.identity);

                float animationLength = GetAnimationLength(splashInstance);
                Destroy(splashInstance, animationLength + 0.6f);

                particles[i].remainingLifetime = -1f;
            }
        }

        _particleSystem.SetParticles(particles, particles.Length);
    }

    private bool IsRaindropExpired(ParticleSystem.Particle particle) {
        return particle.remainingLifetime <= 0.1f;
    }

    private float GetAnimationLength(GameObject obj) {
        Animator animator = obj.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null) {
            return animator.runtimeAnimatorController.animationClips[0].length;
        }
        return 0f;
    }
}

