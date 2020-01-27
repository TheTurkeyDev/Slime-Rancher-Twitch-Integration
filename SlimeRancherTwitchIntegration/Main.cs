using HarmonyLib;
using MonomiPark.SlimeRancher.DataModel;
using MonomiPark.SlimeRancher.Regions;
using SlimeRancherTwitchIntegration;
using SRML;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public static List<UpgradeHolder> plotsEdited = new List<UpgradeHolder>();
        public static List<DelayedDowngrade> delayedDowngrades = new List<DelayedDowngrade>();
        public static bool push = false;
        public static PushData pushData;

        // Called before GameContext.Awake
        // You want to register new things and enum values here, as well as do all your harmony patching
        public override void PreLoad()
        {
            HarmonyInstance.PatchAll();
            SceneManager.activeSceneChanged += new UnityAction<Scene, Scene>(NotificationUI.AttachWindow);
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
                if (Levels.IsLevel(Levels.WORLD) && SRInput.Instance.GetInputMode() != SRInput.InputMode.PAUSE)
                {
                    DateTime currentTime = DateTime.UtcNow;
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
                                        spawnObject(s.id, playerLoc);
                                    }
                                }
                                ammo.Clear();
                                break;
                            case "meteor_shower":
                                break;
                            case "downgrade_plot":
                                int duration;
                                int.TryParse(reward.args[0], out duration);
                                int numPlots;
                                int.TryParse(reward.args[1], out numPlots);
                                bool removeAllWalls;
                                bool.TryParse(reward.args[2], out removeAllWalls);
                                delayedDowngrades.Add(new DelayedDowngrade(numPlots, duration, removeAllWalls));
                                break;
                            case "spawn_object":
                                int objectID;
                                int.TryParse(reward.args[0], out objectID);
                                int ammount;
                                int.TryParse(reward.args[1], out ammount);
                                if (Enum.IsDefined(typeof(Identifiable.Id), objectID))
                                {
                                    Identifiable.Id obj = (Identifiable.Id)objectID;
                                    for (int i = 0; i < ammount; i++)
                                        spawnObject(obj, playerLoc);
                                }
                                else
                                {
                                    Debug.LogError("Could not spawn in object with the ID of " + objectID + ". INVALID ID!");
                                }
                                break;
                            case "push_player":
                                float pushMin = 0.1f;
                                float.TryParse(reward.args[0], out pushMin);
                                float pushMax = 0.3f;
                                float.TryParse(reward.args[1], out pushMax);
                                if (push)
                                {
                                    pushData.left += 8;
                                }
                                else
                                {
                                    push = true;
                                    pushData = new PushData(pushMin, pushMax, currentTime, 250);
                                }
                                break;
                            case "adjust_money":
                                int amountToAdjust;
                                int.TryParse(reward.args[0], out amountToAdjust);
                                if (amountToAdjust < 0)
                                {
                                    if (SceneContext.Instance.PlayerState.GetCurrency() < amountToAdjust)
                                        amountToAdjust = SceneContext.Instance.PlayerState.GetCurrency();
                                    SceneContext.Instance.PlayerState.SpendCurrency(amountToAdjust);
                                }
                                else
                                {
                                    SceneContext.Instance.PlayerState.AddCurrency(amountToAdjust);
                                }
                                break;
                            case "shoot_gun":
                                WeaponVacuum vacuum = SRSingleton<SceneContext>.Instance.Player.GetComponentInChildren<WeaponVacuum>();
                                Traverse.Create(vacuum).Method("Expel", new HashSet<GameObject>()).GetValue();
                                break;
                        }
                    }

                    for (int i = plotsEdited.Count - 1; i >= 0; i--)
                    {
                        UpgradeHolder plot = plotsEdited[i];
                        if ((currentTime - plot.Spawned).TotalMilliseconds > plot.Duration)
                        {
                            plotsEdited.RemoveAt(i);
                            plot.Reset();
                        }
                    }

                    if (push)
                    {
                        try
                        {
                            SceneContext.Instance.Player.transform.position += pushData.push;
                        }
                        catch
                        {
                            Debug.LogError("PLAYER (or related) IS NULL! Unable to push the player!");
                        }
                        if ((currentTime - pushData.startTime).TotalMilliseconds > pushData.duration)
                        {
                            pushData.left--;
                            if (pushData.left == 0)
                            {
                                push = false;
                            }
                            else
                            {
                                pushData.startTime = currentTime;
                                pushData.nextPush();
                            }
                        }
                    }

                    for (int i = delayedDowngrades.Count - 1; i >= 0; i--)
                    {
                        DelayedDowngrade dd = delayedDowngrades[i];

                        List<LandPlotModel> validplots = new List<LandPlotModel>();
                        //this.GetComponentInParent<Region>();
                        int corralPlots = 0;
                        RegionMember currentRegion = (RegionMember)Traverse.Create(SceneContext.Instance.PlayerZoneTracker).Field("member").GetValue();
                        foreach (LandPlotModel plot in SceneContext.Instance.GameModel.AllLandPlots().Values)
                        {
                            if (plot.typeId == LandPlot.Id.CORRAL)
                            {
                                corralPlots++;
                                if (currentRegion.setId == ((GameObject)Traverse.Create(plot).Field("gameObj").GetValue()).GetComponentInParent<Region>().setId)
                                {
                                    validplots.Add(plot);
                                }
                            }
                        }

                        Debug.LogError("Corral Plots: " + corralPlots + " Valid Plots: " + validplots.Count);

                        if (corralPlots != 0 && validplots.Count == 0)
                            continue;

                        delayedDowngrades.RemoveAt(i);

                        while (validplots.Count > 0 && dd.numPlots != 0)
                        {
                            LandPlotModel plotModel = validplots.ToArray()[UnityEngine.Random.Range(0, validplots.Count())];
                            validplots.Remove(plotModel);
                            dd.numPlots--;
                            LandPlot plot = ((GameObject)Traverse.Create(plotModel).Field("gameObj").GetValue()).GetComponentInChildren<LandPlot>();
                            HashSet<LandPlot.Upgrade> upgrades = new HashSet<LandPlot.Upgrade>();
                            foreach (PlotUpgrader component in plot.GetComponents<PlotUpgrader>())
                            {
                                if (component is AirNetUpgrader && plot.HasUpgrade(LandPlot.Upgrade.AIR_NET))
                                {
                                    upgrades.Add(LandPlot.Upgrade.AIR_NET);
                                    foreach (GameObject airNet in ((AirNetUpgrader)component).airNets)
                                        airNet.SetActive(false);
                                }
                                else if (component is SolarShieldUpgrader && plot.HasUpgrade(LandPlot.Upgrade.SOLAR_SHIELD))
                                {
                                    upgrades.Add(LandPlot.Upgrade.SOLAR_SHIELD);
                                    foreach (GameObject shield in ((SolarShieldUpgrader)component).shields)
                                        shield.SetActive(false);
                                }
                                else if (component is WallUpgrader && plot.HasUpgrade(LandPlot.Upgrade.WALLS))
                                {
                                    upgrades.Add(LandPlot.Upgrade.WALLS);
                                    ((WallUpgrader)component).standardWalls.SetActive(!dd.removeAllWalls);
                                    ((WallUpgrader)component).upgradeWalls.SetActive(false);
                                }
                                plotsEdited.Add(new UpgradeHolder(plot, upgrades, dd.duration * 1000));
                            }
                        }
                    }
                }
            }

            private static void spawnObject(Identifiable.Id obj, Transform location)
            {
                GameObject slotObject = GameContext.Instance.LookupDirector.GetPrefab(obj);
                RegionMember region = (RegionMember)Traverse.Create(SceneContext.Instance.PlayerZoneTracker).Field("member").GetValue();
                Vector3 vec = UnityEngine.Random.insideUnitSphere;
                vec.y = Math.Abs(vec.y);
                GameObject gameObject = SRBehaviour.InstantiateActor(slotObject, region.setId, location.position + vec, location.rotation, false);
                Rigidbody component = gameObject.GetComponent<Rigidbody>();
                component.AddForce(UnityEngine.Random.insideUnitSphere * 25f);
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
                                                NotificationUI.addChatMessage(message);
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

        public class PushData
        {
            private float minPush;
            private float maxPush;
            public Vector3 push;
            public DateTime startTime;
            public int duration;
            public int left = 8;

            public PushData(float minPush, float maxPush, DateTime startTime, int duration)
            {
                this.minPush = minPush;
                this.maxPush = maxPush;
                this.startTime = startTime;
                this.duration = duration;
                nextPush();
            }

            public void nextPush()
            {
                push = getBoundedRandVector(minPush, maxPush);
            }

            private Vector3 getBoundedRandVector(float min, float max)
            {
                float x;
                float z;
                if (UnityEngine.Random.value > 0.5)
                    x = UnityEngine.Random.Range(-max, -min);
                else
                    x = UnityEngine.Random.Range(min, max);

                if (UnityEngine.Random.value > 0.5)
                    z = UnityEngine.Random.Range(-max, -min);
                else
                    z = UnityEngine.Random.Range(min, max);

                return new Vector3(x, 0, z);
            }
        }

        public class DelayedDowngrade
        {
            public int numPlots;
            public int duration;
            public bool removeAllWalls;

            public DelayedDowngrade(int numPlots, int duration, bool removeAllWalls)
            {
                this.numPlots = numPlots;
                this.duration = duration;
                this.removeAllWalls = removeAllWalls;
            }
        }
    }
}