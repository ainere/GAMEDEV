using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LaserCorridor.Editor
{
    // Scene Visibility affects the editor view only. The runtime Canvas stays active and enabled.
    [InitializeOnLoad]
    public static class HudSceneVisibility
    {
        const string ScenePath = "Assets/LaserCorridor/Scenes/LaserCorridor.unity";
        const string MenuPath = "Laser Corridor/Show HUD in Scene view";
        static bool applying;
        static GameObject trackedHud;
        static bool trackedVisible;

        static string PreferenceKey => "LaserCorridor.HudVisibleInScene." +
            Hash128.Compute(Application.dataPath.Replace('\\', '/').TrimEnd('/').ToLowerInvariant());

        public static bool VisibleInScene => EditorPrefs.GetBool(PreferenceKey, false);

        static HudSceneVisibility()
        {
            EditorApplication.delayCall += ApplyLoadedScenes;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaved += Apply;
            SceneVisibilityManager.visibilityChanged += RememberRootVisibility;
        }

        [MenuItem(MenuPath)]
        static void Toggle()
        {
            SetVisibleInScene(!VisibleInScene);
        }

        [MenuItem(MenuPath, true)]
        static bool ValidateToggle()
        {
            Menu.SetChecked(MenuPath, VisibleInScene);
            return !EditorApplication.isPlayingOrWillChangePlaymode && FindLoadedHud();
        }

        public static void SetVisibleInScene(bool visible)
        {
            EditorPrefs.SetBool(PreferenceKey, visible);
            ApplyLoadedScenes();
        }

        // Batch verification changes only editor visibility and restores the user's preference.
        public static void VerifyHudSceneVisibility()
        {
            bool hadPreference = EditorPrefs.HasKey(PreferenceKey);
            bool originalPreference = VisibleInScene;
            var hud = FindLoadedHud();
            Scene loadedForCheck = default;
            try
            {
                if (!hud)
                {
                    loadedForCheck = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                    hud = FindHud(loadedForCheck);
                }
                void Require(bool condition, string description)
                {
                    if (!condition) throw new System.InvalidOperationException("HUD Scene visibility check failed: " + description);
                }

                Require(hud, "saved HUD exists under Interface");
                var canvas = hud.GetComponent<Canvas>();
                Require(canvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay, "saved Canvas is Screen Space Overlay");
                Require(hud.gameObject.activeSelf && hud.gameObject.activeInHierarchy && canvas.enabled && canvas.rootCanvas.enabled,
                    "saved HUD is active and its root Canvas is enabled");
                bool originalActive = hud.gameObject.activeSelf;
                bool originalHierarchyActive = hud.gameObject.activeInHierarchy;
                bool originalCanvasEnabled = canvas.enabled;
                bool originalRootCanvasEnabled = canvas.rootCanvas.enabled;
                RenderMode originalRenderMode = canvas.renderMode;
                void RequireRuntimeUnchanged()
                {
                    Require(hud.gameObject.activeSelf == originalActive && hud.gameObject.activeInHierarchy == originalHierarchyActive &&
                        canvas.enabled == originalCanvasEnabled && canvas.rootCanvas.enabled == originalRootCanvasEnabled &&
                        canvas.renderMode == originalRenderMode, "editor visibility leaves runtime HUD activation and Canvas unchanged");
                }

                SetVisibleInScene(false);
                Require(!VisibleInScene && SceneVisibilityManager.instance.IsHidden(hud.gameObject, true),
                    "Hide remembers the setting and hides the HUD root and all descendants");
                RequireRuntimeUnchanged();
                SetVisibleInScene(true);
                Require(VisibleInScene && !SceneVisibilityManager.instance.IsHidden(hud.gameObject, false) &&
                    SceneVisibilityManager.instance.AreAllDescendantsVisible(hud.gameObject),
                    "Show remembers the setting and reveals the HUD root and all descendants");
                RequireRuntimeUnchanged();
            }
            finally
            {
                if (hadPreference) EditorPrefs.SetBool(PreferenceKey, originalPreference);
                else EditorPrefs.DeleteKey(PreferenceKey);
                if (hud) Apply(hud.gameObject.scene);
                if (loadedForCheck.IsValid() && loadedForCheck.isLoaded) EditorSceneManager.CloseScene(loadedForCheck, true);
            }
            if (EditorPrefs.HasKey(PreferenceKey) != hadPreference || VisibleInScene != originalPreference)
                throw new System.InvalidOperationException("HUD Scene visibility check failed: original project preference was not restored.");
            Debug.Log("LASER_CORRIDOR_HUD_SCENE_VISIBILITY_VERIFIED: overlay HUD and descendants hide/show only in Scene view; " +
                "runtime activation and Canvas remain enabled; project preference restored.");
        }

        static void OnSceneOpened(Scene scene, OpenSceneMode mode) => Apply(scene);

        static CorridorHud FindHud(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath) return null;
            var matches = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CorridorHud>(true))
                .Where(hud => hud.transform.parent && hud.transform.parent.name == "Interface").ToArray();
            return matches.Length == 1 ? matches[0] : null;
        }

        static CorridorHud FindLoadedHud()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var hud = FindHud(SceneManager.GetSceneAt(i));
                if (hud) return hud;
            }
            return null;
        }

        static void ApplyLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++) Apply(SceneManager.GetSceneAt(i));
        }

        public static void Apply(Scene scene)
        {
            var hud = FindHud(scene);
            if (!hud) return;
            applying = true;
            try
            {
                var visibility = SceneVisibilityManager.instance;
                if (VisibleInScene) visibility.Show(hud.gameObject, true);
                else visibility.Hide(hud.gameObject, true);
                trackedHud = hud.gameObject;
                trackedVisible = !visibility.IsHidden(trackedHud, false);
            }
            finally
            {
                applying = false;
            }
            SceneView.RepaintAll();
        }

        static void RememberRootVisibility()
        {
            if (applying || !trackedHud || trackedHud.scene.path != ScenePath ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool visible = !SceneVisibilityManager.instance.IsHidden(trackedHud, false);
            // Child-eye changes and visibility changes elsewhere do not change the project preference.
            if (visible == trackedVisible) return;
            trackedVisible = visible;
            EditorPrefs.SetBool(PreferenceKey, visible);
        }
    }
}
