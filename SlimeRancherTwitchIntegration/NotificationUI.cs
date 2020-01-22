using HarmonyLib;
using SRML.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SlimeRancherTwitchIntegration
{
    class NotificationUI : MonoBehaviour
    {
        private static GUIStyle guiStyle = new GUIStyle();
        private static GUIStyle boxStyle = new GUIStyle();

        private static List<string> queue = new List<string>();

        private static bool display = false;
        private static int displayLength = 7000;
        private static DateTime displayStart;

        public static void AttachWindow(Scene oldScene, Scene newScene)
        {
            guiStyle.fontSize = 20;
            guiStyle.wordWrap = true;
            DontDestroyOnLoad(new GameObject("_ChatWindow", new Type[1]
            {
        typeof (NotificationUI)
            }));
            SceneManager.activeSceneChanged -= new UnityAction<Scene, Scene>(NotificationUI.AttachWindow);
            SRML.Console.Console.Log("Attached Chat Window successfully!", true);
        }

        public static void addChatMessage(string message)
        {
            queue.Add(message);
            if (queue.Count == 1)
            {
                display = true;
                displayStart = DateTime.UtcNow;
            }
        }

        private void OnGUI()
        {
            if (Levels.IsLevel(Levels.WORLD) && display)
            {
                float timeIn = (float)(DateTime.UtcNow - displayStart).TotalMilliseconds;
                float width = 0;
                if (timeIn < 1000)
                {
                    float completePercent = timeIn / 1000f;
                    width = 400 * completePercent;
                    guiStyle.normal.textColor = new Color(Color.red.r, Color.red.g, Color.red.b, completePercent);
                }
                else if (timeIn >= displayLength - 1000)
                {
                    float completePercent = (displayLength - timeIn) / 1000f;
                    width = 400 * completePercent;
                    guiStyle.normal.textColor = new Color(Color.red.r, Color.red.g, Color.red.b, completePercent);
                }
                else
                {
                    width = 400f;
                    guiStyle.normal.textColor = Color.red;
                }
                Color oldColor = GUI.color;
                GUI.color = Color.gray;
                GUI.Box(new Rect((Screen.width / 2) - 100, Screen.height / 2, width, 100f), "");
                GUI.color = oldColor;

                GUI.Label(new Rect((Screen.width / 2) - 100, Screen.height / 2, 400f, 100f), queue[0], guiStyle);

                if (timeIn / displayLength > 1)
                {
                    queue.RemoveAt(0);
                    if (queue.Count == 0)
                        display = false;
                    else
                        displayStart = DateTime.UtcNow;
                }
            }
        }
    }
}
