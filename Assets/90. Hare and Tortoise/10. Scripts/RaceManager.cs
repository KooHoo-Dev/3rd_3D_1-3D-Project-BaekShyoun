using System.Threading;
using Study.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;

namespace HareAndTortoise
{
    public enum StageType
    {
        TutorialA,
        TutorialB,
        TutorialC,
        Stage1,
    }
    
    public class RaceManager : MonoBehaviour
    {
        public static RaceManager Instance { get; private set; }

        [Header("Stage Settings")]
        [SerializeField] private StageType stageType = StageType.TutorialC;

        public bool IsStage1 => stageType == StageType.Stage1;
        public bool CanSleep { get; private set; } = true;
        public bool CanAttack { get; private set; } = true;
        public bool HasPreparation { get; private set; } = true;

        [Header("Stage1 Spawn Settings")]
        [SerializeField] private Transform hareTransform;
        [SerializeField] private Transform tortoiseTransform;
        [SerializeField] private Transform hareSpawnPoint;
        [SerializeField] private Transform tortoiseSpawnPoint;
        
        public bool canStartTortoise { get; private set; }
        public bool canStartHare { get; private set; }

        public bool IsPreParing { get; private set; } = true;

        [Header("Prepare Settings")]
        [SerializeField] private float prepareDuration = 60f;
        public float PreparationRemaining { get; private set; }
        private bool isRequestToStart;
        private CancellationToken destroyToken;
        
        [Header("Cameras")]
        [SerializeField] private CinemachineCamera[] cameras;
        [SerializeField] private CinemachineBrain mainCam;

        [Header("ItemCounts")]
        [SerializeField] private int grenadeCount = 3;
        [SerializeField] private int landmineCount = 3;
        [SerializeField] private UIController itemUI;

        public int GrenadeCount => grenadeCount;
        public int LandmineCount => landmineCount;
        public bool IsRaceFinished => canDecideWinner;
        public bool CanSkipPreparation => HasPreparation && IsPreParing &&
            isRequestToStart == false && canDecideWinner == false;
        
        
        // 승리 결정
        private bool canDecideWinner = false;
        private string winnerName;

        
        private void OnTriggerEnter(Collider other)
        {
            if (canDecideWinner) return;
            
            if (other.CompareTag("Tortoise"))
            {
                Debug.Log("거북이 결승선 진입 감지");
                if (IsStage1)
                {
                    TortoiseController tortoise = other.GetComponentInParent<TortoiseController>();
                    if (tortoise == null || tortoise.HasDoneRoute == false) return;
                }
                Debug.Log($"거북이 승리!");
                itemUI.ShowResult(IsStage1
                    ? "거북이 승리!\n스테이지 선택으로 돌아가시겠습니까?"
                    : "거북이 승리!\n계속 진행하시겠습니까?");
                canStartHare = false;
                canDecideWinner = true;
                winnerName = "거북이";
                StageProgress.MarkCleared(stageType);
                SoundManager.PlayResult(true);
            }
            // 래그돌의 팔다리 Collider에도 부모의 토끼를 찾아 결승 판정을 적용한다.
            HareController hare = other.GetComponentInParent<HareController>();
            if (hare != null)
            {
                if (IsStage1)
                {
                    if (hare.HasDoneRoute == false && hare.CanFinishInFlight == false) return;
                }
                Debug.Log($"토끼 승리!");
                itemUI.ShowResult(IsStage1
                    ? "토끼 승리!\n스테이지 선택으로 돌아가시겠습니까?"
                    : stageType == StageType.TutorialA
                    ? "토끼 승리!\n다음 이야기로 진행하시겠습니까?" : "토끼 승리!\n다시 도전하시겠습니까?");
                canStartTortoise = false;
                canDecideWinner = true;
                winnerName = "토끼";
                // TutorialA는 토끼 승리가 정상적인 튜토리얼 완료 조건이다.
                if (stageType == StageType.TutorialA) StageProgress.MarkCleared(stageType);
                SoundManager.PlayResult(stageType == StageType.TutorialA);
            }
        }

        private void Awake()
        {
            Instance = this;
            destroyToken = this.GetCancellationTokenOnDestroy();
            CheckStage();
            PreparationRemaining = prepareDuration;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            canStartTortoise = false;
            canStartHare = false;

            if (stageType == StageType.Stage1)
            {
                hareTransform.GetComponent<HareController>().PlaceAtSpawn(hareSpawnPoint);
                tortoiseTransform.GetComponent<TortoiseController>().PlaceAtSpawn(tortoiseSpawnPoint);
            }
            
            itemUI.ShowRaceText(HasPreparation ? null : "Enter를 눌러 경주 시작");
            
            IsPreParing = HasPreparation;
            if (HasPreparation)
            {
                // Stage1의 스폰 배치를 마친 실제 위치를 기준으로 준비 카메라를 맞춘다.
                PrepCameraController preparationCamera = cameras[0].GetComponent<PrepCameraController>();
                if (preparationCamera != null) preparationCamera.FocusOnRacers();
                SelectCamera(0);
                StartCountDown().Forget();
            }
            else SelectCamera(1);

            if (stageType == StageType.TutorialA)
                itemUI.ShowTutorialSubtitles("WASD로 이동하고 마우스를 회전시키며 카메라 시점을 제어하세요.");
            else if (stageType == StageType.TutorialC)
                itemUI.ShowTutorialSubtitles(
                    "우클릭으로 맵을 이동하고, 스크롤로 ZoomIn/Out을 할 수 있습니다.",
                    "좌클릭으로 토끼가 갈만한 곳에 지뢰를 설치하세요.");
        }

