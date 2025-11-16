using System.Collections.Generic;
using System.Linq;
using AC.Scene;
using AC.User;
using BepInEx.Logging;
using Cysharp.Threading.Tasks;
using H;
using HarmonyLib;
using Il2CppSystem.Threading;
using ILLGAMES.ADV;
using RuntimeUnityEditor.Core.Inspector;
using RuntimeUnityEditor.Core.Inspector.Entries;
using RuntimeUnityEditor.Core.ObjectTree;
using RuntimeUnityEditor.Core.Utils;
using UnityEngine;
using Array = System.Array;
using Exception = System.Exception;
using Math = System.Math;

namespace CheatTools
{
    public static class CheatToolsWindowInit
    {
        private static KeyValuePair<object, string>[] _openInInspectorButtons;
        private static NPCData _currentVisibleChara;

        // Only true when dialog box is open
        private static bool ADVOpen => ADVCore._instance && ADVCore._instance.isActiveAndEnabled;
        private static SaveData CurrentSaveData => Manager.Game.Instance?.SaveData;
        private static bool InsideH => HScene.IsActive();
        // TODO faster way to get this?
        private static ExploreScene ExploreSceneInstance => _exploreSceneInstance ? _exploreSceneInstance : _exploreSceneInstance = UnityEngine.Object.FindObjectOfType<ExploreScene>();
        private static ExploreScene _exploreSceneInstance;

        private static bool InsideCommunication
        {
            get
            {
                var exploreScene = ExploreSceneInstance;
                return exploreScene != null && exploreScene.CommunicationUI != null && exploreScene.CommunicationUI.isActiveAndEnabled && exploreScene.CommunicationUI._targets.Count > 0 && !InsideH;
            }
        }

