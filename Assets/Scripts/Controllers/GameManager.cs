using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public static GameManager Instance { get; private set; }
    public enum eLevelMode
    {
        TIMER,
        MOVES
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
        GAME_WIN
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;

            StateChangedAction(m_state);
        }
    }


    private GameSettings m_gameSettings;


    private BoardController m_boardController;

    private UIMainManager m_uiMenu;

    private LevelCondition m_levelCondition;
    private IdenticalBarController m_barController; 
    private Coroutine m_autoplayRoutine;
    private bool m_isAutoplay = false;

    private void Awake()
    {
        Instance = this;
        State = eStateGame.SETUP;

        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);

        m_uiMenu = FindObjectOfType<UIMainManager>();
        m_uiMenu.Setup(this);
    }

    void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_boardController != null) m_boardController.Update();
    }


    internal void SetState(eStateGame state)
    {
        State = state;

        if(State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
        }
        else
        {
            DOTween.PlayAll();
        }
    }

    public void LoadLevel(eLevelMode mode)
    {
        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        IdenticalBarController barController = m_uiMenu.GetIdenticalBarController();
        m_boardController.StartGame(this, m_gameSettings, barController);
        m_barController = barController;

        m_barController.Setup(mode);

        if (mode == eLevelMode.MOVES)
        {
            m_levelCondition = this.gameObject.AddComponent<LevelMoves>();
            m_levelCondition.Setup(m_gameSettings.LevelMoves, m_uiMenu.GetLevelConditionView(), m_boardController);
        }
        else if (mode == eLevelMode.TIMER)
        {
            m_levelCondition = this.gameObject.AddComponent<LevelTime>();
            m_levelCondition.Setup(m_gameSettings.LevelTime, m_uiMenu.GetLevelConditionView(), this);
        }

        m_levelCondition.ConditionCompleteEvent += GameOver;
        m_boardController.OnMoveEvent += CheckForWin;

        State = eStateGame.GAME_STARTED;
    }

    public void GameOver()
    {
        StartCoroutine(WaitBoardController());
    }

    internal void ClearLevel()
    {
        if (m_uiMenu != null)
        {
            IdenticalBarController barController = m_uiMenu.GetIdenticalBarController();
            if (barController != null)
            {
                barController.ClearBar(); 
            }
        }
        m_isAutoplay = false;
        if (m_autoplayRoutine != null)
        {
            StopCoroutine(m_autoplayRoutine);
            m_autoplayRoutine = null;
        }
        if (m_boardController)
        {
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }
    }

    private IEnumerator WaitBoardController()
    {
        while (m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        State = eStateGame.GAME_OVER;

        if (m_levelCondition != null)
        {
            if (m_boardController != null)
            {
                m_boardController.OnMoveEvent -= CheckForWin;
            }

            m_levelCondition.ConditionCompleteEvent -= GameOver;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }

    private void CheckForWin()
    {
        if (m_boardController == null) return;

        if (m_boardController.CheckIfBoardIsClear())
        {
            m_boardController.SetBusy(true);

            SetState(eStateGame.GAME_WIN);
        }
    }

    public void StartAutoplay(eLevelMode mode, bool autoWin)
    {
        if (mode != eLevelMode.MOVES)
        {
            Debug.LogError("Autoplay chỉ hỗ trợ chế độ MOVES!");
            return;
        }

        m_isAutoplay = true;

        LoadLevel(mode);

        m_autoplayRoutine = StartCoroutine(AutoplayRoutine(autoWin));
    }

    private IEnumerator AutoplayRoutine(bool autoWin)
    {
        yield return new WaitForSeconds(1.0f);

        while (m_state == eStateGame.GAME_STARTED && m_isAutoplay)
        {
            yield return new WaitForSeconds(0.5f);

            if (m_boardController == null) yield break;
            yield return new WaitUntil(() => !m_boardController.IsBusy);

            Cell cellToClick = null;
            if (autoWin)
            {
                cellToClick = FindBestMoveToWin();
            }
            else
            {
                cellToClick = FindBestMoveToLose();
            }

            if (cellToClick != null)
            {
                m_boardController.OnCellClicked(cellToClick);
            }
            else
            {
                Debug.Log("AUTOPLAY: Không tìm thấy nước đi. Dừng lại.");
                m_isAutoplay = false;
                break;
            }
        }
    }

    private Cell FindBestMoveToWin()
    {
        List<Cell> availableCells = m_boardController.GetAvailableCells(); 
        if (availableCells.Count == 0) return null;

        List<Item> itemsInBar = m_barController.GetItemsInBar(); 

        var barCounts = itemsInBar
            .OfType<NormalItem>() 
            .GroupBy(item => item.ItemType) 
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var cell in availableCells)
        {
            NormalItem item = cell.Item as NormalItem; 
            if (item == null) continue;

            if (barCounts.ContainsKey(item.ItemType) && barCounts[item.ItemType] == 2)
            {
                return cell; 
            }
        }

        foreach (var cell in availableCells)
        {
            NormalItem item = cell.Item as NormalItem;
            if (item == null) continue;

            if (barCounts.ContainsKey(item.ItemType) && barCounts[item.ItemType] == 1)
            {
                return cell; 
            }
        }

        return availableCells[0];
    }

    private Cell FindBestMoveToLose()
    {
        List<Cell> availableCells = m_boardController.GetAvailableCells();
        if (availableCells.Count == 0) return null;

        List<Item> itemsInBar = m_barController.GetItemsInBar();

        var barCounts = itemsInBar
            .OfType<NormalItem>()
            .GroupBy(item => item.ItemType)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var cell in availableCells)
        {
            NormalItem item = cell.Item as NormalItem;
            if (item == null) continue;

            if (!barCounts.ContainsKey(item.ItemType))
            {
                return cell;
            }
        }

        foreach (var cell in availableCells)
        {
            NormalItem item = cell.Item as NormalItem;
            if (item == null) continue;

            if (barCounts.ContainsKey(item.ItemType) && barCounts[item.ItemType] == 1)
            {
                return cell;
            }
        }

        return availableCells[0];
    }


}