        public void NotifyHareSleeping()
        {
            if (stageType == StageType.TutorialB && canDecideWinner == false)
                itemUI.ShowTutorialSubtitles("토끼가 잠을 자기 시작했습니다! 이때 서둘러 결승선을 통과하세요!");
        }

        private void Update()
        {
            if (HasPreparation == false && SimpleInput.GetKeyDown(Key.Enter)) RequestToStart();
            
            // 이건 진짜 임시. 대충 초기화 로직임
            if (SimpleInput.GetKeyDown(Key.Digit0)) SceneManager.LoadScene(0);
            if (SimpleInput.GetKeyDown(Key.Digit1)) SceneManager.LoadScene(1);
        }

        public bool TryUseGrenade()
        {
            if (grenadeCount <= 0)
            {
                SoundManager.Play(SoundEffect.ItemUnavailable);
                return false;
            }
            grenadeCount--;
            itemUI.RefreshCounts();
            return true;
        }
        
        public bool TryUseLandmine()
        {
            if (landmineCount <= 0)
            {
                SoundManager.Play(SoundEffect.ItemUnavailable);
                return false;
            }
            landmineCount--;
            itemUI.RefreshCounts();
            return true;
        }

        private void CheckStage()
        {
            switch (stageType)
            {
                case StageType.TutorialA:
                    CanSleep = false;
                    CanAttack = false;
                    HasPreparation = false;
                    break;
                case StageType.TutorialB:
                    CanSleep = true;
                    CanAttack = false;
                    HasPreparation = false;
                    break;
                case StageType.TutorialC or StageType.Stage1:
                    CanSleep = false;
                    CanAttack = true;
                    HasPreparation = true;
                    break;
            }
        }

        private void LoadStage()
        {
            switch (stageType)
            {
                case StageType.TutorialA:
                    SceneController.PlayStory("BeforeTutorialB");
                    break;
                case StageType.TutorialB:
                    SceneController.PlayStory("BeforeTutorialC");
                    break;
                case StageType.TutorialC:
                    SceneController.PlayStory("AfterTutorialC");
                    break;
            }
        }
        
        public void SkipPreparation()
        {
            if (CanSkipPreparation == false) return;
            RequestToStart();
        }

        private void RequestToStart()
        {
            if (isRequestToStart || canDecideWinner) return;
            
            isRequestToStart = true;
            if (HasPreparation) return;

            StartTutorialCountdown().Forget();
        }

        private async UniTask StartTutorialCountdown()
        {
            // A·B는 출발 전 3초 동안 이동과 출발음을 모두 보류한다.
            for (int count = 3; count > 0; count--)
            {
                itemUI.ShowRaceText(count.ToString());
                await UniTask.WaitForSeconds(1f, cancellationToken: destroyToken);
            }
            await BeginRace();
        }
        
        private async UniTask StartCountDown()
        {
            float remainTime = prepareDuration;
            while (remainTime > 0f && isRequestToStart == false)
            {
                PreparationRemaining = Mathf.Max(0f, remainTime);
                await UniTask.Yield(cancellationToken: destroyToken);
                remainTime -= Time.deltaTime;
            }

            PreparationRemaining = 0f;
            IsPreParing = false;
            
            SelectCamera(1);
            await UniTask.NextFrame(cancellationToken: destroyToken);
            await UniTask.WaitUntil(() => mainCam.IsBlending == false, cancellationToken: destroyToken);

            await BeginRace();
        }

        private async UniTask BeginRace()
        {
            // 네 씬 모두 Start 표시·출발음 이후에 이동을 허용한다.
            itemUI.ShowRaceText("Start!");
            SoundManager.Play(SoundEffect.RaceStart);
            canStartTortoise = true;
            canStartHare = true;
            if (stageType == StageType.TutorialC)
                itemUI.ShowTutorialSubtitles(
                    "우클릭을 누른 채 F키를 누르면 수류탄을 던질 수 있습니다.",
                    "수류탄을 통해 토끼를 방해하세요.");
            await UniTask.WaitForSeconds(1f, cancellationToken: destroyToken);
            itemUI.ShowRaceText(null);
        }

        private void SelectCamera(int index)
        {
            if (index < 0 || index >= cameras.Length) return;
            for (int i = 0; i < cameras.Length; i++)
            {
                cameras[i].Priority = (i == index) ? 20 : 10;
            }
        }
        
        public void OnResultYes()
        {
            if (canDecideWinner == false) return;

            if (IsStage1)
            {
                // Stage1은 승패와 관계없이 선택 화면으로 돌아간다.
                SceneManager.LoadScene("70. Stage");
            }
            else if (stageType == StageType.TutorialA)
            {
                LoadStage();
            }
            else if (winnerName == "토끼")
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            else
            {
                LoadStage();
            }
        }

        public void OnResultNo()
        {
            if (canDecideWinner == false) return;

            SceneManager.LoadScene(IsStage1 ? "80. Title" : "70. Stage");
        }
    }
}
