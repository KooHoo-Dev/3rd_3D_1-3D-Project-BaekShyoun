using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HareAndTortoise
{
    public enum SoundEffect
    {
        UIClick, DialogueNext, RaceStart, GrenadeEquip, GrenadeThrow,
        LandminePlace, ItemUnavailable, HareHit, HareLand, HareGetUp,
    }

    // 씬마다 같은 프리팹을 배치한다. 첫 매니저만 유지해 씬 이동 중 클릭음도 끊기지 않게 한다.
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("BGM")]
        [SerializeField] private AudioClip titleNormal;
        [SerializeField] private AudioClip titleDark;
        [SerializeField] private AudioClip story;
        [SerializeField] private AudioClip raceNormal;
        [SerializeField] private AudioClip raceDark;

        [Header("UI And Race")]
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip dialogueNext;
        [SerializeField] private AudioClip raceStart;
        [SerializeField] private AudioClip victory;
        [SerializeField] private AudioClip defeat;

        [Header("Items")]
        [SerializeField] private AudioClip grenadeEquip;
        [SerializeField] private AudioClip grenadeThrow;
        [SerializeField] private AudioClip landminePlace;
        [SerializeField] private AudioClip itemUnavailable;

        [Header("Hare")]
        [SerializeField] private AudioClip hareAnimalHop;
        [SerializeField] private AudioClip hareHumanoidFootsteps;
        [SerializeField] private AudioClip hareHit;
        [SerializeField] private AudioClip hareLand;
        [SerializeField] private AudioClip hareGetUp;

        [Header("Tortoise")]
        [SerializeField] private AudioClip tortoiseAnimalFootsteps;
        [SerializeField] private AudioClip tortoiseHumanoidFootsteps;

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] private float effectsVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float footstepsVolume = 0.35f;

        private AudioSource musicSource;
        private AudioSource effectsSource;
        private AudioSource uiSource;
        private AudioSource hareFootstepsSource;
        private AudioSource tortoiseFootstepsSource;
        private HareController hare;
        private TortoiseController tortoise;
        private RaceManager race;
        private int loadedSceneHandle = int.MinValue;
        private bool useHumanoid;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            musicSource = CreateSource(true, 128);
            effectsSource = CreateSource(false, 96);
            uiSource = CreateSource(false, 64);
            hareFootstepsSource = CreateSource(true, 160);
            tortoiseFootstepsSource = CreateSource(true, 160);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private AudioSource CreateSource(bool loop, int priority)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.priority = priority;
            return source;
        }

        private void Start()
        {
            // 실행 중 별도로 생성됐을 때도 현재 씬을 적용한다.
            if (Instance == this && loadedSceneHandle != SceneManager.GetActiveScene().handle)
                ApplyScene(SceneManager.GetActiveScene());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyScene(scene);
        }

        private void ApplyScene(Scene scene)
        {
            loadedSceneHandle = scene.handle;
            hareFootstepsSource.Stop();
            tortoiseFootstepsSource.Stop();
            effectsSource.Stop();
            race = FindFirstObjectByType<RaceManager>();
            hare = race != null ? FindFirstObjectByType<HareController>() : null;
            tortoise = race != null ? FindFirstObjectByType<TortoiseController>() : null;
            useHumanoid = race != null && race.CanAttack;

            AudioClip music;
            musicSource.volume = musicVolume;
            if (scene.name == "90. Story") music = story;
            else if (race != null) music = useHumanoid ? raceDark : raceNormal;
            else music = SceneController.IsVillain ? titleDark : titleNormal;

            // 타이틀과 선택 화면에서 같은 곡이면 끊지 않고 이어서 재생한다.
            if (musicSource.clip != music || musicSource.isPlaying == false)
            {
                musicSource.Stop();
                musicSource.clip = music;
                if (music != null) musicSource.Play();
            }

            // 프리팹에 씬 오브젝트를 연결하지 않고, 각 씬의 버튼에 한 번만 등록한다.
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].onClick.AddListener(PlayUIClick);
        }

        private void Update()
        {
            if (Instance != this) return;
            musicSource.volume = musicVolume;
            effectsSource.volume = effectsVolume;
            uiSource.volume = uiVolume;
            hareFootstepsSource.volume = footstepsVolume;
            tortoiseFootstepsSource.volume = footstepsVolume;

            bool canPlaySteps = race != null && race.IsRaceFinished == false;
            UpdateFootsteps(hareFootstepsSource,
                useHumanoid ? hareHumanoidFootsteps : hareAnimalHop,
                canPlaySteps && hare != null && hare.IsMoving);
            UpdateFootsteps(tortoiseFootstepsSource,
                useHumanoid ? tortoiseHumanoidFootsteps : tortoiseAnimalFootsteps,
                canPlaySteps && tortoise != null && tortoise.IsMoving);
        }

        private void UpdateFootsteps(AudioSource source, AudioClip clip, bool isMoving)
        {
            if (isMoving == false || clip == null)
            {
                if (source.isPlaying) source.Stop();
                return;
            }
            if (source.clip != clip)
            {
                source.Stop();
                source.clip = clip;
            }
            if (source.isPlaying == false) source.Play();
        }

        private void PlayUIClick()
        {
            Play(SoundEffect.UIClick);
        }

        public static void Play(SoundEffect effect)
        {
            if (Instance == null) return;
            AudioClip clip;
            switch (effect)
            {
                case SoundEffect.UIClick: clip = Instance.uiClick; break;
                case SoundEffect.DialogueNext: clip = Instance.dialogueNext; break;
                case SoundEffect.RaceStart: clip = Instance.raceStart; break;
                case SoundEffect.GrenadeEquip: clip = Instance.grenadeEquip; break;
                case SoundEffect.GrenadeThrow: clip = Instance.grenadeThrow; break;
                case SoundEffect.LandminePlace: clip = Instance.landminePlace; break;
                case SoundEffect.ItemUnavailable: clip = Instance.itemUnavailable; break;
                case SoundEffect.HareHit: clip = Instance.hareHit; break;
                case SoundEffect.HareLand: clip = Instance.hareLand; break;
                case SoundEffect.HareGetUp: clip = Instance.hareGetUp; break;
                default: return;
            }
            if (clip == null) return;
            bool isUI = effect == SoundEffect.UIClick || effect == SoundEffect.DialogueNext ||
                        effect == SoundEffect.ItemUnavailable;
            AudioSource source = isUI ? Instance.uiSource : Instance.effectsSource;
            source.volume = isUI ? Instance.uiVolume : Instance.effectsVolume;
            source.PlayOneShot(clip);
        }

        public static void PlayResult(bool success)
        {
            if (Instance == null) return;
            Instance.musicSource.Stop();
            Instance.hareFootstepsSource.Stop();
            Instance.tortoiseFootstepsSource.Stop();
            AudioClip clip = success ? Instance.victory : Instance.defeat;
            if (clip != null)
            {
                Instance.uiSource.volume = Instance.uiVolume;
                Instance.uiSource.PlayOneShot(clip);
            }
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }
}
