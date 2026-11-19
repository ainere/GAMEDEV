using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Showcase
{
    /// <summary>Tab cycles through the showcase scenes; F1, F2 and F3 jump straight to one. H hides or shows the hotkey help.</summary>
    public sealed class ShowcaseSceneSwitcher : MonoBehaviour
    {
        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.hKey.wasPressedThisFrame)
                ShowcaseHud.Visible = !ShowcaseHud.Visible;

            int count = SceneManager.sceneCountInBuildSettings;
            if (count == 0)
                return;
            int current = SceneManager.GetActiveScene().buildIndex;

            if (keyboard.tabKey.wasPressedThisFrame)
                SceneManager.LoadScene((current + 1) % count);
            else if (keyboard.f1Key.wasPressedThisFrame && count > 0)
                SceneManager.LoadScene(0);
            else if (keyboard.f2Key.wasPressedThisFrame && count > 1)
                SceneManager.LoadScene(1);
            else if (keyboard.f3Key.wasPressedThisFrame && count > 2)
                SceneManager.LoadScene(2);
        }
    }
}
