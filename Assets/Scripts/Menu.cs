using System.Reflection;
using UnityEngine;

public class MenuBlurController : MonoBehaviour
{
    public GameObject menuPanel;

    public Component postProcessVolume;
    public float blurAmount = 1f;

    private PropertyInfo _volumeWeightProperty;
    private bool isMenuOpen;

    private void Start()
    {
        CacheVolumeWeightProperty();

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }

    private void Update()
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
        {
            OpenMenu();
            return;
        }

        CloseMenu();
    }

    private void OpenMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }

        SetVolumeWeight(blurAmount);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        SetVolumeWeight(0f);

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

    private void CacheVolumeWeightProperty()
    {
        if (postProcessVolume == null)
        {
            return;
        }

        _volumeWeightProperty = postProcessVolume.GetType().GetProperty("weight", BindingFlags.Public | BindingFlags.Instance);

        if (_volumeWeightProperty == null || _volumeWeightProperty.PropertyType != typeof(float))
        {
            _volumeWeightProperty = null;
        }
    }

    private void SetVolumeWeight(float value)
    {
        if (postProcessVolume == null)
        {
            return;
        }

        if (_volumeWeightProperty == null)
        {
            CacheVolumeWeightProperty();
            if (_volumeWeightProperty == null)
            {
                return;
            }
        }

        _volumeWeightProperty.SetValue(postProcessVolume, value);
    }
}
