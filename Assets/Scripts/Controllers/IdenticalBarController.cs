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

            item.View.DOMove(worldPos, moveDuration).SetEase(Ease.OutQuad);

            item.View.DOScale(0.7f, moveDuration);
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

        if (!matchFound && m_itemsInBar.Count >= slots.Count)
        {
            Debug.Log("GAME OVER!");
        }

        m_isChecking = false;

    }
}