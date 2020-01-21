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
    class ChatWindow : MonoBehaviour
    {
        private static GUIStyle guiStyle = new GUIStyle();

        private static List<string> chat = new List<string>();

        public static void AttachWindow(Scene oldScene, Scene newScene)
        {
            chat.Add("Twitch Integration Console");
            chat.Add("=====================");
            chat.Add("Hello!");
            guiStyle.fontSize = 20;
            guiStyle.normal.textColor = Color.red;
            guiStyle.wordWrap = true;
            DontDestroyOnLoad(new GameObject("_ChatWindow", new Type[1]
            {
        typeof (ChatWindow)
            }));
            SceneManager.activeSceneChanged -= new UnityAction<Scene, Scene>(ChatWindow.AttachWindow);
            SRML.Console.Console.Log("Attached Chat Window successfully!", true);
        }

        public static void addChatMessage(string message)
        {
            chat.Add(message);
        }

        private void OnGUI()
        {
            if (Levels.IsLevel(Levels.WORLD))
                GUI.Label(new Rect(Screen.width - 425f, Screen.height - 125f, 400f, 100f), string.Join("\n", chat.ToArray()), guiStyle);
        }
    }
}
