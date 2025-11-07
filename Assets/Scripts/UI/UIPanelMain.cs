using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelMain : MonoBehaviour, IMenu
{
    [SerializeField] private Button btnAutoWin;
    [SerializeField] private Button btnAutoLose;
    [SerializeField] private Button btnTimer;
    [SerializeField] private Button btnMoves;

    private UIMainManager m_mngr;

    private void Awake()
    {
        btnAutoWin.onClick.AddListener(OnClickAutoWin);
        btnAutoLose.onClick.AddListener(OnClickAutoLose);
        btnMoves.onClick.AddListener(OnClickMoves);
        btnTimer.onClick.AddListener(OnClickTimer);
    }

    private void OnDestroy()
    {
        if (btnAutoWin) btnAutoWin.onClick.RemoveAllListeners();
        if (btnAutoLose) btnAutoLose.onClick.RemoveAllListeners();
        if (btnMoves) btnMoves.onClick.RemoveAllListeners();
        if (btnTimer) btnTimer.onClick.RemoveAllListeners();
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    private void OnClickTimer()
    {
        m_mngr.LoadLevelTimer();
    }

    private void OnClickMoves()
    {
        m_mngr.LoadLevelMoves();
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

    private void OnClickAutoWin()
    {
        m_mngr.StartAutoplayWin();
    }

    private void OnClickAutoLose()
    {
        m_mngr.StartAutoplayLose();
    }
}
