# EEG BCI Drone Simulator / EEG 腦機介面無人機模擬器

This Unity prototype is part of **EEG-Based BCI Error Detection for Computer Vision-Guided Autonomous Drone Navigation** research.
本 Unity 原型是 **EEG-Based BCI Error Detection for Computer Vision-Guided Autonomous Drone Navigation（用於電腦視覺導引自主無人機導航的 EEG 腦機介面錯誤偵測）**研究的一部分。

It provides a first-person drone simulation, a bus collision scenario, local event logs, and optional UDP markers for future integration with a laboratory EEG system.
本專案提供第一人稱無人機模擬、巴士碰撞情境、本機事件紀錄，以及可選用的 UDP 標記功能，供後續整合實驗室 EEG 系統。

## Current scope / 目前範圍

The scene tools create a Sydney-inspired intersection with a bus, a stationary light rail vehicle at a red signal, low-rise buildings, a Woolies-style storefront, pedestrians, and a Tello-style drone.
場景工具可建立雪梨風格的十字路口，包含巴士、停在紅燈前的輕軌、低樓層建築、Woolies 風格店面、行人與 Tello 風格無人機。

The scenery uses simplified geometry, and the pedestrians and light rail remain static.
環境採用簡化幾何模型，行人與輕軌目前保持靜止。

Brand references describe visual inspiration and do not imply affiliation or official endorsement.
品牌名稱僅用於說明外觀參考，不代表合作關係或官方認可。

## Requirements / 環境需求

Open the project with **Unity 6000.6.3f1**, as recorded in `ProjectSettings/ProjectVersion.txt`.
請使用 `ProjectSettings/ProjectVersion.txt` 所記錄的 **Unity 6000.6.3f1** 開啟專案。

The project uses Universal Render Pipeline **17.6.0** and Input System **1.20.0**, with dependencies recorded in `Packages/manifest.json` and `Packages/packages-lock.json`.
專案使用 Universal Render Pipeline **17.6.0** 與 Input System **1.20.0**，相依套件記錄於 `Packages/manifest.json` 和 `Packages/packages-lock.json`。

The prototype was developed on Windows with a keyboard; other platforms have not been verified.
本原型在 Windows 上以鍵盤操作開發，其他平台尚未驗證。

## Quick start / 快速開始

1. Clone or download this repository, then add its root folder through Unity Hub.
   複製或下載此 repository，再透過 Unity Hub 加入專案根目錄。
2. Open it with the matching Unity version and wait for package restoration and script compilation.
   使用相符的 Unity 版本開啟，等待套件還原與腳本編譯完成。
3. Open `Assets/_Project/Scenes/DroneBusCollision.unity` to use the saved scene.
   開啟 `Assets/_Project/Scenes/DroneBusCollision.unity`，使用已儲存的場景。
4. To generate a fresh scene instead, select **Tools > EEG Drone > Build Experiment Scene**.
   如需重新產生場景，請選取 **Tools > EEG Drone > Build Experiment Scene**。
5. Press **Play**, click inside the **Game** view, and press **Enter** to start a trial.
   按下 **Play**，點選 **Game** 畫面內部，再按 **Enter** 開始一輪試驗。
6. Hold **W** to approach the bus, and inspect the Console for the event markers and collision timestamp.
   按住 **W** 接近巴士，並在 Console 查看事件標記與碰撞時間戳記。

Rebuilding the scene replaces manual edits in the generated scene after a confirmation dialog.
重建場景時會顯示確認對話框，確認後將取代該產生場景中的手動修改。

Scene is the editing view, while Game shows the camera attached to the drone.
Scene 是編輯用視角，Game 則顯示掛在無人機上的攝影機畫面。

## Controls / 操作方式

