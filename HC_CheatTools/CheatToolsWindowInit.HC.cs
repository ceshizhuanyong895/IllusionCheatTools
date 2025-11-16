using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Character;
using RuntimeUnityEditor.Core.Inspector;
using RuntimeUnityEditor.Core.Inspector.Entries;
using RuntimeUnityEditor.Core.ObjectTree;
using UnityEngine;

namespace CheatTools
{
    public static class CheatToolsWindowInit
    {
        private static KeyValuePair<object, string>[] _openInInspectorButtons;
        private static Human _currentVisibleGirl;

        public static void Initialize(CheatToolsPlugin instance)
        {
            CheatToolsWindow.OnShown += _ =>
            {
                _openInInspectorButtons = new[]
                {
                    //new KeyValuePair<object, string>(_hScene, "HSceneFlagCtrl.instance"),
                    new KeyValuePair<object, string>(Manager.HSceneManager._instance, "Manager.HSceneManager.instance"),
                    new KeyValuePair<object, string>(HC.Scene.ADVScene._instance, "ADVScene.instance"),
                    new KeyValuePair<object, string>((object)Manager.Game._instance ?? typeof(Manager.Game), "Manager.Game"),
                    new KeyValuePair<object, string>(Manager.Game.SaveData, "Manager.Game.SaveData"),
                    new KeyValuePair<object, string>(typeof(Manager.Config), "Manager.Config"),
                    new KeyValuePair<object, string>((object)Manager.Scene._instance ?? typeof(Manager.Scene), "Manager.Scene"),
                    new KeyValuePair<object, string>((object)Manager.Sound._instance ?? typeof(Manager.Sound), "Manager.Sound"),
                    new KeyValuePair<object, string>(typeof(Manager.GameSystem), "Manager.GameSystem"),
                    new KeyValuePair<object, string>(typeof(Manager.Map), "Manager.Map")
                };
            };

            CheatToolsWindow.Cheats.Add(new CheatEntry(w => H.HSceneFlagCtrl._instance != null, DrawHSceneCheats, null));
            CheatToolsWindow.Cheats.Add(new CheatEntry(w => H.HSceneFlagCtrl._instance != null && Manager.HSceneManager._instance != null, DrawGirlCheatMenu, "無法在此畫面編輯角色狀態。\n您必須開始 H 場景，編輯角色，並完成 H 場景才能儲存變更。"));
            CheatToolsWindow.Cheats.Add(CheatEntry.CreateOpenInInspectorButtons(() => _openInInspectorButtons));
            CheatToolsWindow.Cheats.Add(new CheatEntry(w => Manager.Game.SaveData != null, DrawGlobalUnlocks, null));

            //Harmony.CreateAndPatchAll(typeof(Hooks));
        }

        private static void DrawGlobalUnlocks(CheatToolsWindow obj)
        {
            GUILayout.Label("危險區域！這些作弊是永久性的，除非重設存檔，否則無法復原。");

            if (GUILayout.Button("獲得所有成就", GUILayout.ExpandWidth(true)))
            {
                var achievementKeys = new List<int>();
                foreach (var achievementKey in Manager.Game.SaveData.Achievement.Keys)
                    achievementKeys.Add(achievementKey);

                foreach (var achievementKey in achievementKeys)
                    HC.SaveData.SaveData.UnlockAchievement(achievementKey);
            }

            if (GUILayout.Button("解鎖所有特權", GUILayout.ExpandWidth(true)))
            {
                var achievementKeys = new List<int>();
                foreach (var achievementKey in Manager.Game.SaveData.AchievementExchange.Keys)
                    achievementKeys.Add(achievementKey);

                foreach (var achievementKey in achievementKeys)
                    HC.SaveData.SaveData.UnlockAchievementExchange(achievementKey);
            }
        }

