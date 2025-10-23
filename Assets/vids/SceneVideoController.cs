using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class SceneVideoController : MonoBehaviour
{
    [Header("Clips")]
    public VideoClip introClip;
    public VideoClip outroClip;

    [Header("Behaviour")]
    public bool playIntroOnStart = false;
    public float introStartDelay = 0.5f;
    public bool allowAnyInputToSkip = false;

    [Header("Audio/Sync")]
    public bool useAudioSourceOutput = true;
    public bool syncToAudioDSP = true;

    [Header("UI Layer (child panel with RawImage)")]
    public GameObject videoLayer;
    public RawImage videoSurface;
    public RenderTexture renderTexture;

    [Header("Components (auto if null)")]
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    bool prepared, playing, createdRT;

    void Awake()
    {
        Application.runInBackground = true;

        if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>() ?? gameObject.AddComponent<VideoPlayer>();

        if (useAudioSourceOutput)
        {
            if (!audioSource) audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        }

        if (videoLayer == gameObject)
        {
            Debug.LogWarning("videoLayer must be a CHILD panel, not this Canvas/controller object.");
            videoLayer = null;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.skipOnDrop = true;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.timeUpdateMode = syncToAudioDSP ? VideoTimeUpdateMode.DSPTime : VideoTimeUpdateMode.GameTime;

        if (useAudioSourceOutput)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audioSource);
            videoPlayer.controlledAudioTrackCount = 1;
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        }

        EnsureRenderTarget();

        videoPlayer.prepareCompleted += OnPrepared;
        videoPlayer.loopPointReached += OnFinished;
        videoPlayer.errorReceived += OnError;

        SetLayerVisible(false);
    }

    void Start()
    {
        if (playIntroOnStart && introClip) StartCoroutine(AutoIntro());
    }

    IEnumerator AutoIntro()
    {
        yield return new WaitForSecondsRealtime(introStartDelay);
        PlayIntro();
    }

    void Update()
    {
        if (!playing || !allowAnyInputToSkip) return;

        bool any =
            Input.anyKeyDown ||
            Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) ||
            Input.GetKeyDown(KeyCode.JoystickButton0) ||
            Input.GetKeyDown(KeyCode.JoystickButton1) ||
            Input.GetKeyDown(KeyCode.JoystickButton7) ||
            Input.GetKeyDown(KeyCode.Escape) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return);

        if (any) SkipOrStop();
    }

    void OnDestroy()
    {
        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.loopPointReached -= OnFinished;
        videoPlayer.errorReceived -= OnError;

        if (createdRT && renderTexture)
        {
            if (renderTexture.IsCreated()) renderTexture.Release();
            Destroy(renderTexture);
        }
    }

    // Public API
    public void PlayIntro() => PlayClip(introClip);
    public void PlayOutro() => PlayClip(outroClip);

    public void PlayClip(VideoClip clip)
    {
        if (!clip) { Debug.LogWarning("PlayClip: null clip."); return; }
        StopAllCoroutines();
        StartCoroutine(CoPlay(clip));
    }

    public void SkipOrStop()
    {
        if (!playing) { SetLayerVisible(false); return; }
        playing = false;
        if (videoPlayer.isPlaying) videoPlayer.Stop();
        if (useAudioSourceOutput && audioSource && audioSource.isPlaying) audioSource.Stop();
        SetLayerVisible(false);
    }

    // Internals
    IEnumerator CoPlay(VideoClip clip)
    {
        EnsureRenderTarget();
        SetLayerVisible(true);
        BringLayerToFront();

        prepared = false;
        playing = false;

        videoPlayer.Stop();
        if (useAudioSourceOutput && audioSource) audioSource.Stop();

        videoPlayer.clip = clip;
        videoPlayer.Prepare();

        float t = 0f;
        const float TIMEOUT = 10f;
        while (!prepared && t < TIMEOUT)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!prepared)
        {
            Debug.LogError("Video prepare timeout.");
            SetLayerVisible(false);
            yield break;
        }

        playing = true;
        if (useAudioSourceOutput && audioSource) audioSource.Play();
        videoPlayer.Play();
    }

    void OnPrepared(VideoPlayer vp) => prepared = true;

    void OnFinished(VideoPlayer vp)
    {
        playing = false;
        SetLayerVisible(false);
    }

    void OnError(VideoPlayer vp, string msg)
    {
        Debug.LogError("Video error: " + msg);
        playing = false;
        SetLayerVisible(false);
    }

    void EnsureRenderTarget()
    {
        if (!renderTexture)
        {
            int w = (videoPlayer.clip && videoPlayer.clip.width > 0) ? (int)videoPlayer.clip.width : Screen.width;
            int h = (videoPlayer.clip && videoPlayer.clip.height > 0) ? (int)videoPlayer.clip.height : Screen.height;
            renderTexture = new RenderTexture(w, h, 0);
            renderTexture.Create();
            createdRT = true;
        }

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;

        if (videoSurface) videoSurface.texture = renderTexture;
    }

    void SetLayerVisible(bool on)
    {
        if (videoLayer) videoLayer.SetActive(on);
        else if (videoSurface) videoSurface.enabled = on;
    }

    void BringLayerToFront()
    {
        if (!videoSurface) return;
        videoSurface.transform.SetAsLastSibling();
    }
}