        public static void Initialize(CheatToolsPlugin instance)
        {
            CheatToolsWindow.OnShown += window =>
            {
                _openInInspectorButtons = new[]
                {
                    new KeyValuePair<object, string>(HScene._instance ? HScene.Instance : typeof(HScene), "H.HScene"),
                    new KeyValuePair<object, string>(ExploreSceneInstance, "AC.Scene.ExploreScene"),
                    new KeyValuePair<object, string>(CurrentSaveData, "SaveData"),
                    new KeyValuePair<object, string>(typeof(Manager.Config), "Manager.Config"),
                    new KeyValuePair<object, string>(Manager.Game._instance ? Manager.Game.Instance : typeof(Manager.Game), "Manager.Game"),
                    new KeyValuePair<object, string>((object)Manager.Scene._instance ?? typeof(Manager.Scene), "Manager.Scene"),
                    new KeyValuePair<object, string>((object)Manager.Sound._instance ?? typeof(Manager.Sound), "Manager.Sound"),
                    new KeyValuePair<object, string>(typeof(Manager.GameSystem), "Manager.GameSystem"),
                };
            };

            CheatToolsWindow.Cheats.Add(new CheatEntry(_ => InsideH, DrawHSceneCheats, null));
            CheatToolsWindow.Cheats.Add(new CheatEntry(_ => InsideCommunication, DrawAdvCheats, null));
            CheatToolsWindow.Cheats.Add(new CheatEntry(_ => ExploreSceneInstance, DrawExploreCheats, null));
            CheatToolsWindow.Cheats.Add(new CheatEntry(_ => CurrentSaveData != null, DrawSavedataCheats, null));
            CheatToolsWindow.Cheats.Add(new CheatEntry(_ => CurrentSaveData?.NPCDataList?.Sum(x => x.Count) > 0, DrawGirlCheatMenu, "無法在此畫面編輯角色狀態，或沒有角色。請讀取存檔或開始新遊戲並將角色加入名冊。"));
            CheatToolsWindow.Cheats.Add(CheatEntry.CreateOpenInInspectorButtons(() => _openInInspectorButtons));

            Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private static void DrawHSceneCheats(CheatToolsWindow cheatToolsWindow)
        {
            var hScene = H.HScene._instance;

            GUILayout.Label("H 場景控制");

            var hflag = hScene.CtrlFlag;
            DrawUtils.DrawSlider("計量條上升速率", 0.001f, 0.5f, () => hflag.SpeedGuageRate, f => hflag.SpeedGuageRate = f, "快感計量條的上升速度");
            DrawUtils.DrawSlider("計量條下降速率", 0.001f, 0.5f, () => hflag.GuageDecreaseRate, f => hflag.GuageDecreaseRate = f, "快感計量條的下降速度（僅在極少數情況下發生）");

            var hGauge = hScene.Sprite.GaugeUI;
            DrawUtils.DrawSlider("男性計量條", 0f, 1f, () => hGauge._gaugeM.Value, f => hGauge._gaugeM.Value = f);
            DrawUtils.DrawSlider("女性計量條", 0f, 1f, () => hGauge._gaugeF.Value, f => hGauge._gaugeF.Value = f);

            if (GUILayout.Button("在檢視器中開啟 HScene"))
                Inspector.Instance.Push(new InstanceStackEntry(hScene, "H.HScene"), true);
        }

        private static void DrawAdvCheats(CheatToolsWindow cheatToolsWindow)
        {
            var commUi = ExploreSceneInstance.CommunicationUI;

            GUILayout.Label("ADV 場景控制");

            // TODO

            DrawUtils.DrawBool("顯示黑邊 (Letterbox)", () => commUi._objLetterBox.activeSelf, b => commUi._objLetterBox.SetActive(b));
        }


        private static void DrawExploreCheats(CheatToolsWindow cheatToolsWindow)
        {
            var expScene = ExploreSceneInstance;
            var cycle = expScene.SaveData.Cycle;

            Hooks.RiggedRng = GUILayout.Toggle(Hooks.RiggedRng, new GUIContent("操縱 RNG (成功率高於 0% 即成功)", null, "所有成功率至少 1% 的動作都將永遠成功。必須在與角色對話前啟用。\n警告：這將影響整個遊戲的 RNG。NPC（可能）將永遠成功執行他們的動作，這將嚴重扭曲模擬。某些事件可能永遠不會發生，或不斷重複，直到關閉此選項。"));

            GUILayout.Space(5);

            DrawUtils.DrawSlider("經過時間", 0, expScene._propertyData.Explore.LengthTimeZone, () => cycle.ElapsedTime, f => cycle.ElapsedTime = f);

            if (GUILayout.Button("無限時間限制 (直到遊戲重啟)"))
            {
                expScene._propertyData.Explore._lengthTimeZone = 100000;
                cycle.ElapsedTime = 0;
            }

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

            if (!InsideCommunication && !InsideH)
            {
                GUI.color = Color.red;
                if (GUILayout.Button(new GUIContent("我。是。磁鐵。", null, "警告：可能會導致遊戲卡死，請先儲存！您也許可以透過跳過目前時段來解除卡死。")))
                {
                    ExploreSceneInstance.Player._state.Release();

                    foreach (var npc in ExploreSceneInstance.NPCList)
                    {
                        // BUG: Can softlock in first person mode with no controls enabled other than wasd and right click
                        ExploreSceneInstance.CallNPC(npc)
                                            .ContinueWith((Il2CppSystem.Action)(() =>
                                            {
                                                // TODO find a way to ensure player is not softlocked, this doesn't really help
                                                ExploreSceneInstance._cycleUI.Visible = true;
                                            }));
                    }
                }
                GUI.color = Color.white;
            }
        }

        private static void DrawSavedataCheats(CheatToolsWindow cheatToolsWindow)
        {
            var savedata = CurrentSaveData;
            GUILayout.BeginVertical(GUI.skin.box);
            {
                var cycle = savedata.Cycle;

                DrawUtils.DrawNums("星期幾", 7, () => (byte)cycle.DayOfWeek, b => cycle.DayOfWeek = b, "星期日是第 1 天");
                DrawUtils.DrawInt("總日數", () => cycle.ElapsedDay, i => cycle.ElapsedDay = i, "遊戲中經過的總日數。");
                DrawUtils.DrawInt("總週數", () => cycle.ElapsedWeek, i => cycle.ElapsedWeek = i, "遊戲中經過的總週數。用於計算何時舉辦祭典。");

                var isFestivalWeek = cycle.IsFestivalWeek();
                GUILayout.Label($"IsFestivalWeek={isFestivalWeek}  IsShoppingWeek={cycle.IsShoppingWeek()}");

                // Skip to Sunday buttons
                {
                    var prevEnabled = GUI.enabled;
                    if (cycle.DayOfWeek is 6 or 0)
                        GUI.enabled = false;

                    void JumpToSunday(bool festival)
                    {
                        if (ExploreSceneInstance?.isActiveAndEnabled == true)
                        {
                            savedata.ChangeDay(DaysOfWeek.Saturday);
                            if (festival) while (!cycle.IsFestivalWeek()) cycle.ElapsedWeek++;
                            ExploreSceneInstance.ChangeNextCycle(new Il2CppSystem.Nullable<TimeZones>(TimeZones.Return), true, CancellationToken.None);
                        }
                        else
                        {
                            savedata.ChangeDay(DaysOfWeek.Sunday);
                            if (festival) while (!cycle.IsFestivalWeek()) cycle.ElapsedWeek++;
                        }
                    }

                    if (GUILayout.Button("跳至星期日")) JumpToSunday(false);

                    GUI.enabled = prevEnabled;
                    if (isFestivalWeek && cycle.DayOfWeek is 6 or 0)
                        GUI.enabled = false;

                    if (GUILayout.Button("跳至下一個祭典")) JumpToSunday(true);

                    GUI.enabled = prevEnabled;
                }
            }
            GUILayout.EndVertical();

            var playerData = savedata.PlayerData;
            if (playerData != null)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                // Do not use .Tastes because it always returns 80 for some reason
                for (var i = 0; i < playerData._tastes.Length; i++)
                {
                    var thisIndex = i;
                    DrawUtils.DrawSlider("味覺 " + (i + 1), 0, 20, () => playerData._tastes[thisIndex], val =>
                    {
                        playerData._tastes[thisIndex] = (byte)val;
                        if (InsideCommunication)
                            ExploreSceneInstance.CommunicationUI.RefreshTasteGraph();
                    });
                }

                if (GUILayout.Button("檢視 SaveData.PlayerData"))
                    Inspector.Instance.Push(new InstanceStackEntry(playerData, "PlayerData"), true);

                GUILayout.EndVertical();
            }
        }

