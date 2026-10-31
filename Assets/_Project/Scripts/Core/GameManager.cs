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
        private const float StrongEndingFlame = 45f;

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
            BeginNewGame();
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
            if (visible)
                menu.ShowPause();
            else
                menu.Hide();
        }

        public void ShowEnd(string message)
        {
            gameEnded = true;
            objectiveChanged.Raise(string.Empty);
            endLabel.text = message;
            endPanel.SetActive(true);
            saveSystem.Delete();
        }

        private void ShowMap()
        {
            // Час на мапі стоїть, тому вибір маршруту нічого не коштує.
            stateMachine.Change(new SceneState(this, MapState, "RouteMap", 0f, false));
            objectiveChanged.Raise("Оберіть наступний пункт маршруту: натисніть помаранчевий прямокутник");
            saveSystem.Save();
        }

        private void OnNodeSelected(RouteNode node) =>
            stateMachine.Change(new SceneState(this, RideState, "Bus", 1f, true));

        private void OnArrived()
        {
            RouteNode node = route.CompleteTravel();
            string stopScene = node.Type.StopScene();

            if (node.Type == NodeType.Final)
                stateMachine.Change(new EndState(this, BuildEnding()));
            else if (stopScene != null)
            {
                stateMachine.Change(new SceneState(this, StopState, stopScene, 1f, false));
                objectiveChanged.Raise("Шукайте предмети в кімнатах. Щоб поїхати, поверніться до дверей ліворуч і натисніть E. Темрява наближається!");
            }
            else if (node.Type == NodeType.Event)
                stateMachine.Change(new EventState(this, (runner, finished) => runner.RunRandom(finished)));
            else
                ShowMap();
        }

        private static string BuildEnding()
        {
            // Кінцівка залежить від того, скільки сили лишилося у Вогника наприкінці дороги.
            if (GameContext.Flame.Current >= StrongEndingFlame)
            {
                return "Кінцева зупинка.\nВогник сходить із сидіння й торкається дроту. Світло біжить лініями, " +
                       "спалахують ліхтарі, вікна, а тоді й зорі.\nНіч скінчилася.";
            }

            return "Кінцева зупинка.\nВогника ледве видно. Він віддає останню іскру дротам, і вздовж вулиці " +
                   "повільно займається один-єдиний ліхтар.\nАле це вже не темрява. Хтось піде на це світло.";
        }

        private void OnFlameExtinguished()
        {
            if (!gameEnded)
                stateMachine.Change(new EndState(this, "Вогник згас. Ніч лишилася назавжди."));
        }

        private void ReturnToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(0);
        }
    }
}
