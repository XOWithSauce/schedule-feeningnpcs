using MelonLoader;
using System.Collections;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Economy;
using UnityEngine;
using ScheduleOne.Persistence;
using MelonLoader.Utils;
using ScheduleOne.VoiceOver;
using ScheduleOne.AvatarFramework.Equipping;
using ScheduleOne.Tools;
using HarmonyLib;
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
using ScheduleOne.ItemFramework;
using ScheduleOne.UI.Handover;
using ScheduleOne.Quests;

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
        public const string Version = "1.2.3";
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
        public int economyMultiplier = 10; // 1 - 10 multiplier (1 small economy growth, 10 max economy growth)
        public bool ignoreExistingDeals = true; // when false, Customer that has deal cant become a feenns
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
        public static Customer[] cmers;
        public static List<object> coros = new();
        public static HashSet<Customer> feens = new();
        public static bool registered = false;
        public static ModConfig currentConfig;

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
                LoadManager.Instance.onLoadComplete.RemoveListener(OnLoadCompleteCb);
        }

        private void OnLoadCompleteCb()
        {
            if (registered) return;
            currentConfig = ConfigLoader.Load();
            cmers = UnityEngine.Object.FindObjectsOfType<Customer>(true);
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

        #region Harmony Patch Exit to Menu to break coros
        [HarmonyPatch(typeof(ExitToMenu), "Exit")]
        public static class Tools_ExitToMenu_Patch
        {
            public static bool Prefix(ExitToMenu __instance)
            {
                //MelonLogger.Msg("Pre-Exit Task");
                registered = false;
                foreach (object coro in coros)
                {
                    if (coro != null)
                        MelonCoroutines.Stop(coro);
                }
                coros.Clear();
                feens.Clear();
                return true;
            }
        }
        #endregion

        #region Harmony Patch Process Handover
        [HarmonyPatch(typeof(Customer), "ProcessHandover")]
        public static class Customer_ProcessHandover_Patch
        {
            public static bool Prefix(Customer __instance, HandoverScreen.EHandoverOutcome outcome, Contract contract, List<ItemInstance> items, bool handoverByPlayer, bool giveBonuses = true)
            {
                //MelonLogger.Msg("ProcessHandover Customer Postfix");
                coros.Add(MelonCoroutines.Start(PreProcessHandover(__instance, handoverByPlayer)));
                return true;
            }
        }

        public static IEnumerator PreProcessHandover(Customer __instance, bool handoverByPlayer)
        {
            if (!currentConfig.increaseEconomy) yield break;

            // Player Average Economy
            float lastNetWNorm = Mathf.Clamp01(MoneyManager.Instance.LastCalculatedNetworth / 10000000f);
            float lifetimeNorm = Mathf.Clamp01(MoneyManager.Instance.LifetimeEarnings / 10000000f);
            float cashBNorm = Mathf.Clamp01(MoneyManager.Instance.cashBalance / 10000000f);
            float economyTotal = (lastNetWNorm + lifetimeNorm + cashBNorm) / 3f;
            economyTotal = Mathf.Max(economyTotal, 0.01f);

            // Validate economy multiplier
            int currMult = 1;
            if (currentConfig.economyMultiplier >= 1 && currentConfig.economyMultiplier <= 10)
                currMult = currentConfig.economyMultiplier;

            // Customer Economy
            float weekSpend = __instance.CustomerData.MaxWeeklySpend;
            float minWeekSpend = __instance.CustomerData.MinWeeklySpend;

            float instSpendNorm = Mathf.Clamp01(weekSpend / 10000000f);
            if (economyTotal <= instSpendNorm) yield break;

            float diff = economyTotal - instSpendNorm; // max ~0.999 if at 10mil on all 

            float target = weekSpend + ((weekSpend * currMult) * diff); // max = weekSpend * mult * 1.9999
            if (target >= 500000f * currMult)
                target = 500000f * currMult; // Cap at 5 mil at full mult

            float targetMin = minWeekSpend + ((minWeekSpend * currMult) * diff); // max = minWeekSpend * mult * 1.9999
            if (targetMin >= 166666f * currMult)
                targetMin = 166666f * currMult; // Cap at 1.666mil at full mult

            float adjustmentFactorMax = diff * (currMult * 0.1f);
            float resultMax = Mathf.Lerp(
                weekSpend,
                target,
                adjustmentFactorMax
            );
            resultMax = Mathf.Round(resultMax);
            if (resultMax <= 5000000f)
                __instance.CustomerData.MaxWeeklySpend = resultMax;

            float adjustmentFactorMin = diff * (currMult * 0.03f);
            float resultMin = Mathf.Lerp(
                minWeekSpend,
                targetMin,
                adjustmentFactorMin
            );
            resultMin = Mathf.Round(resultMin);
            if (resultMin <= 1666666f)
                __instance.CustomerData.MinWeeklySpend = resultMin;

            //MelonLogger.Msg($"EconomyTotal: {economyTotal}");
            //MelonLogger.Msg($"MaxSpend ( {weekSpend} -> {__instance.CustomerData.MaxWeeklySpend} ) +{__instance.CustomerData.MaxWeeklySpend-weekSpend}");
            //MelonLogger.Msg($"MinSpend ( {minWeekSpend} -> {__instance.CustomerData.MinWeeklySpend} ) +{__instance.CustomerData.MinWeeklySpend - minWeekSpend}");
            //MelonLogger.Msg($"Adjustment Factor: {adjustmentFactorMin}/{adjustmentFactorMax}");

            yield return null;
        }


        #endregion

        #region Random effect coros
        private IEnumerator ChangeCustomerEconomy(Customer c)
        {
            int roll = UnityEngine.Random.Range(0, 4);
            // Validate economy multiplier
            int currMult = 1;
            if (currentConfig.economyMultiplier >= 1 && currentConfig.economyMultiplier <= 10)
                currMult = currentConfig.economyMultiplier;

            switch (roll)
            {
                // Orders count
                case 0: // Increase MinOrdersPerWeek by 1-3
                    {
                        int increment = Mathf.Clamp(Mathf.CeilToInt(currMult / 4f), 1, 3);
                        int baseMax = 4 * currMult;
                        int current = c.CustomerData.MinOrdersPerWeek;
                        int max = c.CustomerData.MaxOrdersPerWeek;
                        int result = current + increment;
                        if (current >= baseMax || result >= max) break;
                        c.CustomerData.MinOrdersPerWeek = Mathf.Max(baseMax, result);
                        //MelonLogger.Msg("MinOrdersPerWeek: " + c.CustomerData.MinOrdersPerWeek);
                        break;
                    }

                case 1: // Increase MaxOrdersPerWeek by 1-3
                    {
                        int increment = Mathf.Clamp(Mathf.CeilToInt(currMult / 4f), 1, 3);
                        int baseMax = 7 * currMult;
                        int current = c.CustomerData.MaxOrdersPerWeek;
                        int result = current + increment;
                        if (current >= baseMax || result >= baseMax) break;
                        c.CustomerData.MaxOrdersPerWeek = Mathf.Max(baseMax, result);
                        //MelonLogger.Msg("MaxOrdersPerWeek: " + c.CustomerData.MaxOrdersPerWeek);
                        break;
                    }

                // Spending habits
                case 2: // Increase MaxWeeklySpend by 1-10%
                    {
                        // because max increment is 10% and dynamics cap at 5mil
                        // we use 450 000 as base and at max multiplier 4 500 000 so that + 10% to that is always 5mil
                        float baseMax = 450000f * currMult;
                        float current = c.CustomerData.MaxWeeklySpend;
                        float result = c.CustomerData.MaxWeeklySpend * (1.00f + (0.01f * currMult));
                        if (current >= baseMax) break;
                        c.CustomerData.MaxWeeklySpend = Mathf.Max(baseMax, result);
                        //MelonLogger.Msg("MaxWeeklySpend: " + c.CustomerData.MaxWeeklySpend);
                        break;
                    }

                case 3: // Increase MinWeeklySpend by 1-10%
                    {
                        // because max increment is 10% and dynamics cap around third of max week cap ~1.666mil
                        // we use 150 000 as base and at max multiplier 1 500 000 so that + 10% to that is always max ~1.666mil
                        float baseMax = 150000f * currMult;
                        float current = c.CustomerData.MinWeeklySpend;
                        float max = c.CustomerData.MaxWeeklySpend;
                        float result = c.CustomerData.MinWeeklySpend * (1.00f + (0.01f * currMult));
                        if (current >= baseMax || result >= max) break;
                        c.CustomerData.MinWeeklySpend = Mathf.Max(baseMax, result);
                        //MelonLogger.Msg("MinWeeklySpend: " + c.CustomerData.MinWeeklySpend);
                        break;
                    }
            }
            yield return null;
        }
        private IEnumerator RandomFeenEffect(Customer c)
        {
            int roll = UnityEngine.Random.Range(0, 8);
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
                    Player player = Player.Local;
                    List<Customer> nearbyCustomers = new();

                    foreach (Customer c in cmers)
                    {
                        if (!currentConfig.ignoreExistingDeals && c.CurrentContract != null)
                            continue;

                        if (Vector3.Distance(c.NPC.transform.position, player.transform.position) > currentConfig.feeningRadius)
                            continue;

                        string currentBehavior = "";
                        if (c.NPC.behaviour.activeBehaviour != null)
                            currentBehavior = c.NPC.behaviour.activeBehaviour.ToString();

                        if (!currentBehavior.Contains("Request product from player") && !feens.Contains(c) && c.NPC.RelationData.Unlocked && !c.NPC.Health.IsKnockedOut && !c.NPC.Health.IsDead && c.NPC.Movement.CanMove())
                        {
                            nearbyCustomers.Add(c);
                            if (nearbyCustomers.Count >= currentConfig.maxFeensStack)
                                break;
                        }
                    }

                    if (nearbyCustomers.Count > 0)
                    {
                        //MelonLogger.Msg($"Nearby Feening: {nearbyCustomers.Count}");
                        coros.Add(MelonCoroutines.Start(this.SetFeening(nearbyCustomers, player)));
                    }

                }
                catch (Exception ex)
                {
                    MelonLogger.Error("Feening NPCs caught an error: " + ex);
                }
            }
        }
        private IEnumerator SetFeening(List<Customer> cs, Player player)
        {
            foreach (Customer c in cs)
            {
                yield return new WaitForSeconds(2f);
                if (!registered) yield break;

                if (c.NPC.isInBuilding)
                    c.NPC.ExitBuilding();
                yield return new WaitForSeconds(0.5f);
                if (!c.NPC.Movement.CanGetTo(player.transform.position, proximityReq: currentConfig.feeningRadius))
                    continue;

                if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
                {
                    if (currentConfig.randomEffects)
                        coros.Add(MelonCoroutines.Start(RandomFeenEffect(c)));
                }
                else
                {
                    if (currentConfig.increaseEconomy)
                        coros.Add(MelonCoroutines.Start(ChangeCustomerEconomy(c)));
                }
                    

                c.RequestProduct(player);
                feens.Add(c);
                //MelonLogger.Msg("NearbyFeeningDone");
            }

            yield return null;
        }

    }
}

