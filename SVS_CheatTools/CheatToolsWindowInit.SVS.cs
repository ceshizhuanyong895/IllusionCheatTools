using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Character;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using IllusionMods;
using RuntimeUnityEditor.Core.Inspector;
using RuntimeUnityEditor.Core.Inspector.Entries;
using RuntimeUnityEditor.Core.ObjectTree;
using RuntimeUnityEditor.Core.Utils;
using SaveData;
using SaveData.Extension;
using UnityEngine;
using UnityEngine.UI;

namespace CheatTools
{
    public static class CheatToolsWindowInit
    {
        private static ImguiComboBoxSimple _belongingsDropdown;
        private static ImguiComboBoxSimple _traitsDropdown;
        private static ImguiComboBoxSimple _hPreferenceDropdown;
        private static int _otherCharaListIndex;
        private static ImguiComboBox _otherCharaDropdown = new();
        private static KeyValuePair<object, string>[] _openInInspectorButtons;
        private static Actor _currentVisibleChara, _currentVisibleCharaMain;
        private static bool InsideADV => ADV.ADVManager._instance?.IsADV == true;
        private static bool InsideH => SV.H.HScene.Active();

        public static void Initialize(CheatToolsPlugin instance)
        {
            CheatToolsWindow.OnShown += window =>
            {
                _openInInspectorButtons = new[]
                {
                    new KeyValuePair<object, string>((object)SV.H.HScene._instance ?? SV.H.HScene._instance, "SV.H.HScene"),
                    new KeyValuePair<object, string>(ADV.ADVManager._instance, "ADV.ADVManager"),
                    new KeyValuePair<object, string>((object)Manager.Game._instance ?? typeof(Manager.Game), "Manager.Game"),
                    new KeyValuePair<object, string>(Manager.Game.saveData, "Manager.Game.saveData"),
                    new KeyValuePair<object, string>(typeof(Manager.Config), "Manager.Config"),
                    new KeyValuePair<object, string>((object)Manager.Scene._instance ?? typeof(Manager.Scene), "Manager.Scene"),
                    new KeyValuePair<object, string>((object)Manager.Sound._instance ?? typeof(Manager.Sound), "Manager.Sound"),
                    new KeyValuePair<object, string>(typeof(Manager.GameSystem), "Manager.GameSystem"),
                    new KeyValuePair<object, string>((object)Manager.MapManager._instance ?? typeof(Manager.MapManager), "Manager.MapManager"),
                    new KeyValuePair<object, string>((object)Manager.SimulationManager._instance ?? typeof(Manager.SimulationManager), "Manager.SimulationManager"),
                    new KeyValuePair<object, string>((object)Manager.TalkManager._instance ?? typeof(Manager.TalkManager), "Manager.TalkManager"),
                };

                if (_belongingsDropdown == null)
                {
                    _belongingsDropdown = new ImguiComboBoxSimple(Manager.Game.BelongingsInfoTable.AsManagedEnumerable().OrderBy(x => x.Key).Select(x => new GUIContent(x.Value)).ToArray());
                    for (var i = 0; i < _belongingsDropdown.Contents.Length; i++)
                    {
                        var iCopy = i;
                        TranslationHelper.TranslateAsync(_belongingsDropdown.Contents[iCopy].text, s => _belongingsDropdown.Contents[iCopy].text = s);
                    }
                    window.ComboBoxesToDisplay.Add(_belongingsDropdown);
                }
                if (_traitsDropdown == null)
                {
                    var guiContents = Manager.Game.IndividualityInfoTable.AsManagedEnumerable().ToDictionary(x => x.Value.ID, x => new GUIContent(x.Value.Name, null, x.Value.Information)).OrderBy(x => x.Key).ToList();
                    _traitsDropdown = new ImguiComboBoxSimple(guiContents.Select(x => x.Value).ToArray());
                    _traitsDropdown.ContentsIndexes = guiContents.Select(x => x.Key).ToArray();
                    for (var i = 0; i < _traitsDropdown.Contents.Length; i++)
                    {
                        var iCopy = i;
                        TranslationHelper.TranslateAsync(_traitsDropdown.Contents[iCopy].text, s => _traitsDropdown.Contents[iCopy].text = s);
                        TranslationHelper.TranslateAsync(_traitsDropdown.Contents[iCopy].tooltip, s => _traitsDropdown.Contents[iCopy].tooltip = s);
                    }
                    window.ComboBoxesToDisplay.Add(_traitsDropdown);
                }
                if (_hPreferenceDropdown == null)
                {
                    var guiContents = Manager.Game.PreferenceHInfoTable.AsManagedEnumerable().ToDictionary(x => x.Key, x => new GUIContent(x.Value)).OrderBy(x => x.Key).ToList();
                    _hPreferenceDropdown = new ImguiComboBoxSimple(guiContents.Select(x => x.Value).ToArray());
                    _hPreferenceDropdown.ContentsIndexes = guiContents.Select(x => x.Key).ToArray();
                    for (var i = 0; i < _hPreferenceDropdown.Contents.Length; i++)
                    {
                        var iCopy = i;
                        TranslationHelper.TranslateAsync(_hPreferenceDropdown.Contents[iCopy].text, s => _hPreferenceDropdown.Contents[iCopy].text = s);
                    }
                    window.ComboBoxesToDisplay.Add(_hPreferenceDropdown);
                }
                window.ComboBoxesToDisplay.Add(_otherCharaDropdown);
            };

            CheatToolsWindow.Cheats.Add(new CheatEntry(_ => InsideH, DrawHSceneCheats, null));
            CheatToolsWindow.Cheats.Add(new CheatEntry(_ => InsideADV, DrawAdvCheats, null));
            CheatToolsWindow.Cheats.Add(new CheatEntry(_ => Manager.Game.saveData.WorldTime > 0, DrawGeneralCheats, null));
            CheatToolsWindow.Cheats.Add(new CheatEntry(_ => Manager.Game.Charas.Count > 0, DrawGirlCheatMenu, "無法在此畫面編輯角色狀態，或沒有角色。請讀取存檔或開始新遊戲並將角色加入名冊。"));
            CheatToolsWindow.Cheats.Add(CheatEntry.CreateOpenInInspectorButtons(() => _openInInspectorButtons));

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private static void DrawHSceneCheats(CheatToolsWindow cheatToolsWindow)
        {
            var hScene = SV.H.HScene._instance;

            GUILayout.Label("H 場景控制");

            foreach (var actor in hScene.Actors)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(actor.Name + " 計量條： " + actor.GaugeValue.ToString("N1"), GUILayout.Width(150));
                    GUI.changed = false;
                    var newValue = GUILayout.HorizontalSlider(actor.GaugeValue, 0, 100);
                    if (GUI.changed)
                        actor.SetGaugeValue(newValue);

                    // todo editing siru array doesn't cause updates
                    //    for (int i = 0; i < hActor._siruLv.Length; i++)
                    //    {
                    //        GUILayout.BeginHorizontal();
                    //        GUILayout.Label($"{(ChaFileDefine.SiruParts)i}: lv{hActor._siruLv[i]}", GUILayout.Width(150));
                    //        hActor._siruLv[i] = (byte)GUILayout.HorizontalSlider(hActor._siruLv[i], 0, 6);
                    //        GUILayout.EndHorizontal();
                    //    }
                }
                GUILayout.EndHorizontal();
            }

