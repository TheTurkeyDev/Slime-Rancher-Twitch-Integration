using HarmonyLib;
using InControl;
using MonomiPark.SlimeRancher.Regions;
using SlimeRancherTwitchIntegration;
using SRML;
using SRML.SR;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using static Ammo;

namespace TwitchIntegration
{
    public class Main : ModEntryPoint
    {
        public static ConcurrentQueue<RewardData> rewardsQueue = new ConcurrentQueue<RewardData>();

        // Called before GameContext.Awake
        // You want to register new things and enum values here, as well as do all your harmony patching
        public override void PreLoad()
        {
            HarmonyInstance.PatchAll();
            SceneManager.activeSceneChanged += new UnityAction<Scene, Scene>(ChatWindow.AttachWindow);
        }


        // Called before GameContext.Start
        // Used for registering things that require a loaded gamecontext
        public override void Load()
        {
        }

        // Called after all mods Load's have been called
        // Used for editing existing assets in the game, not a registry step
        public override void PostLoad()
        {
            StartConnection();
            Debug.Log("Twitch Integration started. Prepare for fun chat!");
        }

        [HarmonyPatch(typeof(SceneContext), "LateUpdate")]
        class Patch
        {
            static void Prefix()
            {
                if (Levels.IsLevel(Levels.WORLD))
                {
                    Transform playerLoc = SceneContext.Instance.Player.transform;
                    RewardData reward;
                    if (rewardsQueue.TryDequeue(out reward))
                    {
                        switch (reward.action)
                        {
                            case "sound":
                                break;
                            case "inventory_bomb":
                                Ammo ammo = SceneContext.Instance.PlayerState.Ammo;
                                foreach (Slot s in (Slot[])Traverse.Create(ammo).Property("Slots").GetValue())
                                {
                                    if (s == null)
                                        continue;
                                    for (int i = 0; i < s.count; i++)
                                    {
                                        GameObject slotObject = GameContext.Instance.LookupDirector.GetPrefab(s.id);
                                        RegionMember region = (RegionMember)Traverse.Create(SceneContext.Instance.PlayerZoneTracker).Field("member").GetValue();
                                        GameObject gameObject = SRBehaviour.InstantiateActor(slotObject, region.setId, playerLoc.position + playerLoc.forward * 0.5f, playerLoc.rotation, false);
                                        Rigidbody component = gameObject.GetComponent<Rigidbody>();
                                        component.AddForce((playerLoc.forward * 100f + UnityEngine.Random.insideUnitSphere * 100f) * component.mass);
                                    }
                                }
                                ammo.Clear();
                                break;
                        }
                    }
                }
            }
        }


        //public void OnBindingAdded(PlayerAction action, BindingSource source){ }

        private static readonly CancellationTokenSource source = new CancellationTokenSource();

        //Tells the connection to shut down
        public static void Shutdown()
        {
            source.Cancel();
        }

        //Starts and handles the connection
        public static void StartConnection()
        {
            CancellationToken token = source.Token;
            if (!token.IsCancellationRequested)
            {
                //Starts the connection task on a new thread
                Task.Factory.StartNew(() =>
                {
                    //Keep making new pipes
                    while (!token.IsCancellationRequested)
                    {
                        //Catch any errors
                        try
                        {
                            //pipeName is the same as your subfolder name in the Integrations folder of the app
                            using (NamedPipeClientStream client = new NamedPipeClientStream(".", "SlimeRancher", PipeDirection.In))
                            {
                                using (StreamReader reader = new StreamReader(client))
                                {
                                    //Keep trying to connect
                                    while (!token.IsCancellationRequested && !client.IsConnected)
                                    {
                                        try
                                        {
                                            client.Connect(1000);//Don't wait too long, so mod can shut down quickly if still trying to connect
                                        }
                                        catch (TimeoutException)
                                        {
                                            //Ignore
                                        }
                                        catch (System.ComponentModel.Win32Exception)
                                        {
                                            //Ignore and sleep for a bit, since the connection didn't time out
                                            Thread.Sleep(500);
                                        }
                                    }
                                    //Keep trying to read
                                    while (!token.IsCancellationRequested && client.IsConnected && !reader.EndOfStream)
                                    {

                                        //Read line from stream
                                        string line = reader.ReadLine();

                                        if (line != null)
                                        {
                                            if (line.StartsWith("Action: "))
                                            {
                                                string[] data = line.Substring(8).Split(' ');
                                                rewardsQueue.Enqueue(new RewardData(data[0], data.Skip(1).ToArray()));
                                                //Handle action message. This is what was generated by your IntegrationAction Execute method
                                                //Make sure you handle it on the correct thread
                                            }
                                            else if (line.StartsWith("Message: "))
                                            {
                                                string message = line.Substring(9);
                                                ChatWindow.addChatMessage(message);
                                            }
                                        }
                                        //Only read every 50ms
                                        Thread.Sleep(50);
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            //Ignore
                        }
                    }

                }, token);
            }
        }

        public class RewardData
        {
            public string action;
            public string[] args;

            public RewardData(string action, string[] args)
            {
                this.action = action;
                if (args == null)
                    this.args = new string[0];
                else
                    this.args = args;
            }
        }
    }
}