        private static NPCData[] GetCurrentActors()
        {
            if (CurrentSaveData?.NPCDataList == null) return Array.Empty<NPCData>();

            // todo faster?
            var allNpcs = CurrentSaveData.NPCDataList.SelectMany(x => x).Where(x => x?.NPCInstance?.BaseData != null).ToDictionary(x => x.NPCInstance.BaseData, x => x);
            if (InsideH)
            {
                return HScene.Instance._hActorAll.Where(x => x?.ActorData != null).Select(x =>
                {
                    allNpcs.TryGetValue(x.ActorData, out var npcd);
                    return npcd;
                }).Where(x => x != null).ToArray();
            }

            if (InsideCommunication)
            {
                return ExploreSceneInstance.CommunicationUI._targets.AsManagedEnumerable().Where(x => x?.BaseData != null).Select(x =>
                {
                    allNpcs.TryGetValue(x.BaseData, out var npcd);
                    return npcd;
                }).Where(x => x != null).ToArray();
            }

            //return allNpcs.Values.ToArray();
            return Array.Empty<NPCData>();
        }

        private static void DrawGirlCheatMenu(CheatToolsWindow cheatToolsWindow)
        {
            var npcList = CurrentSaveData.NPCDataList.SelectMany(x => x).Where(x => x != null).ToList();

            GUILayout.Label("角色狀態編輯器");

            // TODO

            foreach (var chara in GetCurrentActors())
            {
                if (GUILayout.Button($"選擇 {chara.HumanData.GetCharaName(true) ?? chara.CharaFileName}"))
                {
                    _currentVisibleChara = chara;
                }
            }

            GUILayout.Space(6);

            try
            {
                if (_currentVisibleChara != null)
                    DrawSingleCharaCheats(_currentVisibleChara, cheatToolsWindow);
                else
                    GUILayout.Label("請選擇一個角色來編輯其狀態");
            }
            catch (Exception e)
            {
                CheatToolsPlugin.Logger.LogError(e);
                _currentVisibleChara = null;
            }
        }

        private static void DrawSingleCharaCheats(NPCData currentChara, CheatToolsWindow cheatToolsWindow)
        {
            var charaName = currentChara.HumanData.GetCharaName(true);

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("已選擇：", IMGUIUtils.LayoutOptionsExpandWidthFalse);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(charaName, IMGUIUtils.LayoutOptionsExpandWidthFalse);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("關閉", IMGUIUtils.LayoutOptionsExpandWidthFalse)) _currentVisibleChara = null;
                }
                GUILayout.EndHorizontal();

