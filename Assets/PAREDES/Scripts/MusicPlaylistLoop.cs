using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlaylistLoop : MonoBehaviour
{
    public AudioClip trackA;
    public AudioClip trackB;

    AudioSource _source;
    Coroutine _routine;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = false;
    }

    void OnEnable()
    {
        var dir = GetComponent<WallsAudioDirector>();
        if (dir != null && dir != WallsAudioDirector.Instance)
            return;
        if (trackA == null && trackB == null)
            return;
        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(PlayAlternating());
    }

    void OnDisable()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    IEnumerator PlayAlternating()
    {
        var index = 0;
        while (enabled)
        {
            var clip = index % 2 == 0 ? trackA : trackB;
            if (clip == null)
                clip = trackA != null ? trackA : trackB;
            if (clip == null)
                yield break;

            _source.clip = clip;
            _source.loop = false;
            _source.Play();
            WallsAudioDirector.Instance?.RefreshVolumes();

            yield return new WaitForSecondsRealtime(clip.length);
            index++;
        }
    }
}