            DrawBackgroundHideToggles();

            if (GUILayout.Button("在檢視器中開啟 HScene"))
                Inspector.Instance.Push(new InstanceStackEntry(hScene, "SV.H.HScene"), true);
        }

        private static GameObject _bgPanel, _bgDownFrame, _bgUpFrame;
        private static void DrawAdvCheats(CheatToolsWindow cheatToolsWindow)
        {
            GUILayout.Label("ADV 場景控制");

            if (GUILayout.Button(new GUIContent("強制解鎖可見的對話選項", null, "將目前所有可見的按鈕變為可點擊狀態（反灰解除）。主要用於威脅選單。如果成功率為 0%，您仍然無法成功執行該動作。")))
            {
                var commandUi = UnityEngine.Object.FindObjectOfType<SV.CommandUI>();
                // For some reason buttons are found and set as interactable, but if they are in a hidden menu they revert to inactive when unhidden
                foreach (var btn in commandUi.GetComponentsInChildren<Button>(true))
                    btn.interactable = true;
            }

            DrawBackgroundHideToggles();
        }

        // Hiding ADV and H background
        private static void DrawBackgroundHideToggles()
        {
            if (!_bgPanel)
            {
                var bgCmp = Manager.Game.Instance.transform.GetComponentInChildren<SV.HighPolyBackGroundFrame>();
                var tr = bgCmp.animFrame.transform;
                _bgPanel = tr.Find("Panel").gameObject;
                _bgDownFrame = tr.Find("DownFrame").gameObject;
                _bgUpFrame = tr.Find("UpFrame").gameObject;
            }

            var prevActive = _bgDownFrame.activeSelf;
            var newActive = GUILayout.Toggle(prevActive, "顯示背景框");
            if (prevActive != newActive)
            {
                _bgDownFrame.active = newActive;
                _bgUpFrame.active = newActive;
            }

            // There is also a saturation effect that is not disabled by this at 'SimulationScene/Global Volume', didn't find a clean way to disable that one
            prevActive = _bgPanel.activeSelf;
            newActive = GUILayout.Toggle(prevActive, "顯示背景模糊");
            if (prevActive != newActive)
                _bgPanel.active = newActive;
        }

