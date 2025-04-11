using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "IntroStepVideo", menuName = "Intro/Step/Video")]
public class IntroStepVideo : IntroStepBase {
    [Tooltip("Unity Video Player (in the canvas or RawImage).")]
    [SerializeField] private VideoPlayer _videoPlayerPrefab;

    public override IEnumerator RunStep(GameObject context) {
        var _videoPlayer = Instantiate(_videoPlayerPrefab, context.transform);
        _videoPlayer.Play();

        while (_videoPlayer.isPlaying) {
            if (CanSkip && InputManager.Instance.SkipPressed()) {
                _videoPlayer.Stop();
                break;
            }
            yield return null;
        }

        Destroy(_videoPlayer.gameObject);
    }
}