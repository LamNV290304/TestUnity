using UnityEngine;

public class BarItemController : MonoBehaviour
{
    private IdenticalBarController m_barController;
    private Item m_item;

    public void Setup(IdenticalBarController barController, Item item)
    {
        m_barController = barController;
        m_item = item;
    }

    private void OnMouseDown()
    {
        if (m_barController != null && m_item != null)
        {
            m_barController.ReturnItemToBoard(m_item);
        }
    }
}