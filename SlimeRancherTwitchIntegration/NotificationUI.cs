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
        private static Color backgroundColor = new Color(Color.gray.r, Color.gray.g, Color.gray.b, .9f);
        private static GUIStyle guiStyle = new GUIStyle();

        private static List<string> queue = new List<string>();

        private static bool display = false;
        private static int displayLength = 5000;
        private static DateTime displayStart;

        public static void AttachWindow(Scene oldScene, Scene newScene)
        {
            guiStyle.fontSize = 20;
            guiStyle.wordWrap = true;
            guiStyle.padding = new RectOffset(15, 15, 15, 15);
            DontDestroyOnLoad(new GameObject("_ChatWindow", new Type[1]
            {
                typeof (NotificationUI)
            }));
            SceneManager.activeSceneChanged -= new UnityAction<Scene, Scene>(AttachWindow);
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
                if (timeIn < 500)
                {
                    float completePercent = timeIn / 500;
                    width = 400 * completePercent;
                    guiStyle.normal.textColor = new Color(Color.white.r, Color.white.g, Color.white.b, completePercent);
                }
                else if (timeIn >= displayLength - 500)
                {
                    float completePercent = (displayLength - timeIn) / 500;
                    width = 400 * completePercent;
                    guiStyle.normal.textColor = new Color(Color.white.r, Color.white.g, Color.white.b, completePercent);
                }
                else
                {
                    width = 400f;
                    guiStyle.normal.textColor = Color.white;
                }
                Color oldColor = GUI.color;
                GUI.color = backgroundColor;
                GUI.Box(new Rect((Screen.width / 2) - 100, Screen.height / 2 - 50, width, 100f), "");
                GUI.color = oldColor;

                GUI.Label(new Rect((Screen.width / 2) - 100, Screen.height / 2 - 50, 400f, 100f), queue[0], guiStyle);

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
