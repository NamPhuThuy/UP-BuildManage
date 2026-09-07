// ───────────────────────────────────────────────────────────────────────
// RULES:
// 1. PROCESS: Use Debug.Log for trace steps.
// 2. SAFETY: Use Debug.LogError in null/boundary checks.
// 3. ENUM FORMAT: If used enum, use the format:
//    public enum Type
//    {
//        NONE = 0, TYPE_1 = 1, TYPE_2 = 2
//    }
// 4. STRINGS: Use 'private const string' for resource paths, settings keys, and default folder paths.
// 5. DIALOGS: Use Debug.LogError (or Debug.LogWarning) instead of EditorUtility.DisplayDialog for editor errors/warnings.
// 6. FOLDERS: For fields representing folder paths, use 'DefaultAsset' fields to allow dragging and dropping folders instead of using simple string fields.
// 7. CACHING: Provide a 'Reset to Defaults' button in the options panel calling a method named 'ResetToDefaults()' to clear/override cached or persisted EditorPrefs values that might become stale or invalid.
// 8. LISTS: When resetting list fields, avoid re-instantiating them if they are not null. Clear them instead to prevent issues with serialized property bindings.
// 9. NOTIFICATIONS: Reduce to use addition window to notify information, just Debug.Log it with color and method name prefix.
// 10. LOGGING CONCISENESS: Keep Debug.Log text short and focused mainly on keywords (e.g., "OnEnable", "Action 1: Start", "Success", "ResetToDefaults") to ensure maximum readability and zero clutter.
// 11. IN-MEMORY RESET: When resetting cached keys in ResetToDefaults(), ensure you also clear or re-initialize the corresponding in-memory fields (e.g., set to default asset or null). Otherwise, OnDisable() will re-save the old in-memory values back to EditorPrefs when the window closes to reload.
// ───────────────────────────────────────────────────────────────────────

using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using NamPhuThuy.Common;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
#endif

namespace Doors.BuildManage
{
#if UNITY_EDITOR
    /// <summary>
    /// Android Build Setup & Keystore Management Editor Window built with UI Toolkit.
    /// Provides quick 1-click keystore configuration, password persistence across editor sessions,
    /// and core Android build settings management.
    /// </summary>
    public class Window_BuildSetup_Android : EditorWindow
    {
        #region Enums (Rule 3)
        public enum TabType
        {
            NONE = 0,
            KEYSTORE = 1,
            BUILD_CONFIG = 2,
            OPTIONS = 3
        }
        #endregion

        #region Private Fields
        // EditorPrefs keys (Rule 4)
        private const string PREF_KEY_KEYSTORE_PATH = "Doors_BuildSetup_Android_KeystorePath";
        private const string PREF_KEY_KEYSTORE_PASS = "Doors_BuildSetup_Android_KeystorePass";
        private const string PREF_KEY_KEYALIAS_NAME = "Doors_BuildSetup_Android_KeyaliasName";
        private const string PREF_KEY_KEYALIAS_PASS = "Doors_BuildSetup_Android_KeyaliasPass";
        private const string PREF_KEY_ACTIVE_TAB = "Doors_BuildSetup_Android_ActiveTab";
        private const string PREF_KEY_AUTO_APPLY_ON_ENABLE = "Doors_BuildSetup_Android_AutoApplyOnEnable";

        // Titles & Branding (Rule 4)
        private const string WINDOW_TITLE = "Build Setup - Android";
        private const string MENU_ITEM_PATH = "Doors/Build Manage/Window - Build Setup - Android";
        private const string LOG_PREFIX = "<color=#3B82F6>[Window_BuildSetup_Android]</color>";

