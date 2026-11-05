using UnityEngine;
using UnityEngine.UI;

public class FarmShopCategories : MonoBehaviour
{
    public GameObject[] pages;
    public Button[] tabs;
    public int SelectedCategory { get; private set; }
    void OnEnable() { Select(0); }
    public void Animals() { Select(0); }
    public void Fertilizer() { Select(1); }
    public void Fruit() { Select(2); }
    public void Home() { Select(3); }
    public void Tools() { Select(4); }
    public void Select(int index)
    {
        if (pages == null || index < 0 || index >= pages.Length) return;
        SelectedCategory = index;
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i]) pages[i].SetActive(i == index);
            if (tabs != null && i < tabs.Length && tabs[i])
            {
                var image = tabs[i].GetComponent<Image>();
                if (image) image.color = i == index ? new Color(.42f,.64f,.28f) : new Color(.17f,.27f,.21f);
            }
        }
    }
}