        private static void DrawGeneralCheats(CheatToolsWindow cheatToolsWindow)
        {
            Hooks.RiggedRng = GUILayout.Toggle(Hooks.RiggedRng, new GUIContent("操縱 RNG (成功率高於 0% 即成功)", null, "所有成功率至少 1% 的動作都將永遠成功。必須在與角色對話前啟用。\n警告：這將影響整個遊戲的 RNG。NPC（可能）將永遠成功執行他們的動作，這將嚴重扭曲模擬。某些事件可能永遠不會發生，或不斷重複，直到關閉此選項。"));

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("行走速度：");

                var normal = Hooks.SpeedMode == Hooks.SpeedModes.Normal || Hooks.SpeedMode == Hooks.SpeedModes.ReturnToNormal;
                var newNormal = GUILayout.Toggle(normal, "普通");
                if (!normal && newNormal)
                    Hooks.SpeedMode = Hooks.SpeedModes.ReturnToNormal;
                if (GUILayout.Toggle(Hooks.SpeedMode == Hooks.SpeedModes.Fast, "快速"))
                    Hooks.SpeedMode = Hooks.SpeedModes.Fast;
                if (GUILayout.Toggle(Hooks.SpeedMode == Hooks.SpeedModes.Sanic, "音速小子"))
                    Hooks.SpeedMode = Hooks.SpeedModes.Sanic;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                Hooks.InterruptBlock = GUILayout.Toggle(Hooks.InterruptBlock, new GUIContent("阻擋打擾", null, "防止 NPC 打斷其他 2 個角色之間的互動。這不會阻止 NPC 與閒置角色交談。"));
                Hooks.InterruptBlockAllow3P = GUILayout.Toggle(Hooks.InterruptBlockAllow3P, new GUIContent("3P 除外", null, "不要阻擋 NPC 為了要求 3P 而打斷。"));
                Hooks.InterruptBlockAllowNonPlayer = GUILayout.Toggle(Hooks.InterruptBlockAllowNonPlayer, new GUIContent("僅限玩家", null, "僅當玩家控制其中一名相關角色時才阻擋打擾。"));
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUI.enabled = !ReferenceEquals(SV.GameChara.PlayerAI, null);
            if (GUILayout.Button("目前時段無限時間"))
                SV.GameChara.PlayerAI!.charaData.charasGameParam.baseParameter.NowStamina = 100000;
            GUI.enabled = true;

            // todo doesn't work, nullref on open
            //if (GUILayout.Button("TEST Open relationship screen"))
            //{
            //    if (SV.CorrelationDiagramScene.CorrelationDiagram.Instance?.IsOpen() == true)
            //    {
            //        SV.CorrelationDiagramScene.CorrelationDiagram.Instance.CloseExeAsync(new SV.CorrelationDiagramScene.CorrelationDiagram.CloseParameter());
            //    }
            //    else
            //    {
            //        var param = new SV.CorrelationDiagramScene.CorrelationDiagram.OpenParameter();
            //        SV.CorrelationDiagramScene.CorrelationDiagram.Open(ref param);
            //    }
            //}

            DrawUtils.DrawNums("星期幾", 7, () => (byte)Manager.Game.saveData.Week, b => Manager.Game.saveData.Week = b);

            DrawUtils.DrawInt("總日數", () => Manager.Game.saveData.Day, i => Manager.Game.saveData.Day = i, "遊戲中經過的總日數。用於計算月經狀態以及可能其他事項。");

            //GUILayout.BeginHorizontal();
            //{
            //    GUILayout.Label("asd: ");
            //}
            //GUILayout.EndHorizontal();
        }