        // Theme colors (Expression-bodied properties for 0 static memory allocation)
        private static Color COLOR_EDITOR_BG => new Color(0.22f, 0.22f, 0.22f, 1f);
        private static Color COLOR_GREY_BOX => new Color(0.16f, 0.16f, 0.16f, 0.6f);
        private static Color COLOR_GREY_BORDER => new Color(0.26f, 0.26f, 0.26f, 0.8f);
        private static Color COLOR_OCEAN_BLUE => new Color(0.0f, 0.47f, 0.74f, 1f);
        private static Color COLOR_SKY_BLUE => new Color(0.53f, 0.8f, 0.92f, 1f);
        private static Color COLOR_FOREST_MIST => new Color(0.8f, 0.8f, 0.8f, 1f);
        private static Color COLOR_TAB_INACTIVE_BG => new Color(0.16f, 0.16f, 0.16f, 1f);
        private static Color COLOR_TAB_INACTIVE_BORDER => new Color(0.11f, 0.11f, 0.11f, 1f);
        private static Color COLOR_DANGER_BG => new Color(0.55f, 0.15f, 0.15f, 1f);
        private static Color COLOR_DANGER_BORDER => new Color(0.6f, 0.2f, 0.2f, 0.8f);
        private static Color COLOR_SUCCESS_GREEN => new Color(0.18f, 0.65f, 0.35f, 1f);

        // Keystore state
        [SerializeField] private string _keystorePath = "";
        [SerializeField] private string _keystorePass = "";
        [SerializeField] private string _keyaliasName = "";
        [SerializeField] private string _keyaliasPass = "";
        [SerializeField] private bool _autoApplyOnEnable = false;

        // UI state
        private TabType _activeTab = TabType.KEYSTORE;
        private bool _showPasswords = false;

        // Visual Element references
        private VisualElement _contentContainer;
        private VisualElement _tabHeaderContainer;
        private Label _statusLabel;
        #endregion

        #region Menu Item
        [MenuItem(MENU_ITEM_PATH)]
        public static void ShowWindow()
        {
            var window = GetWindow<Window_BuildSetup_Android>(WINDOW_TITLE);
            window.minSize = new Vector2(500, 680);
            window.Show();
        }
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            Debug.Log($"{LOG_PREFIX} OnEnable"); // Rule 10 (Keywords only)

            // Load persisted settings
            _keystorePath = EditorPrefs.GetString(PREF_KEY_KEYSTORE_PATH, PlayerSettings.Android.keystoreName);
            _keystorePass = EditorPrefs.GetString(PREF_KEY_KEYSTORE_PASS, "");
            _keyaliasName = EditorPrefs.GetString(PREF_KEY_KEYALIAS_NAME, PlayerSettings.Android.keyaliasName);
            _keyaliasPass = EditorPrefs.GetString(PREF_KEY_KEYALIAS_PASS, "");
            _activeTab = (TabType)EditorPrefs.GetInt(PREF_KEY_ACTIVE_TAB, (int)TabType.KEYSTORE);
            _autoApplyOnEnable = EditorPrefs.GetBool(PREF_KEY_AUTO_APPLY_ON_ENABLE, false);

            if (_autoApplyOnEnable && !string.IsNullOrEmpty(_keystorePath))
            {
                ApplyKeystoreToPlayerSettings(silent: true);
            }
        }

        private void OnDisable()
        {
            Debug.Log($"{LOG_PREFIX} OnDisable"); // Rule 10 (Keywords only)

            // Save persisted settings
            EditorPrefs.SetString(PREF_KEY_KEYSTORE_PATH, _keystorePath);
            EditorPrefs.SetString(PREF_KEY_KEYSTORE_PASS, _keystorePass);
            EditorPrefs.SetString(PREF_KEY_KEYALIAS_NAME, _keyaliasName);
            EditorPrefs.SetString(PREF_KEY_KEYALIAS_PASS, _keyaliasPass);
            EditorPrefs.SetInt(PREF_KEY_ACTIVE_TAB, (int)_activeTab);
            EditorPrefs.SetBool(PREF_KEY_AUTO_APPLY_ON_ENABLE, _autoApplyOnEnable);
        }

