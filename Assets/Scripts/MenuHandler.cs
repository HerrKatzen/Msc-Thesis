using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuHandler : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> menu;
    private GameObject lastActiveMenuItem = null;
    public void SetMenu(GameObject menuItem)
    {
        foreach (var m in menu)
        {
            if (m == menuItem) m.SetActive(true);
            else m.SetActive(false);
        }
    }

    public void SetMenuNONE()
    {
        foreach (var m in menu)
        {
            if (m.activeInHierarchy) lastActiveMenuItem = m;
            m.SetActive(false);
        }
    }

    public void ResetMenu()
    {
        SetMenu(lastActiveMenuItem);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