| Key / 按鍵 | Action / 動作 |
| --- | --- |
| Enter | Start a trial or restart after completion. / 開始試驗，或在結束後開始下一輪。 |
| W / S | Move forward / backward. / 前進／後退。 |
| A / D | Move left / right. / 左移／右移。 |
| Space | Ascend. / 上升。 |
| Left Ctrl | Descend. / 下降。 |
| R | Abort the active trial. / 取消進行中的試驗。 |

Movement is locked before a trial starts and after a collision or cancellation.
試驗開始前，以及碰撞或取消之後，無人機會停止並鎖定移動。

Starting the next trial returns the drone to its configured starting position.
開始下一輪時，無人機會回到設定的起始位置。

## Event markers / 事件標記

| Code / 代碼 | Event / 事件 | Trigger / 觸發條件 |
| --- | --- | --- |
| 1 | `TRIAL_START` | Enter starts a trial. / 按 Enter 開始一輪試驗。 |
| 2 | `APPROACH_START` | First observed movement toward the bus that reduces the surface gap. / 首次觀察到朝向巴士且縮短表面間距的移動。 |
| 3 | `NEAR_BUS` | Surface gap reaches the configured threshold after approach begins. / 開始接近後，表面間距進入設定門檻。 |
| 4 | `BUS_COLLISION` | First collision with the configured bus ends the trial. / 首次碰撞指定巴士並結束試驗。 |
| 9 | `TRIAL_ABORTED` | R cancels a trial, or an active trial is interrupted when the session closes. / 按 R 取消試驗，或工作階段結束時中斷進行中的試驗。 |

Each event can occur only once per trial, and sequence numbers continue across trials within a Play session.
每個事件在每輪試驗中只會記錄一次，序號會在同一次 Play 工作階段的不同試驗間持續累加。

The default near-distance threshold is **1 metre**, measured between the physical surfaces of the axis-aligned box colliders rather than object centres.
預設接近門檻為 **1 公尺**，量測的是與座標軸對齊之方盒碰撞器的物理表面間距，而非物件中心距離。

The collision also produces one standalone `BUS_COLLISION` Console message, a timestamp, and an optional explosion effect controlled by **Play Explosion** on ExperimentManager.
碰撞還會在 Console 產生一筆獨立的 `BUS_COLLISION` 訊息與時間戳記，並可透過 ExperimentManager 的 **Play Explosion** 設定控制爆炸特效。

## Local recording and UDP / 本機紀錄與 UDP

UDP is disabled by default, so trials can run without a known laboratory IP address.
UDP 預設關閉，因此即使尚未取得實驗室 IP 位址，也可以執行試驗。

In the Unity Editor, each Play session creates a UTF-8 JSONL file under `Logs/ExperimentMarkers/`.
在 Unity Editor 中，每次 Play 工作階段都會在 `Logs/ExperimentMarkers/` 建立一個 UTF-8 JSONL 檔案。

In a standalone player, the output path is `Application.persistentDataPath/ExperimentMarkers/`.
在獨立執行程式中，輸出路徑為 `Application.persistentDataPath/ExperimentMarkers/`。

Each line contains one marker, and UDP sends the same JSON object in one datagram without automatic retries.
每行包含一筆標記，UDP 會以單一資料報傳送相同的 JSON 物件，且不會自動重送。

The following is an illustrative payload, not a participant recording.
以下為封包格式示例，不是參與者的實驗紀錄。

```json
{
  "schema_version": 1,
  "session_id": "example-session",
  "trial_id": 1,
  "sequence": 3,
  "marker": 3,
  "event_name": "NEAR_BUS",
  "unity_time_s": 125.483921,
  "trial_time_s": 2.683921,
  "distance_m": 0.98
}
```

Configure **Enable Udp**, **Destination Ip**, and **Destination Port** on ExperimentManager before pressing Play.
請在按 Play 前，於 ExperimentManager 設定 **Enable Udp**、**Destination Ip** 和 **Destination Port**。