        private static void DrawGirlCheatMenu(CheatToolsWindow cheatToolsWindow)
        {
            GUILayout.Label("角色狀態編輯器");

            foreach (var chara in GameUtilities.GetCurrentActors(false))
            {
                var main = chara.Value.FindMainActorInstance();
                var isCopy = !ReferenceEquals(main.Value, chara.Value);
                if (GUILayout.Button($"選擇 #{chara.Key} - {chara.Value.GetCharaName(true)}{(isCopy ? " (副本)" : "")}"))
                {
                    _currentVisibleChara = chara.Value;
                    _currentVisibleCharaMain = isCopy ? main.Value : null;
                }
            }

            GUILayout.Space(6);

            try
            {
                if (_currentVisibleChara != null)
                    DrawSingleCharaCheats(_currentVisibleChara, _currentVisibleCharaMain, cheatToolsWindow);
                else
                    GUILayout.Label("請選擇一個角色來編輯其狀態");
            }
            catch (Exception e)
            {
                CheatToolsPlugin.Logger.LogError(e);
                _currentVisibleChara = null;
            }
        }

        private static void DrawSingleCharaCheats(Actor currentAdvChara, Actor mainChara, CheatToolsWindow cheatToolsWindow)
        {
            var comboboxMaxY = (int)cheatToolsWindow.WindowRect.bottom - 30;
            var isCopy = mainChara != null;

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("已選擇：", IMGUIUtils.LayoutOptionsExpandWidthFalse);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(currentAdvChara.GetCharaName(true), IMGUIUtils.LayoutOptionsExpandWidthFalse);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("關閉", IMGUIUtils.LayoutOptionsExpandWidthFalse)) _currentVisibleChara = null;
                }
                GUILayout.EndHorizontal();

                if (isCopy)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label(new GUIContent("!! 這是個副本角色 !!", null, "對此角色所做的所有變更將在目前場景結束後遺失。\n\n" +
                                                                                               "如果您想進行永久性變更，請開啟此角色的主要實體並在那裡進行變更。\n" +
                                                                                               "您將需要退出並重新進入目前場景以將變更傳播到副本角色）。"), IMGUIUtils.LayoutOptionsExpandWidthFalse);
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("開啟主要實體"))
                        {
                            _currentVisibleChara = mainChara;
                            _currentVisibleCharaMain = null;
                        }
                    }
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(6);

                var charasGameParam = currentAdvChara.charasGameParam;
                if (charasGameParam != null)
                {
                    var baseParameter = currentAdvChara.charasGameParam.baseParameter;

                    {
                        GUILayout.Label("遊戲內數值 (透過遊玩改變)");

                        DrawUtils.DrawSlider("耐力", 0, 1000, () => baseParameter.Stamina, val => baseParameter.Stamina = val);
                        DrawUtils.DrawSlider("目前耐力", 0, baseParameter.Stamina + 100, () => baseParameter.NowStamina, val => baseParameter.NowStamina = val,
                                             "當角色由玩家控制時，此欄位用於決定距離時段結束還有多久。NPC 不會使用它。\n初始值等於 '耐力 + 100'。");
                        DrawUtils.DrawSlider("對話", 0, 1000, () => baseParameter.Conversation, val => baseParameter.Conversation = val);
                        DrawUtils.DrawSlider("學習", 0, 1000, () => baseParameter.Study, val => baseParameter.Study = val);
                        DrawUtils.DrawSlider("生活", 0, 1000, () => baseParameter.Living, val => baseParameter.Living = val);
                        DrawUtils.DrawSlider("工作", 0, 1000, () => baseParameter.Job, val => baseParameter.Job = val, "似乎無效，變更會被覆蓋。");

                        GUILayout.Space(6);
                    }

                    GUILayout.BeginVertical(GUI.skin.box);
                    {
                        var menstruationsLength = charasGameParam.menstruations.Length;
                        var currentDayIndex = Manager.Game.saveData.Day % menstruationsLength;

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("月經： ");

                            GUI.color = currentAdvChara.IsMenstruation(ActorExtensionH.Menstruation.Normal) ? Color.green : Color.white;
                            if (GUILayout.Button("普通")) SetMenstruationForDay(currentDayIndex, 0);
                            GUI.color = currentAdvChara.IsMenstruation(ActorExtensionH.Menstruation.Safe) ? Color.green : Color.white;
                            if (GUILayout.Button("安全日")) SetMenstruationForDay(currentDayIndex, 1);
                            GUI.color = currentAdvChara.IsMenstruation(ActorExtensionH.Menstruation.Danger) ? Color.green : Color.white;
                            if (GUILayout.Button("危險日")) SetMenstruationForDay(currentDayIndex, 2);
                            GUI.color = Color.white;
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(menstruationsLength / 7 + "-週循環", GUILayout.Width(80));
                            var mensUiItems = new GUIContent[] { new("普"), new("安"), new("危") };
                            for (var i = 0; i < menstruationsLength; i++)
                            {
                                var mens = charasGameParam.menstruations[i];
                                GUI.color = currentDayIndex == i ? Color.green : Color.white;
                                if (GUILayout.Button(mensUiItems[mens]))
                                    SetMenstruationForDay(i, (mens + 1) % 3);

                                if (i == 6)
                                {
                                    GUI.color = Color.white;
                                    GUILayout.EndHorizontal();
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label("週期表：", GUILayout.Width(80));
                                }
                            }
                            GUI.color = Color.white;
                        }
                        GUILayout.EndHorizontal();

                        void SetMenstruationForDay(int index, int newMens) => charasGameParam.menstruations[index] = newMens;
                    }
                    GUILayout.EndVertical();

                    GUILayout.Space(6);

                    GUILayout.BeginVertical(GUI.skin.box);
                    if (isCopy)
                    {
                        GUILayout.Label("無法編輯副本角色的關係，请先開啟主要角色。");
                    }
                    else
                    {
                        // DarkSoldier27: Ok I figure it out:
                        // 0:LOVE
                        // 1:FRIEND
                        // 2:INDIFFERENT
                        // 3:DISLIKE
                        // values go from 0 to 30, reaching 30 increase a favorability point in longSensitivityCounts <- this is what determined their status, the max value for this one is also 30
                        // and yeah reaching below 0 reduce a point

                        GUILayout.BeginHorizontal();

                        GUILayout.Label("編輯與...的關係： ");

                        var targets = Manager.Game.saveData.Charas.AsManagedEnumerable().Select(x => x.Value).Where(x => x != null && !x.Equals(currentAdvChara)).ToArray();

                        _otherCharaListIndex = Math.Clamp(_otherCharaListIndex, -1, targets.Length - 1);

                        GUI.changed = false;
                        var result = GUILayout.Toggle(_otherCharaListIndex == -1, "所有人");
                        if (GUI.changed)
                            _otherCharaListIndex = result ? -1 : 0;

                        GUILayout.EndHorizontal();

                        if (_otherCharaListIndex >= 0)
                        {
                            _otherCharaListIndex = _otherCharaDropdown.Show(_otherCharaListIndex, targets.Select(x => new GUIContent(x.GetCharaName(true))).ToArray(), comboboxMaxY);
                            targets = new[] { targets[_otherCharaListIndex] };
                        }

                        if (targets.Length == 1)
                        {
                            // H Affinity controls
                            var targetChara = targets[0];
                            var targetCharaId = targetChara.TryGetActorId();
                            var to = baseParameter.GetHAffinity(targetCharaId);
                            var currentCharaId = currentAdvChara.TryGetActorId();
                            var targetBaseParameter = targetChara.charasGameParam.baseParameter;
                            var fro = targetBaseParameter.GetHAffinity(currentCharaId);

                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("H 親和度：");
                                GUILayout.FlexibleSpace();
                                GUILayout.Label($"對 -> lv{to.LV} {to.Point}pt", IMGUIUtils.LayoutOptionsExpandWidthFalse);
                                if (GUILayout.Button("+1")) baseParameter.AddHAffinity(targetCharaId, 20);
                                if (GUILayout.Button("0")) baseParameter.RemoveHAffinity(targetCharaId);
                                GUILayout.FlexibleSpace();
                                GUILayout.Label($"<- 來自 lv{fro.LV} {fro.Point}pt", IMGUIUtils.LayoutOptionsExpandWidthFalse);
                                if (GUILayout.Button("+1")) targetBaseParameter.AddHAffinity(currentCharaId, 20);
                                if (GUILayout.Button("0")) targetBaseParameter.RemoveHAffinity(currentCharaId);

                            }
                            GUILayout.EndHorizontal();
                        }
                        else if (targets.Length > 1)
                        {
                            GUILayout.BeginHorizontal();
                            {
                                GUILayout.Label("與所有人的 H 親和度： ");
                                if (GUILayout.Button("最高等級"))
                                {
                                    var targetIds = targets.Select(x => x.TryGetActorId()).ToArray();
                                    foreach (var targetId in targetIds) baseParameter.AddHAffinity(targetId, 100);

                                    var currentCharaId = currentAdvChara.TryGetActorId();
                                    foreach (var target in targets) target.charasGameParam.baseParameter.AddHAffinity(currentCharaId, 100);
                                }
                                if (GUILayout.Button("設為 0"))
                                {
                                    var targetIds = targets.Select(x => x.TryGetActorId()).ToArray();
                                    foreach (var targetId in targetIds) baseParameter.RemoveHAffinity(targetId);

                                    var currentCharaId = currentAdvChara.TryGetActorId();
                                    foreach (var target in targets) target.charasGameParam.baseParameter.RemoveHAffinity(currentCharaId);
                                }
                            }
                            GUILayout.EndHorizontal();
                        }

                        GUILayout.Label("警告：BETA 版，設定可能會被遊戲隨機重設。編輯後儲存並重新載入遊戲以獲得最佳成功機會。");

                        DrawSingleRankEditor(SensitivityKind.Love, currentAdvChara, targets);
                        DrawSingleRankEditor(SensitivityKind.Friend, currentAdvChara, targets);
                        DrawSingleRankEditor(SensitivityKind.Distant, currentAdvChara, targets);
                        DrawSingleRankEditor(SensitivityKind.Dislike, currentAdvChara, targets);
                    }
                    GUILayout.EndVertical();

                    //todo charasGameParam.sensitivity, same deal as relationships with tables and stocks

                    GUILayout.Space(6);
                }

                var gameParam = currentAdvChara.charFile.GameParameter;
                if (gameParam != null)
                {
                    GUILayout.BeginVertical(GUI.skin.box);
                    {
                        GUILayout.Label("角色卡數值 (與角色製作器中相同)");

                        DrawUtils.DrawStrings("職業", new[] { "無", "救生員", "咖啡廳", "神社" }, () => gameParam.job, b => gameParam.job = b);
                        DrawUtils.DrawNums("同性戀傾向", 5, () => gameParam.sexualTarget, b => gameParam.sexualTarget = b);
                        DrawUtils.DrawNums("貞操等級", 5, () => gameParam.lvChastity, b => gameParam.lvChastity = b);
                        DrawUtils.DrawNums("社交等級", 5, () => gameParam.lvSociability, b => gameParam.lvSociability = b);
                        DrawUtils.DrawNums("對話等級", 5, () => gameParam.lvTalk, b => gameParam.lvTalk = b);
                        DrawUtils.DrawNums("學習等級", 5, () => gameParam.lvStudy, b => gameParam.lvStudy = b);
                        DrawUtils.DrawNums("生活等級", 5, () => gameParam.lvLiving, b => gameParam.lvLiving = b);
                        DrawUtils.DrawNums("身體等級", 5, () => gameParam.lvPhysical, b => gameParam.lvPhysical = b);
                        DrawUtils.DrawNums("戰鬥風格", 3, () => gameParam.lvDefeat, b => gameParam.lvDefeat = b);

                        DrawUtils.DrawBool("是處女", () => gameParam.isVirgin, b => gameParam.isVirgin = b);
                        DrawUtils.DrawBool("是肛交處女", () => gameParam.isAnalVirgin, b => gameParam.isAnalVirgin = b);
                        DrawUtils.DrawBool("是男性處男", () => gameParam.isMaleVirgin, b => gameParam.isMaleVirgin = b);
                        DrawUtils.DrawBool("是男性肛交處男", () => gameParam.isMaleAnalVirgin, b => gameParam.isMaleAnalVirgin = b);
                    }
                    GUILayout.EndVertical();

                    GUILayout.Space(6);

                    DrawBelongingsPicker(gameParam, comboboxMaxY);
                    DrawTargetAnswersPicker(_hPreferenceDropdown, "H 偏好", gameParam, sv => sv.preferenceH, comboboxMaxY);
                    DrawTargetAnswersPicker(_traitsDropdown, "特質", gameParam, sv => sv.individuality, comboboxMaxY);
                }

                if (gameParam != null && GUILayout.Button("檢視 GameParameter"))
                    Inspector.Instance.Push(new InstanceStackEntry(gameParam, "GameParam " + currentAdvChara.GetCharaName(true)), true);

                if (charasGameParam != null && GUILayout.Button("檢視 CharactersGameParameter"))
                    Inspector.Instance.Push(new InstanceStackEntry(charasGameParam, "CharaGameParam " + currentAdvChara.GetCharaName(true)), true);

                if (GUILayout.Button("導航至角色的遊戲物件 (GameObject)"))
                {
                    if (currentAdvChara.transform)
                        ObjectTreeViewer.Instance.SelectAndShowObject(currentAdvChara.transform);
                    else
                        CheatToolsPlugin.Logger.Log(LogLevel.Warning | LogLevel.Message, "角色沒有分配身體模型");
                }

                if (GUILayout.Button("在檢視器中開啟角色"))
                    Inspector.Instance.Push(new InstanceStackEntry(currentAdvChara, "Actor " + currentAdvChara.GetCharaName(true)), true);

                //if (GUILayout.Button("Inspect extended data"))
                //{
                //    Inspector.Instance.Push(new InstanceStackEntry(ExtensibleSaveFormat.ExtendedSave.GetAllExtendedData(currentAdvChara.chaFile), "ExtData for " + currentAdvChara.Name), true);
                //}
            }
            GUILayout.EndVertical();
        }

        private static void DrawBelongingsPicker(HumanDataGameParameter_SV gameParam, int comboboxMaxY)
        {
            if (_belongingsDropdown == null) return;

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("持有的物品：");
            var targetArr = gameParam.belongings;
            foreach (var gameParameterBelonging in targetArr)
            {
                GUILayout.BeginHorizontal();
                {
                    if (gameParameterBelonging >= 0 && gameParameterBelonging < _belongingsDropdown.Contents.Length)
                        GUILayout.Label(_belongingsDropdown.Contents[gameParameterBelonging]);
                    else
                        GUILayout.Label("未知的物品 ID " + gameParameterBelonging);

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("X", IMGUIUtils.LayoutOptionsExpandWidthFalse))
                    {
                        gameParam.belongings = new Il2CppStructArray<int>(targetArr.Where(x => x != gameParameterBelonging).ToArray());
                    }

                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            {
                _belongingsDropdown.Show(comboboxMaxY);
                if (GUILayout.Button("給予", IMGUIUtils.LayoutOptionsExpandWidthFalse))
                {
                    if (!gameParam.belongings.Contains(_belongingsDropdown.Index))
                        gameParam.belongings = new Il2CppStructArray<int>(targetArr.AddItem(_belongingsDropdown.Index).ToArray());
                }
                if (GUILayout.Button(new GUIContent("給予所有人", null, "將此物品給予所有角色（如果他們尚未擁有），包含您自己。"), IMGUIUtils.LayoutOptionsExpandWidthFalse))
                {
                    foreach (var chara in Manager.Game.Charas.AsManagedEnumerable().Select(x => x.Value))
                    {
                        if (!chara.charFile.GameParameter.belongings.Contains(_belongingsDropdown.Index))
                            chara.charFile.GameParameter.belongings = new Il2CppStructArray<int>(chara.charFile.GameParameter.belongings.AddItem(_belongingsDropdown.Index).ToArray());
                    }
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.Space(6);
        }

        private static void DrawTargetAnswersPicker(ImguiComboBoxSimple combobox, string name, HumanDataGameParameter_SV currentCharaData, Func<HumanDataGameParameter_SV, HumanDataGameParameter_SV.AnswerBase> targetAnswers, int comboboxMaxY)
        {
            if (combobox == null) return;

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label(name + "：");
            var answerBase = targetAnswers(currentCharaData);
            var answerArr = answerBase.answer;
            foreach (var traitId in answerArr)
            {
                GUILayout.BeginHorizontal();
                {
                    var index = Array.IndexOf(combobox.ContentsIndexes, traitId);
                    if (index >= 0)
                    {
                        GUILayout.Label(combobox.Contents[index]);
                    }
                    else
                        GUILayout.Label($"未知的 {name} ID {traitId}");

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("X", IMGUIUtils.LayoutOptionsExpandWidthFalse))
                    {
                        answerBase.Set(traitId, false);
                    }
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            {
                combobox.Show(comboboxMaxY);
                var selectedTraitIndex = combobox.ContentsIndexes[combobox.Index];

                if (GUILayout.Button(new GUIContent("新增", null, "如果您新增超過 2 個項目，它們將在遊戲中生效，但在您儲存/載入遊戲或角色後將被移除。\n\n警告：在某些情況下，新增超過 3 個特質或物品可能會導致遊戲崩潰。"), IMGUIUtils.LayoutOptionsExpandWidthFalse))
                {
                    SetAnswer(answerBase, selectedTraitIndex);
                }
                if (GUILayout.Button(new GUIContent("新增至所有人", null, "將此項目新增至所有角色，包含您自己。"), IMGUIUtils.LayoutOptionsExpandWidthFalse))
                {
                    foreach (var chara in Manager.Game.Charas.AsManagedEnumerable().Select(x => x.Value))
                        SetAnswer(targetAnswers(chara.charFile.GameParameter), selectedTraitIndex);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(new GUIContent("全部清除", null, "從所有角色中移除所有項目，使所有列表為空。"), IMGUIUtils.LayoutOptionsExpandWidthFalse))
                {
                    foreach (var chara in Manager.Game.Charas.AsManagedEnumerable().Select(x => x.Value))
                        targetAnswers(chara.charFile.GameParameter).answer = new Il2CppStructArray<int>(new[] { -1, -1 });
                }
                if (GUILayout.Button(new GUIContent("全部修剪至 2 個", null, "僅保留前兩個項目，並從所有角色中移除其餘項目（使每個角色保留 2 個項目，即預設限制）。"), IMGUIUtils.LayoutOptionsExpandWidthFalse))
                {
                    foreach (var chara in Manager.Game.Charas.AsManagedEnumerable().Select(x => x.Value))
                    {
                        var answers = targetAnswers(chara.charFile.GameParameter);
                        var old = answers.answer;
                        answers.answer = new Il2CppStructArray<int>(2);
                        answers.answer[0] = old.Length >= 1 ? old[0] : -1;
                        answers.answer[1] = old.Length >= 2 ? old[1] : -1;
                    }
                }
            }
            GUILayout.EndHorizontal();

            void SetAnswer(HumanDataGameParameter_SV.AnswerBase individuality, int id)
            {
                if (individuality.answer.Contains(-1))
                    individuality.Set(id, true);
                else if (!individuality.answer.Contains(id))
                    individuality.answer = individuality.answer.AddItem(id).ToArray();
            }

            GUILayout.EndVertical();

            GUILayout.Space(6);
        }

        private static void DrawSingleRankEditor(SensitivityKind kind, Actor targetChara, IList<Actor> affectedCharas)
        {
            var targetCharaSensitivity = targetChara.charasGameParam.sensitivity;

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(kind + ":", GUILayout.Width(45));

                GUILayout.Label(new GUIContent("對 ->", null, "目前角色對上方下拉選單中所選目標角色的感情。\n等級：0 - 低，1 - 中，2 - 高，3 - 最高"));

                if (affectedCharas.Count == 1)
                {
                    var rank = targetCharaSensitivity.tableFavorabiliry[affectedCharas[0].TryGetActorId()].ranks[(int)kind];
                    GUILayout.Label(((int)rank).ToString());
                }

                if (GUILayout.Button("+1")) OnOutgoing(1);
                if (GUILayout.Button("-1")) OnOutgoing(-1);

                GUILayout.Label(new GUIContent("<- 來自", null, "目標角色對目前角色的感情。"));

                if (affectedCharas.Count == 1)
                {
                    var rank = affectedCharas[0].charasGameParam.sensitivity.tableFavorabiliry[targetChara.TryGetActorId()].ranks[(int)kind];
                    GUILayout.Label(((int)rank).ToString());
                }

                if (GUILayout.Button("+1")) OnIncoming(1);
                if (GUILayout.Button("-1")) OnIncoming(-1);
            }
            GUILayout.EndHorizontal();
            return;

            void OnOutgoing(int amount)
            {
                var targetIds = affectedCharas.Select(actor => actor.TryGetActorId()).ToArray();
                foreach (var tabkvp in targetCharaSensitivity.tableFavorabiliry)
                {
                    if (targetIds.Contains(tabkvp.Key))
                    {
                        ChangeRank(tabkvp.Value, kind, amount);

                        // All of these overwrite everything we just changed
                        // todo: need to find some way to update relationship status across the game without having to save/load
                        //targetCharaSensitivity.CalcFavorState(tabkvp.Value);
                        //targetCharaSensitivity.LongStockCalc(tabkvp.Value);
                        //targetCharaSensitivity.CalcHighvFavorability();
                    }
                }
            }
            void OnIncoming(int amount)
            {
                var ourId = targetChara.TryGetActorId();
                foreach (var charaKvp in affectedCharas)
                {
                    var otherSensitivity = charaKvp.charasGameParam.sensitivity;
                    var favorabiliryInfo = otherSensitivity.tableFavorabiliry[ourId];
                    ChangeRank(favorabiliryInfo, kind, amount);

                    //otherSensitivity.CalcFavorState(favorabiliryInfo);
                    //otherSensitivity.LongStockCalc(favorabiliryInfo);
                    //otherSensitivity.CalcHighvFavorability();
                }
            }
            void ChangeRank(SensitivityParameter.FavorabiliryInfo favorabiliryInfo, SensitivityKind sensitivityKind, int amount)
            {
                var newRank = (SensitivityParameter.Rank)Mathf.Clamp((int)(favorabiliryInfo.ranks[(int)sensitivityKind] + amount), 0, (int)SensitivityParameter.Rank.MAX);
                favorabiliryInfo.ranks[(int)sensitivityKind] = newRank;

                favorabiliryInfo.longSensitivityCounts[(int)sensitivityKind] = 10 * (int)newRank;
            }
        }
    }

    internal enum SensitivityKind
    {
        Love = 0,
        Friend = 1,
        Distant = 2,
        Dislike = 3
    }
}