        private static void DrawHSceneCheats(CheatToolsWindow cheatToolsWindow)
        {
            var hScene = H.HSceneFlagCtrl._instance;

            GUILayout.Label("H 場景控制");

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("男性計量條： " + hScene.Feel_m.ToString("F2"), GUILayout.Width(150));
                hScene.Feel_m = GUILayout.HorizontalSlider(hScene.Feel_m, 0, 1);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("女性計量條： " + hScene.Feel_f.ToString("F2"), GUILayout.Width(150));
                hScene.Feel_f = GUILayout.HorizontalSlider(hScene.Feel_f, 0, 1);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("痛苦計量條： " + hScene.FeelPain.ToString("F2"), GUILayout.Width(150));
                hScene.FeelPain = GUILayout.HorizontalSlider(hScene.FeelPain, 0, 1);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("打屁股計量條： " + hScene.FeelSpnking.ToString("F2"), GUILayout.Width(150));
                hScene.FeelSpnking = GUILayout.HorizontalSlider(hScene.FeelSpnking, 0, 1);
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("在檢視器中開啟 HScene Flags"))
                Inspector.Instance.Push(new InstanceStackEntry(hScene, "HSceneFlagCtrl"), true);
        }

        internal static string GetHeroineName(Human heroine)
        {
            return !string.IsNullOrEmpty(heroine.fileParam?.fullname) ? heroine.fileParam.fullname : heroine.name;
        }

        private static void DrawGirlCheatMenu(CheatToolsWindow cheatToolsWindow)
        {
            GUILayout.Label("角色狀態編輯器");

            var visibleGirls = Manager.HSceneManager._instance.Females; //Character.Human._list;

            for (var i = 0; i < visibleGirls.Count; i++)
            {
                var girl = visibleGirls[i];
                if (girl == null) continue;
                if (GUILayout.Button($"選擇 #{i} - {GetHeroineName(girl)}"))
                    _currentVisibleGirl = girl;
            }

            GUILayout.Space(6);

            if (_currentVisibleGirl != null)
                DrawSingleGirlCheats(_currentVisibleGirl);
            else
                GUILayout.Label("請選擇一個角色來編輯其狀態");
        }

        private static void DrawSingleGirlCheats(Human currentAdvGirl)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("已選女主角名稱： " + GetHeroineName(currentAdvGirl));
                GUILayout.Space(6);

                var gi = currentAdvGirl.fileGameInfo;
                if (gi != null)
                {
                    //var anyChanges = false;

                    void DrawSingleStateBtn(ChaFileDefine.State state)
                    {
                        if (GUILayout.Button(state.ToString()))
                        {
                            gi.nowState = state;
                            gi.calcState = state;
                            gi.nowDrawState = state;
                            gi.Favor = state == ChaFileDefine.State.Favor ? 100 : Mathf.Min(gi.Favor, 90);
                            gi.Enjoyment = state == ChaFileDefine.State.Enjoyment ? 100 : Mathf.Min(gi.Enjoyment, 90);
                            gi.Aversion = state == ChaFileDefine.State.Aversion ? 100 : Mathf.Min(gi.Aversion, 90);
                            gi.Slavery = state == ChaFileDefine.State.Slavery ? 100 : Mathf.Min(gi.Slavery, 90);
                            gi.Broken = state == ChaFileDefine.State.Broken ? 100 : Mathf.Min(gi.Broken, 90);
                            gi.Dependence = state == ChaFileDefine.State.Dependence ? 100 : Mathf.Min(gi.Dependence, 90);
                            //anyChanges = true;
                        }
                    }

                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("目前狀態： " + gi.nowState);
                        GUILayout.FlexibleSpace();
                        DrawSingleStateBtn(ChaFileDefine.State.Blank);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        DrawSingleStateBtn(ChaFileDefine.State.Favor);
                        DrawSingleStateBtn(ChaFileDefine.State.Enjoyment);
                        DrawSingleStateBtn(ChaFileDefine.State.Aversion);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    {
                        DrawSingleStateBtn(ChaFileDefine.State.Slavery);
                        DrawSingleStateBtn(ChaFileDefine.State.Broken);
                        DrawSingleStateBtn(ChaFileDefine.State.Dependence);
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.Space(6);

                    GUILayout.Label("統計數據：");

                    void ShowSingleSlider(string name, Action<int> set, Func<int> get)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            var status = get();
                            GUILayout.Label(name + "： " + status, GUILayout.Width(120));
                            var newStatus = Mathf.RoundToInt(GUILayout.HorizontalSlider(status, 0, 100));
                            if (newStatus != status)
                            {
                                set(newStatus);
                                //anyChanges = true;
                            }
                        }
                        GUILayout.EndHorizontal();
                    }