A blank or invalid IP address, an invalid port, or a socket setup failure leaves local recording active and reports a warning.
若 IP 空白或無效、Port 無效，或 socket 初始化失敗，系統會保留本機紀錄並顯示警告。

### Local UDP test / 本機 UDP 測試

1. With Play stopped, enable UDP and set the destination to `127.0.0.1` with port `5005`.
   停止 Play，啟用 UDP，將目標設為 `127.0.0.1`，Port 設為 `5005`。
2. Open **Tools > EEG Drone > UDP Marker Test Receiver**, then enter Play Mode.
   開啟 **Tools > EEG Drone > UDP Marker Test Receiver**，再進入 Play Mode。
3. Click **Start Listening** in the receiver window using the same port.
   在接收視窗使用相同的 Port，點選 **Start Listening**。
4. Return focus to Game and complete a trial to inspect the received JSON messages.
   將焦點切回 Game 並完成一輪試驗，查看接收到的 JSON 訊息。

The test receiver listens only on localhost and does not write markers into EEG recording software.
測試接收器僅監聽本機，並不會將標記寫入 EEG 錄製軟體。

## Timing and research limits / 時間與研究限制

`unity_time_s` uses `Time.realtimeSinceStartupAsDouble`, while `trial_time_s` is elapsed time since that trial started.
`unity_time_s` 使用 `Time.realtimeSinceStartupAsDouble`，而 `trial_time_s` 表示該輪試驗開始後經過的時間。

These values describe Unity event detection, not a synchronized EEG clock or the measured onset of a visible display change.
這些數值描述 Unity 偵測到事件的時間，不是已同步的 EEG 時鐘，也不是經量測的螢幕視覺變化起始時間。

Approach and proximity checks are sampled in `LateUpdate`, so their timing depends on the render and physics update cadence.
接近動作與距離檢查在 `LateUpdate` 中取樣，因此時間取決於畫面與物理更新的節奏。

UDP delivery is not acknowledged, and marker alignment, latency, packet loss, and the laboratory receiver format still need end-to-end validation.
UDP 傳送沒有接收確認，標記對齊、延遲、封包遺失及實驗室接收格式仍需進行端到端驗證。

Explosion feedback is an additional visual stimulus and should be configured consistently for the intended experiment.
爆炸回饋是額外的視覺刺激，應依實驗目的維持一致設定。

## Verification / 驗證

Run **Tools > EEG Drone > Run Marker Checks** to check trial state, duplicate prevention, reset behaviour, local logging, invalid settings, and loopback UDP transmission.
執行 **Tools > EEG Drone > Run Marker Checks**，檢查試驗狀態、重複觸發防護、重設行為、本機紀錄、無效設定與本機 UDP 傳輸。

These checks do not replace Play Mode inspection, visual review, or timing validation with the actual EEG system.
這些檢查不能取代 Play Mode 操作確認、畫面檢視，或與實際 EEG 系統進行的時間驗證。

## Project structure / 專案結構

```text
Assets/_Project/
  Scenes/                 # Saved experiment scene / 已儲存的實驗場景
  Scripts/Drone/          # Keyboard movement and collision handling / 鍵盤移動與碰撞處理
  Scripts/Experiment/     # Trial state, markers, UDP and effects / 試驗狀態、標記、UDP 與特效
  Editor/                 # Scene builders and verification tools / 場景建置與驗證工具
  Materials/              # Shared scene materials / 場景共用材質
  Models/                 # Generated tram mesh / 產生的輕軌模型
Packages/                 # Dependency manifest and lock file / 相依套件清單與鎖定檔
ProjectSettings/          # Unity project settings / Unity 專案設定
```

The **Tools > EEG Drone** menu also provides individual updates for the bus, drone, intersection, storefront, pedestrians, crossings, and roadside buildings.
**Tools > EEG Drone** 選單也提供巴士、無人機、路口、店面、行人、斑馬線與沿街建築的個別更新工具。

