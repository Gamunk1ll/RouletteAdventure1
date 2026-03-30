using UnityEngine;
using UnityEngine.Rendering;

public class Menu : MonoBehaviour
{
    public GameObject menuPanel;

    public Volume postProcessVolume;
    public float blurAmount = 1f;

    private bool isMenuOpen = false;

    void Start()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (isMenuOpen)
            OpenMenu();
        else
            CloseMenu();
    }

    void OpenMenu()
    {
        if (menuPanel != null)
            menuPanel.SetActive(true);

        if (postProcessVolume != null)
            postProcessVolume.weight = blurAmount;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseMenu()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (postProcessVolume != null)
            postProcessVolume.weight = 0f;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnPlayButton()
    {
        CloseMenu();
    }

    public void OnQuitButton()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}