                    void ShowSingleTextfield(string name, Action<int> set, Func<int> get)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(name + "： ", GUILayout.Width(120));
                            GUI.changed = false;
                            var status = get();
                            var textField = GUILayout.TextField(status.ToString());
                            if (GUI.changed && int.TryParse(textField, out var newStatus) && newStatus != status)
                            {
                                set(newStatus);
                                //anyChanges = true;
                            }

                            GUI.changed = false;
                        }
                        GUILayout.EndHorizontal();
                    }

                    ShowSingleSlider("好感度", i => gi.Favor = i, () => gi.Favor);
                    ShowSingleSlider("愉悅", i => gi.Enjoyment = i, () => gi.Enjoyment);
                    ShowSingleSlider("厭惡", i => gi.Aversion = i, () => gi.Aversion);
                    ShowSingleSlider("奴役", i => gi.Slavery = i, () => gi.Slavery);
                    ShowSingleSlider("崩壞", i => gi.Broken = i, () => gi.Broken);
                    ShowSingleSlider("依賴", i => gi.Dependence = i, () => gi.Dependence);
                    ShowSingleSlider("骯髒", i => gi.Dirty = i, () => gi.Dirty);
                    ShowSingleSlider("疲勞", i => gi.Tiredness = i, () => gi.Tiredness);
                    ShowSingleSlider("尿意", i => gi.Toilet = i, () => gi.Toilet);
                    ShowSingleSlider("性慾", i => gi.Libido = i, () => gi.Libido);

                    ShowSingleSlider("警戒", i => gi.alertness = i, () => gi.alertness);

                    ShowSingleTextfield("H 次數", i => { gi.hCount = i; if (i == 0) gi.firstHFlag = true; }, () => gi.hCount);

                    //todo allow changing in lobby, needed for persisting the changes
                    // if (anyChanges)
                    //     _onGirlStatsChanged(_currentVisibleGirl);

                    if (GUILayout.Button("查看更多狀態與旗標"))
                        Inspector.Instance.Push(new InstanceStackEntry(gi, "女主角 " + GetHeroineName(currentAdvGirl)), true);
                }

                GUILayout.Space(6);

                if (GUILayout.Button("導航至女主角的遊戲物件 (GameObject)"))
                {
                    if (currentAdvGirl.transform != null)
                        ObjectTreeViewer.Instance.SelectAndShowObject(currentAdvGirl.transform);
                    else
                        CheatToolsPlugin.Logger.Log(LogLevel.Warning | LogLevel.Message, "女主角沒有分配身體模型");
                }

                if (GUILayout.Button("在檢視器中開啟女主角"))
                    Inspector.Instance.Push(new InstanceStackEntry(currentAdvGirl, "女主角 " + GetHeroineName(currentAdvGirl)), true);

                //if (GUILayout.Button("Inspect extended data"))
                //{
                //    Inspector.Instance.Push(new InstanceStackEntry(ExtensibleSaveFormat.ExtendedSave.GetAllExtendedData(currentAdvGirl.chaFile), "ExtData for " + currentAdvGirl.Name), true);
                //}
            }
            GUILayout.EndVertical();
        }

        //private static class Hooks
        //{
        //}
    }
}