        public void CreateGUI()
        {
            Debug.Log($"{LOG_PREFIX} CreateGUI"); // Rule 10 (Keywords only)

            var root = rootVisualElement;
            root.style.backgroundColor = COLOR_EDITOR_BG;
            root.style.paddingLeft = 14;
            root.style.paddingRight = 14;
            root.style.paddingTop = 14;
            root.style.paddingBottom = 14;

            // 1. Header Row
            root.Add(BuildHeader());

            // 2. Navigation Tabs
            _tabHeaderContainer = BuildNavigation();
            root.Add(_tabHeaderContainer);

            // Separator
            var separator = new VisualElement
            {
                style =
                {
                    height = 2,
                    backgroundColor = COLOR_GREY_BORDER,
                    marginTop = 4,
                    marginBottom = 12
                }
            };
            root.Add(separator);

            // 3. Scrollable content container
            _contentContainer = new ScrollView(ScrollViewMode.Vertical)
            {
                style = { flexGrow = 1 }
            };
            root.Add(_contentContainer);

            RefreshRegion();
        }
        #endregion

        #region Header & Navigation
        private VisualElement BuildHeader()
        {
            var headerRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingBottom = 10,
                    marginBottom = 8,
                    borderBottomWidth = 1,
                    borderBottomColor = COLOR_GREY_BORDER
                }
            };

            // Android Icon / Logo Box
            var iconBox = new VisualElement
            {
                style =
                {
                    width = 44,
                    height = 44,
                    marginRight = 12,
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                    backgroundColor = COLOR_GREY_BOX,
                    justifyContent = Justify.Center,
                    alignItems = Align.Center
                }
            };
            var iconLabel = new Label("🤖") { style = { fontSize = 22 } };
            iconBox.Add(iconLabel);
            headerRow.Add(iconBox);

            // Title and Subtitle
            var textColumn = new VisualElement { style = { flexGrow = 1 } };
            var mainTitle = new Label("Android Build & Keystore Setup")
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = 16,
                    color = COLOR_SKY_BLUE
                }
            };
            var subTitle = new Label("Doors / Module Build Manage")
            {
                style =
                {
                    fontSize = 11,
                    color = COLOR_FOREST_MIST,
                    unityFontStyleAndWeight = FontStyle.Normal
                }
            };
            textColumn.Add(mainTitle);
            textColumn.Add(subTitle);
            headerRow.Add(textColumn);

            return headerRow;
        }

        private VisualElement BuildNavigation()
        {
            var bar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart
                }
            };

            bar.Add(CreateNavigationButton("Keystore", TabType.KEYSTORE));
            bar.Add(CreateNavigationButton("Build Settings (wip)", TabType.BUILD_CONFIG));
            bar.Add(CreateNavigationButton("Options / Presets (wip)", TabType.OPTIONS));

            return bar;
        }

        private Button CreateNavigationButton(string label, TabType tab)
        {
            bool isActive = _activeTab == tab;
            var btn = new Button(() => SwitchRegion(tab))
            {
                text = label,
                style =
                {
                    flexGrow = 1,
                    height = 28,
                    fontSize = 12,
                    marginLeft = 2,
                    marginRight = 2,
                    unityFontStyleAndWeight = isActive ? FontStyle.Bold : FontStyle.Normal,
                    backgroundColor = isActive ? COLOR_OCEAN_BLUE : COLOR_TAB_INACTIVE_BG,
                    color = isActive ? Color.white : COLOR_FOREST_MIST,
                    borderTopWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderRightWidth = 1,
                    borderTopColor = isActive ? COLOR_SKY_BLUE : COLOR_TAB_INACTIVE_BORDER,
                    borderBottomColor = isActive ? COLOR_SKY_BLUE : COLOR_TAB_INACTIVE_BORDER,
                    borderLeftColor = isActive ? COLOR_SKY_BLUE : COLOR_TAB_INACTIVE_BORDER,
                    borderRightColor = isActive ? COLOR_SKY_BLUE : COLOR_TAB_INACTIVE_BORDER,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };

            return btn;
        }

        private void SwitchRegion(TabType newTab)
        {
            if (_activeTab == newTab) return;
            _activeTab = newTab;

            var parent = _tabHeaderContainer.parent;
            int siblingIdx = parent.IndexOf(_tabHeaderContainer);
            parent.Remove(_tabHeaderContainer);
            _tabHeaderContainer = BuildNavigation();
            parent.Insert(siblingIdx, _tabHeaderContainer);

            RefreshRegion();
        }

        private void RefreshRegion()
        {
            _contentContainer.Clear();

            switch (_activeTab)
            {
                case TabType.KEYSTORE:
                    BuildKeystoreTab(_contentContainer);
                    break;
                case TabType.BUILD_CONFIG:
                    BuildBuildConfigTab(_contentContainer);
                    break;
                case TabType.OPTIONS:
                    BuildOptionsTab(_contentContainer);
                    break;
            }
        }
        #endregion

        #region Tab 1: Keystore Page
        private void BuildKeystoreTab(VisualElement container)
        {
            // 1. Current Live Status Box
            var statusBox = UITKEditorHelper.BuildBox("Current Project Status");
            statusBox.style.backgroundColor = COLOR_GREY_BOX;
            statusBox.style.borderTopColor = COLOR_GREY_BORDER;
            statusBox.style.borderBottomColor = COLOR_GREY_BORDER;
            statusBox.style.borderLeftColor = COLOR_GREY_BORDER;
            statusBox.style.borderRightColor = COLOR_GREY_BORDER;

            bool isCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            string activeKeystoreName = PlayerSettings.Android.keystoreName;
            string activeAliasName = PlayerSettings.Android.keyaliasName;
            bool hasPass = !string.IsNullOrEmpty(PlayerSettings.Android.keystorePass);
            bool hasAliasPass = !string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass);

            string statusText = isCustomKeystore
                ? $"✓ Custom Keystore: ENABLED\n" +
                  $"• Path: {(string.IsNullOrEmpty(activeKeystoreName) ? "<Not set>" : activeKeystoreName)}\n" +
                  $"• Alias: {(string.IsNullOrEmpty(activeAliasName) ? "<Not set>" : activeAliasName)}\n" +
                  $"• Keystore Pass: {(hasPass ? "●●●●●● (Set)" : "Empty / Cleared by Unity")}\n" +
                  $"• Alias Pass: {(hasAliasPass ? "●●●●●● (Set)" : "Empty / Cleared by Unity")}"
                : "⚠ Custom Keystore: DISABLED (Using Default Debug Key)";

            _statusLabel = new Label(statusText)
            {
                style =
                {
                    fontSize = 12,
                    color = isCustomKeystore ? COLOR_SKY_BLUE : Color.yellow,
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 4
                }
            };
            statusBox.Add(_statusLabel);
            container.Add(statusBox);

            // 2. Keystore Credentials Setup Box
            var credBox = UITKEditorHelper.BuildBox("Keystore Credentials");
            credBox.style.backgroundColor = COLOR_GREY_BOX;
            credBox.style.borderTopColor = COLOR_GREY_BORDER;
            credBox.style.borderBottomColor = COLOR_GREY_BORDER;
            credBox.style.borderLeftColor = COLOR_GREY_BORDER;
            credBox.style.borderRightColor = COLOR_GREY_BORDER;

            // Keystore Path + Browse Row
            var pathRow = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            var pathField = new TextField("Keystore File")
            {
                value = _keystorePath,
                style = { flexGrow = 1 }
            };
            pathField.RegisterValueChangedCallback(e =>
            {
                _keystorePath = e.newValue;
            });
            pathRow.Add(pathField);

            var browseBtn = new Button(BrowseKeystoreFile)
            {
                text = "Browse...",
                style =
                {
                    width = 75,
                    height = 22,
                    marginLeft = 6,
                    fontSize = 11,
                    backgroundColor = COLOR_OCEAN_BLUE,
                    color = Color.white,
                    borderTopLeftRadius = 3,
                    borderTopRightRadius = 3,
                    borderBottomLeftRadius = 3,
                    borderBottomRightRadius = 3
                }
            };
            pathRow.Add(browseBtn);
            credBox.Add(pathRow);

            // Keystore Password
            var keystorePassField = new TextField("Keystore Pass")
            {
                value = _keystorePass,
                isPasswordField = !_showPasswords,
                style = { marginBottom = 6 }
            };
            keystorePassField.RegisterValueChangedCallback(e =>
            {
                _keystorePass = e.newValue;
            });
            credBox.Add(keystorePassField);

            // Key Alias Name
            var aliasField = new TextField("Key Alias Name")
            {
                value = _keyaliasName,
                style = { marginBottom = 6 }
            };
            aliasField.RegisterValueChangedCallback(e =>
            {
                _keyaliasName = e.newValue;
            });
            credBox.Add(aliasField);

            // Key Alias Password
            var aliasPassField = new TextField("Key Alias Pass")
            {
                value = _keyaliasPass,
                isPasswordField = !_showPasswords,
                style = { marginBottom = 8 }
            };
            aliasPassField.RegisterValueChangedCallback(e =>
            {
                _keyaliasPass = e.newValue;
            });
            credBox.Add(aliasPassField);

            // Show/Hide Password Toggle
            var toggleShowPass = new Toggle("Show Passwords") { value = _showPasswords };
            toggleShowPass.RegisterValueChangedCallback(e =>
            {
                _showPasswords = e.newValue;
                keystorePassField.isPasswordField = !_showPasswords;
                aliasPassField.isPasswordField = !_showPasswords;
            });
            credBox.Add(toggleShowPass);

            container.Add(credBox);

            // 3. Actions Box
            var actionBox = UITKEditorHelper.BuildBox("Actions");
            actionBox.style.backgroundColor = COLOR_GREY_BOX;
            actionBox.style.borderTopColor = COLOR_GREY_BORDER;
            actionBox.style.borderBottomColor = COLOR_GREY_BORDER;
            actionBox.style.borderLeftColor = COLOR_GREY_BORDER;
            actionBox.style.borderRightColor = COLOR_GREY_BORDER;

            // Apply Button
            var applyBtn = new Button(() => ApplyKeystoreToPlayerSettings(silent: false))
            {
                text = "⚡ Apply Keystore to PlayerSettings",
                style =
                {
                    height = 36,
                    fontSize = 13,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = COLOR_SUCCESS_GREEN,
                    color = Color.white,
                    marginBottom = 6,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            actionBox.Add(applyBtn);

            // Open Build Profiles Button
            var openProfilesBtn = new Button(OpenBuildProfiles)
            {
                text = "📁 Open Build Profiles",
                style =
                {
                    height = 28,
                    fontSize = 11,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = COLOR_OCEAN_BLUE,
                    color = Color.white,
                    marginBottom = 6,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            actionBox.Add(openProfilesBtn);

            // Load & Clear Row
            var buttonRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };

            var loadBtn = new Button(LoadFromPlayerSettings)
            {
                text = "Read from Project",
                style =
                {
                    flexGrow = 1,
                    height = 28,
                    marginRight = 3,
                    fontSize = 11,
                    backgroundColor = COLOR_OCEAN_BLUE,
                    color = Color.white,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            buttonRow.Add(loadBtn);

            var clearBtn = new Button(ClearKeystoreFromPlayerSettings)
            {
                text = "Disable Custom Keystore",
                style =
                {
                    flexGrow = 1,
                    height = 28,
                    marginLeft = 3,
                    fontSize = 11,
                    backgroundColor = COLOR_DANGER_BG,
                    color = Color.white,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            buttonRow.Add(clearBtn);

            actionBox.Add(buttonRow);
            container.Add(actionBox);
        }

        private void BrowseKeystoreFile()
        {
            string startDir = !string.IsNullOrEmpty(_keystorePath) ? Path.GetDirectoryName(_keystorePath) : Application.dataPath;
            string selectedPath = EditorUtility.OpenFilePanel("Select Android Keystore", startDir, "keystore,jks");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _keystorePath = selectedPath.Replace("\\", "/");
                Debug.Log($"{LOG_PREFIX} Selected Keystore: {_keystorePath}");
                RefreshRegion();
            }
        }

        private void ApplyKeystoreToPlayerSettings(bool silent = false)
        {
            if (string.IsNullOrEmpty(_keystorePath))
            {
                Debug.LogError($"{LOG_PREFIX} Error: Keystore path is empty.");
                return;
            }

            if (!File.Exists(_keystorePath))
            {
                Debug.LogWarning($"{LOG_PREFIX} Warning: Keystore file does not exist on disk at path: {_keystorePath}");
            }

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = _keystorePath;
            PlayerSettings.Android.keystorePass = _keystorePass;
            PlayerSettings.Android.keyaliasName = _keyaliasName;
            PlayerSettings.Android.keyaliasPass = _keyaliasPass;

            if (!silent)
            {
                Debug.Log($"{LOG_PREFIX} Success: Keystore applied to PlayerSettings!");
                RefreshRegion();
            }
        }

        private void LoadFromPlayerSettings()
        {
            _keystorePath = PlayerSettings.Android.keystoreName;
            _keystorePass = PlayerSettings.Android.keystorePass;
            _keyaliasName = PlayerSettings.Android.keyaliasName;
            _keyaliasPass = PlayerSettings.Android.keyaliasPass;

            Debug.Log($"{LOG_PREFIX} Loaded settings from PlayerSettings.");
            RefreshRegion();
        }

        private void ClearKeystoreFromPlayerSettings()
        {
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = "";
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasName = "";
            PlayerSettings.Android.keyaliasPass = "";

            Debug.Log($"{LOG_PREFIX} Cleared: Custom Keystore disabled.");
            RefreshRegion();
        }
        #endregion

        #region Tab 2: Build Config Page
        private void BuildBuildConfigTab(VisualElement container)
        {
            var targetGroup = NamedBuildTarget.Android;

            // 1. Android Identifiers Box
            var idBox = UITKEditorHelper.BuildBox("Package & Versioning");
            idBox.style.backgroundColor = COLOR_GREY_BOX;
            idBox.style.borderTopColor = COLOR_GREY_BORDER;
            idBox.style.borderBottomColor = COLOR_GREY_BORDER;
            idBox.style.borderLeftColor = COLOR_GREY_BORDER;
            idBox.style.borderRightColor = COLOR_GREY_BORDER;

            string appId = PlayerSettings.GetApplicationIdentifier(targetGroup);
            var appIdField = new TextField("Package Name") { value = appId, style = { marginBottom = 6 } };
            appIdField.RegisterValueChangedCallback(e =>
            {
                PlayerSettings.SetApplicationIdentifier(targetGroup, e.newValue);
            });
            idBox.Add(appIdField);

            var versionField = new TextField("Version Name") { value = PlayerSettings.bundleVersion, style = { marginBottom = 6 } };
            versionField.RegisterValueChangedCallback(e =>
            {
                PlayerSettings.bundleVersion = e.newValue;
            });
            idBox.Add(versionField);

            var codeField = new IntegerField("Bundle Version Code") { value = PlayerSettings.Android.bundleVersionCode, style = { marginBottom = 4 } };
            codeField.RegisterValueChangedCallback(e =>
            {
                PlayerSettings.Android.bundleVersionCode = e.newValue;
            });
            idBox.Add(codeField);

            container.Add(idBox);

            // 2. Build Type & Output Box
            var typeBox = UITKEditorHelper.BuildBox("Build Target & Architecture");
            typeBox.style.backgroundColor = COLOR_GREY_BOX;
            typeBox.style.borderTopColor = COLOR_GREY_BORDER;
            typeBox.style.borderBottomColor = COLOR_GREY_BORDER;
            typeBox.style.borderLeftColor = COLOR_GREY_BORDER;
            typeBox.style.borderRightColor = COLOR_GREY_BORDER;

            var aabToggle = new Toggle("Build App Bundle (.aab for Google Play)")
            {
                value = EditorUserBuildSettings.buildAppBundle,
                style = { marginBottom = 6 }
            };
            aabToggle.RegisterValueChangedCallback(e =>
            {
                EditorUserBuildSettings.buildAppBundle = e.newValue;
            });
            typeBox.Add(aabToggle);

            var devToggle = new Toggle("Development Build")
            {
                value = EditorUserBuildSettings.development,
                style = { marginBottom = 6 }
            };
            devToggle.RegisterValueChangedCallback(e =>
            {
                EditorUserBuildSettings.development = e.newValue;
            });
            typeBox.Add(devToggle);

            var backend = PlayerSettings.GetScriptingBackend(targetGroup);
            var backendLabel = new Label($"• Scripting Backend: {backend}")
            {
                style = { fontSize = 12, color = COLOR_FOREST_MIST, marginBottom = 4 }
            };
            typeBox.Add(backendLabel);

            var arch = PlayerSettings.Android.targetArchitectures;
            var archLabel = new Label($"• Target Architectures: {arch}")
            {
                style = { fontSize = 12, color = COLOR_FOREST_MIST, marginBottom = 4 }
            };
            typeBox.Add(archLabel);

            var minSdk = PlayerSettings.Android.minSdkVersion;
            var targetSdk = PlayerSettings.Android.targetSdkVersion;
            var sdkLabel = new Label($"• Min SDK: {minSdk} | Target SDK: {targetSdk}")
            {
                style = { fontSize = 12, color = COLOR_FOREST_MIST, marginBottom = 4 }
            };
            typeBox.Add(sdkLabel);

            container.Add(typeBox);

            // 3. Quick Links & Build Profile Box
            var linksBox = UITKEditorHelper.BuildBox("Build Profiles & Navigation");
            linksBox.style.backgroundColor = COLOR_GREY_BOX;
            linksBox.style.borderTopColor = COLOR_GREY_BORDER;
            linksBox.style.borderBottomColor = COLOR_GREY_BORDER;
            linksBox.style.borderLeftColor = COLOR_GREY_BORDER;
            linksBox.style.borderRightColor = COLOR_GREY_BORDER;

            var activeProfile = BuildProfile.GetActiveBuildProfile();
            string profileName = activeProfile != null ? activeProfile.name : "<None - Using Standard Build Settings>";
            var profileStatusLabel = new Label($"• Active Build Profile: {profileName}")
            {
                style = { fontSize = 12, color = activeProfile != null ? COLOR_SKY_BLUE : COLOR_FOREST_MIST, marginBottom = 8 }
            };
            linksBox.Add(profileStatusLabel);

            // Primary Build Profile Button
            var openBuildProfilesBtn = new Button(OpenBuildProfiles)
            {
                text = "📁 Open Build Profiles",
                style =
                {
                    height = 32,
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = COLOR_OCEAN_BLUE,
                    color = Color.white,
                    marginBottom = 6,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            linksBox.Add(openBuildProfilesBtn);

            var linksRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };

            var openPlayerSettingsBtn = new Button(() => SettingsService.OpenProjectSettings("Project/Player"))
            {
                text = "Open Player Settings",
                style =
                {
                    flexGrow = 1,
                    height = 26,
                    marginRight = 3,
                    fontSize = 11,
                    backgroundColor = COLOR_TAB_INACTIVE_BG,
                    color = Color.white,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            linksRow.Add(openPlayerSettingsBtn);

            var openBuildSettingsBtn = new Button(() => EditorApplication.ExecuteMenuItem("File/Build Settings..."))
            {
                text = "Open Build Settings",
                style =
                {
                    flexGrow = 1,
                    height = 26,
                    marginLeft = 3,
                    fontSize = 11,
                    backgroundColor = COLOR_TAB_INACTIVE_BG,
                    color = Color.white,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            linksRow.Add(openBuildSettingsBtn);

            linksBox.Add(linksRow);
            container.Add(linksBox);
        }

        private static void OpenBuildProfiles()
        {
            Debug.Log($"{LOG_PREFIX} OpenBuildProfiles"); // Rule 10 (Keywords only)

            // 1. Try standard Unity 6 menu item
            if (EditorApplication.ExecuteMenuItem("File/Build Profiles"))
            {
                return;
            }

            if (EditorApplication.ExecuteMenuItem("Window/General/Build Profiles"))
            {
                return;
            }

            // 2. Fallback: ping/select active build profile asset if available
            var activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile != null)
            {
                Selection.activeObject = activeProfile;
                EditorGUIUtility.PingObject(activeProfile);
                Debug.Log($"{LOG_PREFIX} Pinged Active BuildProfile: {activeProfile.name}");
            }
            else
            {
                EditorApplication.ExecuteMenuItem("File/Build Settings...");
            }
        }
        #endregion

        #region Tab 3: Options Page
        private void BuildOptionsTab(VisualElement container)
        {
            var optBox = UITKEditorHelper.BuildBox("Preferences & Automation");
            optBox.style.backgroundColor = COLOR_GREY_BOX;
            optBox.style.borderTopColor = COLOR_GREY_BORDER;
            optBox.style.borderBottomColor = COLOR_GREY_BORDER;
            optBox.style.borderLeftColor = COLOR_GREY_BORDER;
            optBox.style.borderRightColor = COLOR_GREY_BORDER;

            var autoToggle = new Toggle("Auto-Apply Keystore on Editor Open")
            {
                value = _autoApplyOnEnable,
                style = { marginBottom = 6 }
            };
            autoToggle.RegisterValueChangedCallback(e =>
            {
                _autoApplyOnEnable = e.newValue;
                EditorPrefs.SetBool(PREF_KEY_AUTO_APPLY_ON_ENABLE, _autoApplyOnEnable);
            });
            optBox.Add(autoToggle);

            var descLabel = new Label(
                "Unity automatically clears Keystore passwords upon restarting the Editor for security.\n" +
                "Enabling this option will automatically re-inject your stored credentials into PlayerSettings " +
                "whenever this window opens.")
            {
                style =
                {
                    fontSize = 11,
                    color = COLOR_FOREST_MIST,
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 8
                }
            };
            optBox.Add(descLabel);

            container.Add(optBox);

            // Reset Section (Rule 7)
            var resetBox = UITKEditorHelper.BuildBox("Danger Zone");
            resetBox.style.backgroundColor = COLOR_GREY_BOX;
            resetBox.style.borderTopColor = COLOR_DANGER_BORDER;
            resetBox.style.borderBottomColor = COLOR_DANGER_BORDER;
            resetBox.style.borderLeftColor = COLOR_DANGER_BORDER;
            resetBox.style.borderRightColor = COLOR_DANGER_BORDER;

            var resetBtn = new Button(ResetToDefaults)
            {
                text = "Reset All Stored Credentials",
                style =
                {
                    height = 32,
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    backgroundColor = COLOR_DANGER_BG,
                    color = Color.white,
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4
                }
            };
            resetBox.Add(resetBtn);
            container.Add(resetBox);
        }

        /// <summary>
        /// Rule 7 (Reset to Defaults option) & Rule 11 (In-Memory Reset).
        /// </summary>
        private void ResetToDefaults()
        {
            Debug.Log($"{LOG_PREFIX} ResetToDefaults"); // Rule 10 (Keywords only)

            // Clear cached keys
            EditorPrefs.DeleteKey(PREF_KEY_KEYSTORE_PATH);
            EditorPrefs.DeleteKey(PREF_KEY_KEYSTORE_PASS);
            EditorPrefs.DeleteKey(PREF_KEY_KEYALIAS_NAME);
            EditorPrefs.DeleteKey(PREF_KEY_KEYALIAS_PASS);
            EditorPrefs.DeleteKey(PREF_KEY_ACTIVE_TAB);
            EditorPrefs.DeleteKey(PREF_KEY_AUTO_APPLY_ON_ENABLE);

            // Re-init variables in memory (Rule 11)
            _keystorePath = "";
            _keystorePass = "";
            _keyaliasName = "";
            _keyaliasPass = "";
            _activeTab = TabType.KEYSTORE;
            _autoApplyOnEnable = false;
            _showPasswords = false;

            // Reload UI
            Close();
            ShowWindow();
        }
        #endregion
    }
#endif
}
