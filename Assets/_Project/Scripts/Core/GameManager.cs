using NightTrolleybus.Core.Events;
using NightTrolleybus.Core.StateMachine;
using NightTrolleybus.Narrative;
using NightTrolleybus.Route;
using NightTrolleybus.Save;
using NightTrolleybus.UI;
using NightTrolleybus.Vehicle.Repair;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NightTrolleybus.Core
{
    /// <summary>Керує циклом гри: мапа, поїздка, зупинка, збереження й кінець партії.</summary>
    public class GameManager : MonoBehaviour
    {
        private const string MapState = "Map";
        private const string RideState = "Ride";
        private const string StopState = "Stop";
        private const string EventStateName = "Event";
        private const string MenuStateName = "Menu";
        private const string CutsceneStateName = "Cutscene";
        private const string EndStateName = "End";
        private const float LowPower = 0.5f;

        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private RouteManager route;
        [SerializeField] private SaveSystem saveSystem;
        [SerializeField] private RoadEventRunner roadEvents;

        [Header("Канали подій")]
        [SerializeField] private VoidEventChannel flameExtinguished;
        [SerializeField] private VoidEventChannel arrivedAtNode;
        [SerializeField] private VoidEventChannel stopCompleted;
        [SerializeField] private BoolEventChannel inBusChanged;
        [SerializeField] private StringEventChannel gameStateChanged;
        [SerializeField] private StringEventChannel noticeRaised;
        [SerializeField] private StringEventChannel objectiveChanged;

        [Header("Інтерфейс")]
        [SerializeField] private GameMenuView menu;
        [SerializeField] private CutsceneView cutscene;
        [SerializeField] private CutsceneView endingCutscene;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private Text endLabel;
        [SerializeField] private Button restartButton;

        private readonly GameStateMachine stateMachine = new GameStateMachine();
        private PauseState pauseState;
        private bool gameEnded;
        private bool inMenu;

        private void Awake()
        {
            pauseState = new PauseState(this);
            endPanel.SetActive(false);
            restartButton.onClick.AddListener(() => GameContext.Audio?.Play(SoundId.UiClick));
            restartButton.onClick.AddListener(ReturnToMenu);
        }

        private void OnEnable()
        {
            flameExtinguished.Raised += OnFlameExtinguished;
            arrivedAtNode.Raised += OnArrived;
            stopCompleted.Raised += ShowMap;
            route.NodeSelected += OnNodeSelected;
            menu.StartRequested += StartNewGame;
            menu.ContinueRequested += ContinueGame;
            menu.ResumeRequested += stateMachine.Resume;
            menu.ExitToMenuRequested += ReturnToMenu;
        }

        private void OnDisable()
        {
            flameExtinguished.Raised -= OnFlameExtinguished;
            arrivedAtNode.Raised -= OnArrived;
            stopCompleted.Raised -= ShowMap;
            route.NodeSelected -= OnNodeSelected;
            menu.StartRequested -= StartNewGame;
            menu.ContinueRequested -= ContinueGame;
            menu.ResumeRequested -= stateMachine.Resume;
            menu.ExitToMenuRequested -= ReturnToMenu;
        }

        private void Start()
        {
            // Партія починається лише після вибору в меню, а доти час стоїть.
            inMenu = true;
            Time.timeScale = 0f;
            gameStateChanged.Raise(MenuStateName);
            menu.ShowMain(saveSystem.HasSave);
        }

        private void StartNewGame()
        {
            saveSystem.Delete();
            // Катсцена лише на новій грі: «Продовжити» одразу веде на мапу.
            menu.Hide();
            gameStateChanged.Raise(CutsceneStateName);
            cutscene.Play(BeginNewGame);
        }

        private void ContinueGame()
        {
            if (!saveSystem.TryLoad())
            {
                BeginNewGame();
                return;
            }

            LeaveMenu();
            ShowMap();
        }

        private void LeaveMenu()
        {
            inMenu = false;
            menu.Hide();
        }

        private void BeginNewGame()
        {
            LeaveMenu();

            // Нова гра: історія показується поверх мапи, час стоїть.
            route.Generate(Random.Range(int.MinValue, int.MaxValue));
            sceneLoader.Load("RouteMap");
            gameStateChanged.Raise(EventStateName);
            stateMachine.Change(new EventState(this, (runner, finished) => runner.RunIntro(finished)));
        }

        public void RunEvent(System.Action<RoadEventRunner, System.Action> run)
        {
            gameStateChanged.Raise(EventStateName);
            run(roadEvents, ShowMap);
        }

        public void CloseEvent() => roadEvents.Close();

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (inMenu || gameEnded || keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;

            // У міні-грі ремонту Esc означає «відійти», а не паузу.
            RepairHost repairHost = FindAnyObjectByType<RepairHost>();
            if (repairHost != null && (repairHost.IsOpen || repairHost.LastClosedFrame == Time.frameCount))
                return;

            if (stateMachine.IsPaused)
                stateMachine.Resume();
            else
                stateMachine.Pause(pauseState);
        }

        public void EnterScene(string stateName, string sceneName, bool inBus)
        {
            inBusChanged.Raise(inBus);
            gameStateChanged.Raise(stateName);
            sceneLoader.Load(sceneName);
        }

        public void SetPauseVisible(bool visible)
        {
            GameContext.Audio?.SetPaused(visible);
            if (visible)
                menu.ShowPause();
            else
                menu.Hide();
        }

        public void ShowEnd(string message)
        {
            gameEnded = true;
            gameStateChanged.Raise(EndStateName);
            objectiveChanged.Raise(string.Empty);
            endLabel.text = message;
            restartButton.GetComponentInChildren<Text>().text = Loc.T("У головне меню");
            endPanel.SetActive(true);
            saveSystem.Delete();
        }

        private void ShowMap()
        {
            // Час на мапі стоїть, тому вибір маршруту нічого не коштує.
            stateMachine.Change(new SceneState(this, MapState, "RouteMap", 0f, false));
            objectiveChanged.Raise(BuildMapObjective());
            saveSystem.Save();
        }

        private string BuildMapObjective()
        {
            // Підказка на мапі лишається лише для нестачі енергії: шкал тут не видно.
            if (GameContext.Power.Normalized >= LowPower)
                return string.Empty;

            foreach (RouteNode node in route.Nodes)
            {
                if (node.Type == NodeType.Depot && route.IsAvailable(node))
                    return Loc.T("Мало енергії! Їдьте в депо (зелений прямокутник): там зарядна станція");
            }

            return Loc.T("Мало енергії, а депо зараз недоступне. Вимкніть обігрівач і оберіть шлях до депо");
        }

        private void OnNodeSelected(RouteNode node) =>
            stateMachine.Change(new SceneState(this, RideState, "Bus", 1f, true));

        private void OnArrived()
        {
            RouteNode node = route.CompleteTravel();
            string stopScene = node.Type.StopScene();

            if (node.Type == NodeType.Final)
                PlayEnding();
            else if (stopScene != null)
            {
                stateMachine.Change(new SceneState(this, StopState, stopScene, 1f, false));
                objectiveChanged.Raise(node.Type == NodeType.Depot
                    ? Loc.T("Спершу заряд: шафа із синім світлом поруч із входом (E). Потім предмети й назад до дверей ліворуч")
                    : Loc.T("Шукайте предмети в кімнатах. Щоб поїхати, поверніться до дверей ліворуч і натисніть E. Темрява наближається!"));
            }
            else if (node.Type == NodeType.Event)
                stateMachine.Change(new EventState(this, (runner, finished) => runner.RunRandom(finished)));
            else
                ShowMap();
        }

        private void PlayEnding()
        {
            // Час стоїть, щоб Вогник не згас під час відео, а пауза не відкрилася поверх нього.
            Time.timeScale = 0f;
            inMenu = true;
            gameStateChanged.Raise(CutsceneStateName);
            endingCutscene.Play(() =>
            {
                // Партію пройдено: у меню вже нема чого продовжувати.
                saveSystem.Delete();
                ReturnToMenu();
            });
        }

        private void OnFlameExtinguished()
        {
            if (gameEnded)
                return;

            GameContext.Audio?.Play(SoundId.Lose);
            stateMachine.Change(new EndState(this, Loc.T("Вогник згас. Ніч лишилася назавжди.")));
        }

        private void ReturnToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }
}
