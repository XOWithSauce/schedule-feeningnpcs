using MelonLoader;
using System.Collections;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Economy;
using UnityEngine;
using ScheduleOne.Persistence;
using MelonLoader.Utils;
using ScheduleOne.VoiceOver;
using ScheduleOne.AvatarFramework.Equipping;

[assembly: MelonInfo(typeof(FeeningNPCs.FeeningNPCs), FeeningNPCs.BuildInfo.Name, FeeningNPCs.BuildInfo.Version, FeeningNPCs.BuildInfo.Author, FeeningNPCs.BuildInfo.DownloadLink)]
[assembly: MelonColor()]
[assembly: MelonOptionalDependencies("FishNet.Runtime")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace FeeningNPCs
{
    public static class BuildInfo
    {
        public const string Name = "FeeningNPCs";
        public const string Description = "Your customers need more... and more.... AND MORE!!! HELP ME BRO YOU GOT SOME MORE??!!";
        public const string Author = "XOWithSauce";
        public const string Company = null;
        public const string Version = "1.2.2";
        public const string DownloadLink = null;
    }


    [System.Serializable]
    public class ModConfig
    {
        public int maxFeensStack = 3; // How many feens can be selected at once to request
        public int feeningTreshold = 50; // minimum time app has to wait for new feens event
        public int feensClearThreshold = 1440; // how often recent feens are cleared
        public int feeningRadius = 30; // max distance away from player
        public bool randomEffects = true; // when true rolls miscellanious actions
        public bool increaseEconomy = true; // when true adds more spending habits
    }


    public static class ConfigLoader
    {
        private static string path = Path.Combine(MelonEnvironment.ModsDirectory, "FeeningNPCs", "config.json");
        public static ModConfig Load()
        {
            ModConfig config;
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    config = JsonUtility.FromJson<ModConfig>(json);
                }
                catch (Exception ex)
                {
                    config = new ModConfig();
                    MelonLogger.Warning("Failed to read FeeningNPCs config: " + ex);
                }
            }
            else
            {
                config = new ModConfig();
                Save(config);
            }
            return config;
        }

        public static void Save(ModConfig config)
        {
            try
            {
                string json = JsonUtility.ToJson(config, true);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save FeeningNPCs config: " + ex);
            }

        }
    }

    public class FeeningNPCs : MelonMod
    {
        Customer[] cmers;
        public static List<object> coros = new();
        HashSet<Customer> feens = new();
        public static bool registered = false;
        private ModConfig currentConfig;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("Your customers need more... and more.... AND MORE!!! HELP ME BRO YOU GOT SOME MORE??!!");
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == 1)
            {
                if (LoadManager.Instance != null && !registered)
                {
                    LoadManager.Instance.onLoadComplete.AddListener(OnLoadCompleteCb);
                }
            }
            else
            {
                if (LoadManager.Instance != null && registered)
                {
                    LoadManager.Instance.onLoadComplete.RemoveListener(OnLoadCompleteCb);
                }
                registered = false;

                foreach (object coro in coros)
                {
                    MelonCoroutines.Stop(coro);
                }
                coros.Clear();
                feens.Clear();
            }
        }

        private void OnLoadCompleteCb()
        {
            if (registered) return;
            currentConfig = ConfigLoader.Load();
            this.cmers = UnityEngine.Object.FindObjectsOfType<Customer>(true);
            coros.Add(MelonCoroutines.Start(this.ChangeBehv()));
            coros.Add(MelonCoroutines.Start(this.ClearFeens()));
            registered = true;
        }

        private IEnumerator ClearFeens()
        {
            yield return new WaitForSeconds(1f);
            for (; ; )
            {
                yield return new WaitForSeconds(currentConfig.feensClearThreshold);
                if (!registered) yield break;
                //MelonLogger.Msg("Clear Feens");
                feens.Clear();
            }
        }

        #region Random effect coros
        public static IEnumerator ChangeCustomerEconomy(Customer c)
        {
            int roll = UnityEngine.Random.Range(0, 4);
            switch (roll)
            {
                // Orders count
                case 0: // Increase MinOrdersPerWeek by 1
                    {
                        int baseMax = 12;
                        int current = c.CustomerData.MinOrdersPerWeek;
                        if (c.CustomerData.MinOrdersPerWeek >= baseMax) break;
                        c.CustomerData.MinOrdersPerWeek = Mathf.Max(baseMax, current + 1);
                        //MelonLogger.Msg("MinOrdersPerWeek: " + c.CustomerData.MinOrdersPerWeek);
                        break;
                    }

                case 1: // Increase MaxOrdersPerWeek by 1
                    {
                        int baseMax = 24;
                        int current = c.CustomerData.MaxOrdersPerWeek;
                        if (c.CustomerData.MaxOrdersPerWeek >= baseMax) break;
                        c.CustomerData.MaxOrdersPerWeek = Mathf.Max(baseMax, current + 1);
                        //MelonLogger.Msg("MaxOrdersPerWeek: " + c.CustomerData.MaxOrdersPerWeek);
                        break;
                    }

                // Spending habits
                case 2: // Increase MaxWeeklySpend by 1% - cap 30k
                    {
                        float baseMax = 30000f;
                        if (c.CustomerData.MaxWeeklySpend >= baseMax) break;
                        c.CustomerData.MaxWeeklySpend = Mathf.Max(baseMax, c.CustomerData.MaxWeeklySpend * 1.01f);
                        //MelonLogger.Msg("MaxWeeklySpend: " + c.CustomerData.MaxWeeklySpend);
                        break;
                    }


                case 3: // Increase MinWeeklySpend by 1% - cap 10k
                    {
                        float baseMax = 10000f;
                        if (c.CustomerData.MinWeeklySpend >= baseMax) break;
                        c.CustomerData.MinWeeklySpend = Mathf.Max(baseMax, c.CustomerData.MinWeeklySpend * 1.01f);
                        //MelonLogger.Msg("MinWeeklySpend: " + c.CustomerData.MinWeeklySpend);
                        break;
                    }


            }
            yield return null;
        }
        public static IEnumerator RandomFeenEffect(Customer c)
        {
            int roll = UnityEngine.Random.Range(0, 14);
            switch (roll)
            {
                case 0:
                    //MelonLogger.Msg("Aggr");
                    c.NPC.OverrideAggression(1f);
                    break;

                case 1:
                    c.CustomerData.CallPoliceChance = 1f;
                    //MelonLogger.Msg("CallPoliceChance:" + c.CustomerData.CallPoliceChance);
                    break;

                case 2:
                    c.CustomerData.CallPoliceChance = 0.5f;
                    //MelonLogger.Msg("CallPoliceChance:" + c.CustomerData.CallPoliceChance);
                    break;

                case 3:
                    c.NPC.PlayVO(EVOLineType.VeryHurt);
                    c.NPC.Avatar.EmotionManager.AddEmotionOverride("Annoyed", "product_request_fail", 30f, 1);
                    //MelonLogger.Msg("Emotion");
                    break;

                case 4:
                    c.NPC.OverrideAggression(0.5f);
                    //MelonLogger.Msg("OverrideAggr:" + c.NPC.Aggression);
                    break;

                case 5:
                    //MelonLogger.Msg("SicklySkin");
                    c.NPC.Avatar.Effects.SetSicklySkinColor(true);
                    break;

                case 6:
                    coros.Add(MelonCoroutines.Start(NPCAttackEffect(c)));
                    break;

                case 7:
                    coros.Add(MelonCoroutines.Start(RagdollEffect(c)));
                    break;
            }

            yield return null;
        }

        private static IEnumerator NPCAttackEffect(Customer c)
        {
            //MelonLogger.Msg("NPC attack effect");
            yield return new WaitForSeconds(1f);
            if (!registered || c.NPC.RelationData.RelationDelta > 3.8f) yield break;

            Player.GetClosestPlayer(c.transform.position, out float num1);
            int maxIter = 20;
            int i = 0;
            while (num1 > 6f)
            {
                if (i == maxIter) break;
                yield return new WaitForSeconds(0.5f);
                if (!registered) yield break;

                Player.GetClosestPlayer(c.transform.position, out num1);
                i++;
            }
            yield return new WaitForSeconds(0.5f);
            if (!registered) yield break;

            Player closest = Player.GetClosestPlayer(c.transform.position, out float dist, null);
            if (dist < 6f && closest != null && closest.NetworkObject != null)
            {
                c.NPC.behaviour.CombatBehaviour.SetTarget(null, closest.NetworkObject);
                c.NPC.behaviour.CombatBehaviour.SendEnable();
            }
        }
        private static IEnumerator RagdollEffect(Customer c)
        {
            //MelonLogger.Msg("Ragdoll");
            yield return new WaitForSeconds(1f);
            if (!registered) yield break;

            Player.GetClosestPlayer(c.transform.position, out float num3);
            int maxIter2 = 20;
            int j = 0;
            while (num3 > 6f)
            {
                if (j == maxIter2) break;
                yield return new WaitForSeconds(0.5f);
                if (!registered) yield break;

                Player.GetClosestPlayer(c.transform.position, out num3);
                j++;
            }
            yield return new WaitForSeconds(0.5f);
            if (!registered) yield break;

            Player.GetClosestPlayer(c.transform.position, out float dist, null);
            if (dist < 6f)
                c.NPC.Movement.ActivateRagdoll(c.NPC.transform.position, Vector3.forward, 6f);
        }
        #endregion
        private IEnumerator ChangeBehv()
        {
            yield return new WaitForSeconds(1f);

            for (; ; )
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(currentConfig.feeningTreshold, currentConfig.feeningTreshold * 2));
                if (!registered) yield break;
                try
                {
                    //MelonLogger.Msg("Evaluate Nearby Feens");
                    Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);
                    if (players.Length == 0) { continue; }
                    Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];
                    List<Customer> nearbyCustomers = new();

                    foreach (Customer c in cmers)
                    {
                        float dist = Vector3.Distance(c.NPC.transform.position, randomPlayer.transform.position);
                        string currentBehavior = "";
                        if (c.NPC.behaviour.activeBehaviour != null)
                            currentBehavior = c.NPC.behaviour.activeBehaviour.ToString();

                        if (dist < currentConfig.feeningRadius && !currentBehavior.Contains("Request product from player") && !feens.Contains(c) && c.NPC.RelationData.Unlocked && !c.NPC.Health.IsKnockedOut && !c.NPC.Health.IsDead)
                        {
                            nearbyCustomers.Add(c);
                            if (nearbyCustomers.Count >= currentConfig.maxFeensStack)
                                break;
                        }
                    }

                    if (nearbyCustomers.Count > 0)
                    {
                        //MelonLogger.Msg($"Nearby Feening: {nearbyCustomers.Count}");
                        coros.Add(MelonCoroutines.Start(this.SetFeening(nearbyCustomers, randomPlayer)));
                    }

                }
                catch (Exception ex)
                {
                    MelonLogger.Error("Feening NPCs caught an error: " + ex);
                }
            }
        }
        private IEnumerator SetFeening(List<Customer> cs, Player randomPlayer)
        {
            foreach (Customer c in cs)
            {
                yield return new WaitForSeconds(2f);
                if (!registered) yield break;

                if (c.NPC.isInBuilding)
                    c.NPC.ExitBuilding();
                yield return new WaitForSeconds(0.5f);
                if (!c.NPC.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: currentConfig.feeningRadius))
                    continue;

                if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                    if (currentConfig.randomEffects)
                        coros.Add(MelonCoroutines.Start(RandomFeenEffect(c)));
                    else
                    if (currentConfig.increaseEconomy)
                        coros.Add(MelonCoroutines.Start(ChangeCustomerEconomy(c)));

                c.RequestProduct(randomPlayer);
                feens.Add(c);
                //MelonLogger.Msg("NearbyFeeningDone");
            }

            yield return null;
        }

    }
}

