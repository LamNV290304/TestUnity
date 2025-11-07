using System; 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening; 

public class IdenticalBarController : MonoBehaviour
{

    private Camera m_mainCamera;

    public List<Transform> slots;

    public float moveDuration = 0.2f;

    public event Action OnMatchMadeEvent = delegate { };

    private List<Item> m_itemsInBar = new List<Item>();

    private bool m_isChecking = false;
    private bool m_isTimerMode = false;
    void Start()
    {
        m_mainCamera = Camera.main;
    }
    public bool AddItem(Item item)
    {
        if (m_itemsInBar.Count >= slots.Count)
        {
            return false;
        }

        m_itemsInBar.Add(item);

        if (m_isTimerMode && item.View != null)
        {
            var collider = item.View.gameObject.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one; 
            collider.isTrigger = true; 

            var clicker = item.View.gameObject.AddComponent<BarItemController>();
            clicker.Setup(this, item);
        }
        m_itemsInBar = m_itemsInBar
            .OrderBy(it => (it as NormalItem)?.ItemType ?? 0)
            .ToList();

        RearrangeBarVisuals();

        StartCoroutine(CheckForMatchesDelayed());

        return true;
    }

    private void RearrangeBarVisuals()
    {
        if (m_mainCamera == null)
        {
            m_mainCamera = Camera.main;
            if (m_mainCamera == null)
            {
                Debug.LogError("Main Camera!");
                return;
            }
        }

        for (int i = 0; i < m_itemsInBar.Count; i++)
        {
            Item item = m_itemsInBar[i];
            Transform targetSlot = slots[i]; 

            Vector3 screenPos = targetSlot.position;

            Vector3 worldPos = m_mainCamera.ScreenToWorldPoint(screenPos);

            worldPos.z = 0;

            item.View.DOMove(worldPos, moveDuration).SetEase(Ease.OutBack);

            item.View.DOScale(0.7f, moveDuration).SetEase(Ease.OutBack);
        }
    }


    private IEnumerator CheckForMatchesDelayed()
    {
        if (m_isChecking) yield break;
        m_isChecking = true;

        yield return new WaitForSeconds(moveDuration + 0.1f);

        bool matchFound = false;

        var groups = m_itemsInBar
            .Where(it => it is NormalItem)
            .Cast<NormalItem>()
            .GroupBy(item => item.ItemType);

        foreach (var group in groups)
        {
            if (group.Count() >= 3)
            {
                matchFound = true;
                List<Item> itemsToExplode = group.Take(3).Cast<Item>().ToList();

                foreach (Item item in itemsToExplode)
                {
                    m_itemsInBar.Remove(item);

                    item.ExplodeView(); 
                }

                OnMatchMadeEvent();

                break;
            }
        }


        if (matchFound)
        {
            yield return new WaitForSeconds(0.2f);
            RearrangeBarVisuals();


        }

        if (!m_isTimerMode && !matchFound && m_itemsInBar.Count >= slots.Count)
        {
            GameManager.Instance.SetState(GameManager.eStateGame.GAME_OVER);
        }

        m_isChecking = false;

    }

    public void ClearBar()
    {
        foreach (Item item in m_itemsInBar)
        {
            if (item != null)
            {
                item.Clear(); 
            }
        }

        m_itemsInBar.Clear();

        m_isChecking = false;
    }

    public List<Item> GetItemsInBar()
    {
        return m_itemsInBar;
    }

    public void Setup(GameManager.eLevelMode mode)
    {
        m_isTimerMode = (mode == GameManager.eLevelMode.TIMER);
    }

    public void ReturnItemToBoard(Item itemToReturn)
    {
        if (itemToReturn == null) return;

        Cell originalCell = itemToReturn.Cell;

        if (originalCell != null && originalCell.IsEmpty) 
        {
            m_itemsInBar.Remove(itemToReturn);

            originalCell.Assign(itemToReturn); 

            if (itemToReturn.View != null)
            {
                var clicker = itemToReturn.View.GetComponent<BarItemController>();
                if (clicker != null) Destroy(clicker);

                var collider = itemToReturn.View.GetComponent<BoxCollider2D>();
                if (collider != null) Destroy(collider);

                itemToReturn.View.DOMove(originalCell.transform.position, moveDuration).SetEase(Ease.OutQuad);
                itemToReturn.View.DOScale(1.0f, moveDuration).SetEase(Ease.OutQuad);
            }

            RearrangeBarVisuals();
        }
        else
        {
            Debug.Log("No");
        }
    }
}