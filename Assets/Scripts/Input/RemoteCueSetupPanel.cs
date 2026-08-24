using GyroCue.UI;
using UnityEngine;
using UnityEngine.UI;

namespace GyroCue.Input
{
    /// <summary>Compact runtime setup panel for pairing and recovering the cue phone.</summary>
    [DisallowMultipleComponent]
    public sealed class RemoteCueSetupPanel : MonoBehaviour
    {
        private RemoteCueUdpReceiver receiver;
        private RemoteSensorInputAdapter adapter;
        private Text statusLabel;
        private Text listenButtonLabel;
        private Button listenButton;
        private Button disconnectButton;
        private Button calibrateButton;
        private Button touchButton;

        public void Configure(RemoteCueUdpReceiver udpReceiver, RemoteSensorInputAdapter inputAdapter)
        {
            receiver = udpReceiver;
            adapter = inputAdapter;
            BuildCanvas();
        }

        private void Update()
        {
            if (receiver == null || adapter == null || statusLabel == null)
            {
                return;
            }

            var snapshot = receiver.Snapshot;
            statusLabel.text = BuildStatusText(snapshot);
            listenButton.interactable = snapshot.State == RemoteCueReceiverState.Disconnected ||
                                        snapshot.State == RemoteCueReceiverState.Error;
            disconnectButton.interactable = snapshot.State != RemoteCueReceiverState.Disconnected;
            calibrateButton.interactable = snapshot.State == RemoteCueReceiverState.Connected &&
                                           !adapter.IsCalibrationInProgress;
            touchButton.interactable = snapshot.State != RemoteCueReceiverState.Disconnected ||
                                       adapter.RemoteInputEnabled;
            listenButtonLabel.text = snapshot.State == RemoteCueReceiverState.Error ? "Retry" : "Listen";
        }

        private string BuildStatusText(RemoteCueSessionSnapshot snapshot)
        {
            var endpoint = $"{receiver.ListeningAddress}:{snapshot.Port}";
            var calibration = adapter.CalibrationState == RemoteCueCalibrationState.Calibrating
                ? $"  •  calibrating {adapter.CalibrationSamplesCollected}"
                : adapter.CalibrationState == RemoteCueCalibrationState.Calibrated
                    ? "  •  calibrated"
                    : string.Empty;

            switch (snapshot.State)
            {
                case RemoteCueReceiverState.Listening:
                    return $"Cue phone: LISTENING  {endpoint}\nOpen GyroCue on phone 2 and stream UDP here. Touch stays available.";
                case RemoteCueReceiverState.Connected:
                    return $"Cue phone: CONNECTED  {snapshot.Sender}\nLast frame {snapshot.LastFrameAgeSeconds * 1000.0:0} ms ago{calibration}";
                case RemoteCueReceiverState.Stale:
                    return $"Cue phone: STALE  •  touch restored\nNo frame for {snapshot.LastFrameAgeSeconds:0.0}s. Check Wi-Fi or reconnect.";
                case RemoteCueReceiverState.Error:
                    return $"Cue phone: ERROR  •  touch restored\n{snapshot.LastError}";
                default:
                    return $"Cue phone: OFF  •  touch controls active\nTap Listen, then send UDP to {receiver.ListeningAddress}:{receiver.ListenPort}.";
            }
        }

        private void HandleListen()
        {
            adapter.SetRemoteInputEnabled(true);
            receiver.StartListening();
        }

        private void HandleDisconnect()
        {
            receiver.StopListening();
            adapter.MarkRemoteStreamIdle();
        }

        private void HandleCalibrate()
        {
            if (receiver.Snapshot.State == RemoteCueReceiverState.Connected)
            {
                adapter.BeginCalibration();
            }
        }

        private void HandleTouchFallback()
        {
            receiver.StopListening();
            adapter.SetRemoteInputEnabled(false);
        }

        private void BuildCanvas()
        {
            if (statusLabel != null)
            {
                return;
            }

            var canvasObject = new GameObject("RemoteCueSetup", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, worldPositionStays: false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var canvasRect = (RectTransform)canvasObject.transform;
            var safeRoot = new GameObject("SafeArea", typeof(RectTransform));
            safeRoot.transform.SetParent(canvasRect, false);
            var safeRect = (RectTransform)safeRoot.transform;
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            safeRoot.AddComponent<SafeAreaFitter>();

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(safeRect, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(720f, 210f);
            panel.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f, 0.82f);

            statusLabel = CreateText("Status", panelRect, new Vector2(18f, -16f), new Vector2(684f, 108f), 24, TextAnchor.UpperLeft);
            listenButton = CreateButton("ListenButton", panelRect, new Vector2(18f, -142f), "Listen", HandleListen, out listenButtonLabel);
            disconnectButton = CreateButton("DisconnectButton", panelRect, new Vector2(190f, -142f), "Disconnect", HandleDisconnect, out _);
            calibrateButton = CreateButton("CalibrateButton", panelRect, new Vector2(362f, -142f), "Calibrate", HandleCalibrate, out _);
            touchButton = CreateButton("TouchButton", panelRect, new Vector2(534f, -142f), "Use touch", HandleTouchFallback, out _);
        }

        private static Button CreateButton(
            string name,
            RectTransform parent,
            Vector2 position,
            string label,
            UnityEngine.Events.UnityAction action,
            out Text text)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            created.transform.SetParent(parent, false);
            var rect = (RectTransform)created.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(154f, 50f);
            created.GetComponent<Image>().color = new Color(0.15f, 0.42f, 0.54f, 0.95f);

            var button = created.GetComponent<Button>();
            button.onClick.AddListener(action);
            text = CreateText("Label", rect, Vector2.zero, rect.sizeDelta, 22, TextAnchor.MiddleCenter);
            text.text = label;
            return button;
        }

        private static Text CreateText(
            string name,
            RectTransform parent,
            Vector2 position,
            Vector2 size,
            int fontSize,
            TextAnchor alignment)
        {
            var created = new GameObject(name, typeof(RectTransform), typeof(Text));
            created.transform.SetParent(parent, false);
            var rect = (RectTransform)created.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = created.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
