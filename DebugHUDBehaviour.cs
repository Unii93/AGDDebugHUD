using System.Diagnostics;
using Character;
using DisputeLib;
using HarmonyLib;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace AGDDebugHUD
{
    [HarmonyPatch]
    public static class DebugHUDInjector
    {
        [HarmonyPatch(typeof(GameSingleton), "Awake")]
        [HarmonyPostfix]
        static void InjectDebugHUD()
        {
            if (GameObject.Find("AGD_DebugHUD") != null)
                return;

            var hudGO = new GameObject("AGD_DebugHUD");
            UnityEngine.Object.DontDestroyOnLoad(hudGO);
            hudGO.AddComponent<DebugHUDBehaviour>();
            Debug.Log("[[AGDDebugHUD]] Debug HUD injected successfully.");
        }
    }

    public class DebugHUDBehaviour : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _panel;
        private PlayerController _localPlayer;
        private CoreCharacterController _characterController;
        private CharacterMovementController _characterMovementController;
        private RagdollController _characterRagdollController;
        private Stopwatch HUDTimeStopwatch = new Stopwatch();
        public double HUDTimeMs = 0;
            
        private void Awake()
        {
            if (!Plugin.EnabledConfig.Value)
            {
                enabled = false;
                return;
            }
            

            
            RegisterDefaultLines();
            CreateUI();
        }

        private void EnsureControllers()
        {
            if (_localPlayer != null && _characterController != null && _characterMovementController != null && _characterRagdollController != null)
            {
                return;
            }
            if (SessionManager.Instance == null)
            {
                return;
            }
            _localPlayer = SessionManager.Instance.LocalPlayer;
            if (_localPlayer == null)
            {
                return;
            }
            _characterController = _localPlayer.serverSpawnedCharacter;
            if (_characterController != null)
            {
                _characterMovementController = _characterController.GetComponent<CharacterMovementController>();
                _characterRagdollController = _characterController.Model.RagdollController;
            }
        }

        private void RegisterDefaultLines()
        {
            if (Plugin.EnableGameSectionConfig.Value)
            {
                var gameHeader = DebugHUDAPI.RegisterHeader("--- Game ---");

                if (Plugin.EnableFPSConfig.Value)
                {
                    DebugHUDAPI.RegisterCustomLine("FPS", () =>
                    {
                        return string.Format("Frame time: {0:F1}ms FPS: {1:F2}", Time.deltaTime*1000, 1/Time.deltaTime);
                    }, gameHeader);
                }

                if (Plugin.EnableHUDFPSConfig.Value)
                {
                    DebugHUDAPI.RegisterCustomLine("HUD Time", () =>
                    {
                        return string.Format("HUD time: {0:F3}ms FPS impact: {1:F2}", HUDTimeMs, 1/(Time.deltaTime-HUDTimeMs/1000) - 1/Time.deltaTime);
                    }, gameHeader);
                }
            }

            if (Plugin.EnableNetworkSectionConfig.Value)
            {
                var netHeader = DebugHUDAPI.RegisterHeader("--- Network ---");
                if (Plugin.EnablePingConfig.Value)
                {
                    DebugHUDAPI.RegisterCustomLine("Ping", () =>
                    {
                        EnsureControllers();
                        if (_localPlayer == null) return null;
                        return string.Format("Ping: {0}", _localPlayer.CurrentPing.Value);
                    }, netHeader);
                }
            }

            if (Plugin.EnableMovementSectionConfig.Value)
            {
                var moveHeader = DebugHUDAPI.RegisterHeader("--- Movement ---");
                if (Plugin.EnableVelocityConfig.Value)
                {
                    DebugHUDAPI.RegisterCustomLine("Velocity", () =>
                    {
                        EnsureControllers();
                        if (_characterMovementController == null) return null;
                        var v = _characterMovementController.Velocity;
                        return string.Format("Velocity: {0:F2} m/s (X:{1:F2} Y:{2:F2} Z:{3:F2})", v.magnitude, v.x, v.y, v.z);
                    }, moveHeader);
                }

                if (Plugin.EnablePositionConfig.Value)
                {
                    DebugHUDAPI.RegisterCustomLine("Position", () =>
                    {
                        EnsureControllers();
                        if (_characterMovementController == null) return null;
                        var p = _characterMovementController.transform.position;
                        return string.Format("Position: ({0:F2}, {1:F2}, {2:F2})", p.x, p.y, p.z);
                    }, moveHeader);
                }
            }

            if (Plugin.EnablePhysicsSectionConfig.Value)
            {
                var physHeader = DebugHUDAPI.RegisterHeader("--- Physics ---");
                if (Plugin.EnableRagdollStateConfig.Value)
                {
                    DebugHUDAPI.RegisterCustomLine("Ragdoll State", () =>
                    {
                        EnsureControllers();
                        if (_characterRagdollController == null) return null;
                        return string.Format("IsRagdoll: {0}  TimeInRagdollState: {1}",
                            _characterRagdollController.IsRagdollNetworked.Value,
                            _characterRagdollController.TimeInRagdollState.Elapsed);
                    }, physHeader);
                }
            }

            if (Plugin.EnableStatusSectionConfig.Value)
            {
                var statusHeader = DebugHUDAPI.RegisterHeader("--- Status ---");
                if (Plugin.EnableStateConfig.Value)
                {
                    DebugHUDAPI.RegisterCustomLine("State", () =>
                    {
                        EnsureControllers();
                        if (_characterController == null) return null;
                        return string.Format("Grounded: {0}  Aiming: {1}  Alive: {2}  NoClip: {3}",
                            _characterMovementController != null ? _characterMovementController.IsGroundedNetworked.Value.ToString() : "?",
                            _characterController.IsAiming,
                            _characterController.IsAlive,
                            _localPlayer.NoClip);
                    }, statusHeader);
                }

                if (Plugin.EnableStatusEffectsConfig.Value)
                {
                    DebugHUDAPI.RegisterCustomLine("Status Effects", () =>
                    {
                        EnsureControllers();
                        if (_characterController == null) return null;
                        var sec = _characterController.statusEffectController;
                        if (sec != null && sec.StatusEffects != null)
                        {
                            var effects = sec.StatusEffects
                                .Where(e => e != null)
                                .Select(e => e.Effect.ToString())
                                .ToArray();
                            return string.Format("Status Effects ({0}): {1}", effects.Length,
                                effects.Length > 0 ? string.Join(", ", effects) : "none");
                        }
                        return "Status Effects (0): none";
                    }, statusHeader);
                }
                
            }
        }
        
        private void CreateUI()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32767;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            _panel.transform.SetParent(transform, false);

            var panelRect = _panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);

            var panelImage = _panel.GetComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.6f);

            var layout = _panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 2;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var fitter = _panel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            DebugHUDAPI.Initialize(_panel, 14);
        }


        private void Update()
        {
            if (Input.GetKeyDown(Plugin.ToggleKeyConfig.Value))
                _panel.SetActive(!_panel.activeSelf);

            if (!_panel.activeSelf) return;
            
            HUDTimeStopwatch.Restart();
            DebugHUDAPI.RefreshAll();
            HUDTimeStopwatch.Stop();
            HUDTimeMs = HUDTimeStopwatch.Elapsed.TotalMilliseconds;
        }
    }

    public static class DebugHUDAPI
    {
        public class CustomLineEntry
        {
            public enum State { Enabled, Frozen, Disabled }

            public string Label;
            public TextMeshProUGUI Tmp;
            
            public Func<string> Getter;
            public State CurrentState = State.Enabled;
        }

        public class Header
        {
            public string Label;
            internal TextMeshProUGUI Tmp;
        }


        private static readonly Header _defaultHeader = new Header();
        private static readonly Dictionary<Header, List<CustomLineEntry>> _lines = new Dictionary<Header, List<CustomLineEntry>>();
        private static readonly Dictionary<string, Header> _headerByLabel = new Dictionary<string, Header>();
        private static readonly Dictionary<string, CustomLineEntry> _entryByLabel = new Dictionary<string, CustomLineEntry>();
        private static GameObject _panel;
        private static float _fontSize;
        private static bool _initialized;

        public static void RegisterCustomLine(string label, Func<string> getter, Header header = null)
        {
            header ??= _defaultHeader;

            CustomLineEntry entry;
            Header oldHeader = null;
            if (_entryByLabel.TryGetValue(label, out entry))
            {
                entry.Getter = getter;
                foreach (var kvp in _lines)
                {
                    if (kvp.Value.Remove(entry))
                    {
                        oldHeader = kvp.Key;
                        break;
                    }
                }
            }
            else
            {
                entry = new CustomLineEntry { Label = label, Getter = getter };
                _entryByLabel[label] = entry;
            }
            
            if (!_lines.ContainsKey(header))
                _lines[header] = new List<CustomLineEntry>();
            _lines[header].Add(entry);
            
            if (_initialized && _panel != null)
            {
                if (entry.Tmp == null)
                {
                    entry.Tmp = CreateLineInternal(_panel, label, _fontSize, false);
                }
                else if (oldHeader != null && oldHeader != header)
                {
                    UnityEngine.Object.Destroy(entry.Tmp.gameObject);
                    entry.Tmp = CreateLineInternal(_panel, label, _fontSize, false);
                }
            }
        }

        public static Header RegisterHeader(string label)
        {
            Header header;
            if (_headerByLabel.TryGetValue(label, out header))
                return header;

            header = new Header { Label = label };
            _headerByLabel[label] = header;
            _lines[header] = new List<CustomLineEntry>();

            if (_initialized && _panel != null)
                header.Tmp = CreateLineInternal(_panel, label, _fontSize, true);

            return header;
        }

        public static void EnableLine(string label)
        {
            SetLineState(label, CustomLineEntry.State.Enabled);
        }

        public static void DisableLine(string label)
        {
            SetLineState(label, CustomLineEntry.State.Disabled);
        }

        public static void FreezeLine(string label)
        {
            SetLineState(label, CustomLineEntry.State.Frozen);
        }

        public static void SetLineState(string label, CustomLineEntry.State state)
        {
            CustomLineEntry entry;
            if (_entryByLabel.TryGetValue(label, out entry))
                entry.CurrentState = state;
        }

        public static void Initialize(GameObject panel, float fontSize)
        {
            _panel = panel;
            _fontSize = fontSize;
            _initialized = true;

            foreach (var kvp in _lines)
            {
                var header = kvp.Key;
                if (header != _defaultHeader && header.Tmp == null)
                    header.Tmp = CreateLineInternal(panel, header.Label, fontSize, true);

                foreach (var entry in kvp.Value)
                {
                    if (entry.Tmp == null)
                        entry.Tmp = CreateLineInternal(panel, entry.Label, fontSize, false);
                }
            }
        }

        public static void RefreshAll()
        {
            foreach (var kvp in _lines)
            {
                var header = kvp.Key;

                // Render header (non-default)
                if (header != _defaultHeader && header.Tmp != null)
                    header.Tmp.text = header.Label;

                foreach (var entry in kvp.Value)
                {
                    if (entry.Tmp == null)
                        continue;

                    string text;
                    switch (entry.CurrentState)
                    {
                        case CustomLineEntry.State.Disabled:
                            entry.Tmp.gameObject.SetActive(false);
                            continue;
                        case CustomLineEntry.State.Frozen:
                            continue;
                        default: // Enabled
                            text = entry.Getter?.Invoke();
                            bool active = text != null;
                            if (active)
                                entry.Tmp.text = text;
                            entry.Tmp.gameObject.SetActive(active);
                            break;
                    }
                }
            }
        }

        private static TextMeshProUGUI CreateLineInternal(GameObject parent, string name, float fontSize, bool isHeader)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent.transform, false);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = name;
            tmp.fontSize = fontSize;
            tmp.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;
            tmp.color = isHeader ? new Color(1f, 0.85f, 0.3f) : Color.white;
            tmp.raycastTarget = false;

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.minHeight = fontSize + 4;

            return tmp;
        }
    }
}