                void UpdateUiIfNeeded(bool isLoveTalk = false)
                {
                    if (InsideCommunication)
                        ExploreSceneInstance.CommunicationUI.UpdateParameter(isLoveTalk);
                }

                DrawUtils.DrawNums("關係等級", 4, () => currentChara.RelationValue, b =>
                {
                    currentChara.RelationValue = b;
                    currentChara.FavorValue = Math.Max(currentChara.FavorValue, currentChara.RelationValue * 100);
                    UpdateUiIfNeeded(true);
                }, "1 - 陌生人, 2 - 朋友, 3 - 好朋友, 4 - 戀人。\n警告：可能不會立即更新，如有問題請儲存/載入遊戲。");

                DrawUtils.DrawSlider("好感度", 0, 100, () => currentChara.FavorValue - currentChara.RelationValue * 100, i =>
                {
                    currentChara.FavorValue = i + currentChara.RelationValue * 100;
                    UpdateUiIfNeeded();
                });

                GUILayout.Space(5);

                GUILayout.Label($"心情等級={currentChara.MoodLevel}  親密度等級={currentChara.IntimacyRank}  好色度狀態={currentChara.LewdnessState}");
                DrawUtils.DrawSlider("心情", 0, 100, () => currentChara.Mood, i =>
                {
                    currentChara.Mood = i;
                    UpdateUiIfNeeded();
                });
                DrawUtils.DrawSlider("親密度", 0, 100, () => currentChara.Intimacy, i =>
                {
                    currentChara.Intimacy = i;
                    UpdateUiIfNeeded();
                });
                DrawUtils.DrawSlider("好色度", 0, 100, () => currentChara.LewdnessValue, i =>
                {
                    currentChara.LewdnessValue = i;
                    UpdateUiIfNeeded();
                });

                GUILayout.Space(5);

                DrawUtils.DrawSlider("H 經驗", 0, 100, () => currentChara.Sexperience, i =>
                {
                    currentChara.Sexperience = i;
                    UpdateUiIfNeeded();
                }, "H 場景結束時看到的經驗條。");
                DrawUtils.DrawBool("是處女", () => currentChara.IsVirgin, b => currentChara.IsVirgin = b);
                DrawUtils.DrawBool("是肛交處女", () => currentChara.IsAnalVirgin, b => currentChara.IsAnalVirgin = b);
                DrawUtils.DrawInt("H 次數", () => currentChara.HCountValue, b =>
                {
                    var change = b - currentChara.HCountValue;
                    if (change > 0)
                    {
                        for (; change > 0; change--)
                            currentChara.AddHCount();
                    }
                    else
                    {
                        currentChara.HCountValue = b;
                    }
                });

                GUILayout.Space(5);

                if (!InsideCommunication && !InsideH)
                {
                    GUI.color = Color.red;
                    if (GUILayout.Button(new GUIContent("呼叫", null, "警告：在某些情況下可能導致遊戲卡死，使用前請先儲存！")))
                        ExploreSceneInstance.CallNPC(currentChara.NPCInstance);
                    GUI.color = Color.white;
                }
#if DEBUG
                if (GUILayout.Button("DEBUG: try update UI"))
                {
                    ExploreSceneInstance.CommunicationUI.UpdateParameter(true);
                    ExploreSceneInstance.CommunicationUI.UpdateCameraAngle();
                    ExploreSceneInstance.CommunicationUI.UpdateTasteGraph();
                    ExploreSceneInstance.CommunicationUI.RefreshTasteGraph();
                }
#endif
                GUILayout.Space(5);

                if (GUILayout.Button("在檢視器中開啟角色"))
                    Inspector.Instance.Push(new InstanceStackEntry(currentChara, "NPCData " + charaName), true);

                if (GUILayout.Button("導航至角色的遊戲物件 (GameObject)"))
                {
                    if (currentChara.NPCInstance?.Transform)
                        ObjectTreeViewer.Instance.SelectAndShowObject(currentChara.NPCInstance.Transform);
                    else
                        CheatToolsPlugin.Logger.Log(LogLevel.Warning | LogLevel.Message, "角色沒有分配身體模型");
                }
            }
            GUILayout.EndVertical();
        }
    }
}