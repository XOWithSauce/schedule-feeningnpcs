using MelonLoader;
using System.Collections;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Economy;
using UnityEngine;
using ScheduleOne.Persistence;
using MelonLoader.Utils;
using ScheduleOne.VoiceOver;
using HarmonyLib;
using Newtonsoft.Json;
using ScheduleOne.DevUtilities;
using ScheduleOne.Money;
using ScheduleOne.ItemFramework;
using ScheduleOne.UI.Handover;
using ScheduleOne.Quests;
using ScheduleOne.UI;
using ScheduleOne.Levelling;

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
        public const string Version = "1.3.0";
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
        public bool ignoreExistingDeals = false; // when false, Customer that has deal cant become a feen
        public bool randomBonus = true; // when true, customers can give you extra cash upon deal completion
        public float randomBonusChance = 0.3f; // Percentage: 1.0 = 100% chance to give out bonus, 0.0 = 0% chance, never gives bonus
        public bool persistEconomy = true; // when true, save file will persist the customer spending habits
    }

    [System.Serializable]
    public class CustomerEconomy
    {
        public string CustomerID = "defaultFeen";
        public float MinWeeklySpend = 200f;
        public float MaxWeeklySpend = 500f;
        public int MinOrdersPerWeek = 1;
        public int MaxOrdersPerWeek = 5;
    }

    [System.Serializable]
    public class SaveEconomy
    {
        public List<CustomerEconomy> customers = new List<CustomerEconomy>();
    }


    public static class ConfigLoader
    {
        private static string path = Path.Combine(MelonEnvironment.ModsDirectory, "FeeningNPCs", "config.json");
        private static string pathEcon = Path.Combine(MelonEnvironment.ModsDirectory, "FeeningNPCs", "Economy");
        #region Mod configurations
        public static ModConfig Load()
        {
            ModConfig config;
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    config = JsonConvert.DeserializeObject<ModConfig>(json);
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
                string json = JsonConvert.SerializeObject(config);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save FeeningNPCs config: " + ex);
            }
        }
        #endregion
        #region Persistent customer economy
        public static string SanitizeAndFormatName(string orgName)
        {
            string econFileName = orgName;

            if (econFileName != null)
            {
                econFileName = econFileName.Replace(" ", "_").ToLower();
                econFileName = econFileName.Replace(",", "");
                econFileName = econFileName.Replace(".", "");
                econFileName = econFileName.Replace("<", "");
                econFileName = econFileName.Replace(">", "");
                econFileName = econFileName.Replace(":", "");
                econFileName = econFileName.Replace("\"", "");
                econFileName = econFileName.Replace("/", "");
                econFileName = econFileName.Replace("\\", "");
                econFileName = econFileName.Replace("|", "");
                econFileName = econFileName.Replace("?", "");
                econFileName = econFileName.Replace("*", "");
            }
            econFileName = econFileName + ".json";
            return econFileName;
        }

        public static SaveEconomy LoadEconomy(Customer[] cmers)
        {
            SaveEconomy config = new SaveEconomy();
            config.customers = new List<CustomerEconomy>();
            string orgName = LoadManager.Instance.ActiveSaveInfo?.OrganisationName;
            string econFileName = SanitizeAndFormatName(orgName);
            if (File.Exists(Path.Combine(pathEcon, econFileName)))
            {
                try
                {
                    string json = File.ReadAllText(Path.Combine(pathEcon, econFileName));
                    config = JsonConvert.DeserializeObject<SaveEconomy>(json);
                    foreach (Customer c in cmers)
                    {
                        CustomerEconomy match = config.customers.FirstOrDefault(econC => econC.CustomerID == c.NPC.ID);
                        c.CustomerData.MinWeeklySpend = match.MinWeeklySpend;
                        c.CustomerData.MaxWeeklySpend = match.MaxWeeklySpend;
                        c.CustomerData.MinOrdersPerWeek = match.MinOrdersPerWeek;
                        c.CustomerData.MaxOrdersPerWeek = match.MaxOrdersPerWeek;
                    }
                }
                catch (Exception ex)
                {
                    config = new SaveEconomy();
                    MelonLogger.Warning("Failed to read SaveEconomy config: " + ex);
                }
            }
            else
            {
                //MelonLogger.Msg("Economy file does not exist at Mods/FeeningNPCs/Economy, generating economy .json file...");
                config = GenerateEconomyState(cmers);
                Save(cmers, config);
            }
            return config;
        }

        public static void Save(Customer[] cmers, SaveEconomy config)
        {
            try
            {
                config = ApplyEconomyState(cmers, config);
                string orgName = LoadManager.Instance.ActiveSaveInfo?.OrganisationName;
                string econFileName = SanitizeAndFormatName(orgName);
                string saveDestination = Path.Combine(pathEcon, econFileName);
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                Directory.CreateDirectory(Path.GetDirectoryName(saveDestination));
                File.WriteAllText(saveDestination, json);
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("Failed to save SaveEconomy config: " + ex);
            }
        }
        public static SaveEconomy GenerateEconomyState(Customer[] cmers)
        {
            SaveEconomy econ = new SaveEconomy();
            foreach (Customer c in cmers)
            {
                CustomerEconomy cEcon = new CustomerEconomy();
                cEcon.CustomerID = c.NPC.ID;
                cEcon.MinWeeklySpend = c.CustomerData.MinWeeklySpend;
                cEcon.MaxWeeklySpend = c.CustomerData.MaxWeeklySpend;
                cEcon.MinOrdersPerWeek = c.CustomerData.MinOrdersPerWeek;
                cEcon.MaxOrdersPerWeek = c.CustomerData.MaxOrdersPerWeek;
                econ.customers.Add(cEcon);
            }
            return econ;
        }

        public static SaveEconomy ApplyEconomyState(Customer[] cmers, SaveEconomy econ)
        {
            foreach (Customer c in cmers)
            {
                CustomerEconomy match = econ.customers.FirstOrDefault(econC => econC.CustomerID == c.NPC.ID);
                match.MinWeeklySpend = c.CustomerData.MinWeeklySpend;
                match.MaxWeeklySpend = c.CustomerData.MaxWeeklySpend;
                match.MinOrdersPerWeek = c.CustomerData.MinOrdersPerWeek;
                match.MaxOrdersPerWeek = c.CustomerData.MaxOrdersPerWeek;
            }
            return econ;
        }
        #endregion
    }
    public class FeeningNPCs : MelonMod
    {
        public static FeeningNPCs Instance { get; set; }
        Customer[] cmers;
        public static List<object> coros = new();
        public static HashSet<Customer> feens = new();
        public static bool registered = false;
        public static bool lastSaveLoad = false;
        public static bool firstTimeLoad = false;
        public static ModConfig currentConfig;
        public static SaveEconomy currentEconomy;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("Your customers need more... and more.... AND MORE!!! HELP ME BRO YOU GOT SOME MORE??!!");
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == 1)
            {
                if (LoadManager.Instance != null && !registered && !lastSaveLoad && !firstTimeLoad)
                {
                    firstTimeLoad = true;
                    LoadManager.Instance.onLoadComplete.AddListener((UnityEngine.Events.UnityAction)OnLoadCompleteCb);
                }
            }
        }

        private void OnLoadCompleteCb()
        {
            if (registered) return;
            registered = true;
            currentConfig = ConfigLoader.Load();
            cmers = UnityEngine.Object.FindObjectsOfType<Customer>(true);
            if (currentConfig.persistEconomy)
                currentEconomy = ConfigLoader.LoadEconomy(cmers);
            coros.Add(MelonCoroutines.Start(this.ChangeBehv()));
            coros.Add(MelonCoroutines.Start(this.ClearFeens()));
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

        #region Harmony Patches for exiting coros
        static void ExitPreTask()
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
        }

        [HarmonyLib.HarmonyPatch(typeof(LoadManager), "ExitToMenu")]
        public static class LoadManager_ExitToMenu_Patch
        {
            public static bool Prefix(SaveInfo autoLoadSave = null, ScheduleOne.UI.MainMenu.MainMenuPopup.Data mainMenuPopup = null, bool preventLeaveLobby = false)
            {
                //MelonLogger.Msg("Exit Menu");
                if (currentConfig.persistEconomy)
                    ConfigLoader.Save(FeeningNPCs.Instance.cmers, currentEconomy);
                lastSaveLoad = false;
                ExitPreTask();
                return true;
            }
        }

        [HarmonyLib.HarmonyPatch(typeof(DeathScreen), "LoadSaveClicked")]
        public static class DeathScreen_LoadSaveClicked_Patch
        {
            public static bool Prefix(DeathScreen __instance)
            {
                //MelonLogger.Msg("LoadLastSave");
                lastSaveLoad = true;
                ExitPreTask();
                return true;
            }
        }
        #endregion

        #region Harmony Patch Process Handover
        [HarmonyLib.HarmonyPatch(typeof(Customer), "ProcessHandover")]
        public static class Customer_ProcessHandover_Patch
        {
            public static bool Prefix(Customer __instance, HandoverScreen.EHandoverOutcome outcome, Contract contract, List<ItemInstance> items, bool handoverByPlayer, bool giveBonuses = true)
            {
                //MelonLogger.Msg("ProcessHandover Customer Postfix");
                coros.Add(MelonCoroutines.Start(PreProcessHandover(__instance, handoverByPlayer)));
                coros.Add(MelonCoroutines.Start(PreProcessBonuses(__instance, outcome, contract, handoverByPlayer)));
                return true;
            }
        }

        public static IEnumerator PreProcessBonuses(Customer __instance, HandoverScreen.EHandoverOutcome outcome, Contract contract, bool handoverByPlayer)
        {
            if (!currentConfig.randomBonus) yield break;
            if (UnityEngine.Random.Range(0.0f, 1.0f) > currentConfig.randomBonusChance) yield break;

            if (outcome == HandoverScreen.EHandoverOutcome.Finalize && handoverByPlayer)
            {
                yield return new WaitForSeconds(1f);
                if (!registered) yield break;

                float rel = Mathf.Clamp(__instance.NPC.RelationData.RelationDelta, 1.0f, 5f);
                int times = (int)rel;
                float ratio;
                // Higher relation delta gives higher chance of better ratio
                switch (times)
                {
                    case 1:
                        ratio = UnityEngine.Random.Range(0.05f, 0.1f);
                        break;

                    case 2:
                        ratio = UnityEngine.Random.Range(0.05f, 0.13f);
                        break;

                    case 3:
                        ratio = UnityEngine.Random.Range(0.1f, 0.15f);
                        break;

                    case 4:
                        ratio = UnityEngine.Random.Range(0.1f, 0.20f);
                        break;

                    case 5:
                        ratio = UnityEngine.Random.Range(0.1f, 0.3f);
                        break;

                    default:
                        ratio = 1f;
                        break;
                }

                float basePay = contract.Payment * ratio;
                float perReward = Mathf.Round(basePay / times);
                for (int i = 0; i <= times; i++)
                {
                    yield return new WaitForSeconds(UnityEngine.Random.Range(0.6f, 0.8f));
                    if (!registered) yield break;
                    NetworkSingleton<MoneyManager>.Instance.ChangeCashBalance(perReward, true, true);
                }
                NetworkSingleton<LevelManager>.Instance.AddXP(times);
                //MelonLogger.Msg($"--> +{times}XP!");
                //MelonLogger.Msg($"Awarded Cash Bonus: {perReward * times}$");
            }
            yield return null;
        }

        public static IEnumerator PreProcessHandover(Customer __instance, bool handoverByPlayer)
        {
            if (!currentConfig.increaseEconomy) yield break;

            // Validate economy multiplier
            int currMult = 1;
            if (currentConfig.economyMultiplier >= 1 && currentConfig.economyMultiplier <= 10)
                currMult = currentConfig.economyMultiplier;

            // If handover by player award extra xp based on config
            if (handoverByPlayer)
            {
                int xpReward = 1 + UnityEngine.Random.Range(1, 3 + currMult); 
                NetworkSingleton<LevelManager>.Instance.AddXP(xpReward);
                //MelonLogger.Msg($"--> +{xpReward}XP!");
            }
            else
            {
                if (UnityEngine.Random.Range(0f, 1f) < 0.8f)
                {
                    int xpReward = 2;
                    NetworkSingleton<LevelManager>.Instance.AddXP(xpReward);
                    //MelonLogger.Msg($"--> +{xpReward}XP!");
                }
            }

            // Player Average Economy
            float lastNetWNorm = Mathf.Clamp01(MoneyManager.Instance.LastCalculatedNetworth / 5000000f);
            float lifetimeNorm = Mathf.Clamp01(MoneyManager.Instance.LifetimeEarnings / 5000000f);
            float cashBNorm = Mathf.Clamp01(MoneyManager.Instance.cashBalance / 5000000f);
            float economyTotal = (lastNetWNorm + lifetimeNorm + cashBNorm) / 3f;
            economyTotal = Mathf.Max(economyTotal, 0.01f);

            float boostRateMin;
            float boostRateMax;

            if (economyTotal == 0.01f)
            {
                boostRateMin = 0.6f;
                boostRateMax = 0.9f;
            }
            else if (economyTotal < 0.03)
            {
                boostRateMin = 0.5f;
                boostRateMax = 0.8f;
            }
            else if (economyTotal < 0.06)
            {
                boostRateMin = 0.3f;
                boostRateMax = 0.5f;
            }
            else if (economyTotal < 0.1f)
            {
                boostRateMin = 0.15f;
                boostRateMax = 0.2f;
            }
            else if (economyTotal < 0.14f)
            {
                boostRateMin = 0.06f;
                boostRateMax = 0.1f;
            }
            else if (economyTotal < 0.2f)
            {
                boostRateMin = 0.02f;
                boostRateMax = 0.04f;
            }
            else
            {
                boostRateMin = 0.02f;
                boostRateMax = 0.03f;
            }

            // Customer Economy
            float weekSpend = __instance.CustomerData.MaxWeeklySpend;
            float minWeekSpend = __instance.CustomerData.MinWeeklySpend;

            float instSpendNorm = Mathf.Clamp01(weekSpend / 5000000f);
            if (economyTotal <= instSpendNorm) yield break;

            float diff = economyTotal - instSpendNorm;

            float target = weekSpend + ((weekSpend * currMult) * diff);
            if (target >= 500000f * currMult)
                target = 500000f * currMult;
            float targetMin = minWeekSpend + ((minWeekSpend * currMult) * diff);
            if (targetMin >= 166666f * currMult)
                targetMin = 166666f * currMult;

            // Customer internal variables
            float customerAdct = __instance.CurrentAddiction;
            float customerDeps = Mathf.Clamp01((0.01f+__instance.CustomerData.DependenceMultiplier) / 2);
            float customerRela = __instance.NPC.RelationData.NormalizedRelationDelta;
            float internalTotal = Mathf.Clamp01((customerAdct + customerDeps + customerRela) / 3.5f);

            // Adjust max spend cap
            float adjustmentFactorMax = diff * (currMult * boostRateMax);
            float res = Mathf.Lerp(
                    weekSpend,
                    target,
                    adjustmentFactorMax
            );
            float resultMax = Mathf.Clamp((res - weekSpend),
                min: UnityEngine.Random.Range(1f, Mathf.Round(5f * (1f + customerDeps))),
                max: UnityEngine.Random.Range(30f, Mathf.Round(60f * (1f + customerDeps)))
            );
            float maxSpendNew = Mathf.Lerp(
                weekSpend + resultMax,
                res,
                internalTotal
            );
            res = Mathf.Round(maxSpendNew);
            if (res < 80000f)
                __instance.CustomerData.MaxWeeklySpend = res;

            // Adjust min spend cap
            float adjustmentFactorMin = diff * (currMult * boostRateMin);
            float resM = Mathf.Lerp(
                    minWeekSpend,
                    targetMin,
                    adjustmentFactorMin
            );
            float resultMinMax = Mathf.Clamp((resM - minWeekSpend),
                min: UnityEngine.Random.Range(1f, Mathf.Round(3f * (1f + customerDeps))),
                max: UnityEngine.Random.Range(20f, Mathf.Round(40f * (1f + customerDeps)))
            );
            float minSpendNew = Mathf.Lerp(
                minWeekSpend + resultMinMax,
                resM,
                internalTotal
            );
            resM = Mathf.Round(minSpendNew);
            if (resM < 20000f && resM < maxSpendNew)
                __instance.CustomerData.MinWeeklySpend = resM;

            //MelonLogger.Msg($"----\nDependence: {__instance.CustomerData.DependenceMultiplier}\nAddiction: {__instance.CurrentAddiction}\nRelation: {__instance.NPC.RelationData.NormalizedRelationDelta}\nTotal: {internalTotal}\n\nEconomy Diff: {diff}\nBoostRates:{boostRateMin}/{boostRateMax}\nTargets:{resM}/{res}\n----");
            //MelonLogger.Msg($"-------\n*MinSpend +{resM-minWeekSpend}$\n*MaxSpend +{res-weekSpend}$\n-------");
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
                case 0: // Increase MinOrdersPerWeek by 1
                    {
                        if (UnityEngine.Random.Range(0f, 1f) > 0.5f) break;
                        int baseMax = 4 * currMult;
                        int current = c.CustomerData.MinOrdersPerWeek;
                        int max = c.CustomerData.MaxOrdersPerWeek;
                        int result = current + 1;
                        if (current >= baseMax || result >= max) break;
                        c.CustomerData.MinOrdersPerWeek = result;
                        //MelonLogger.Msg($"MinOrdersPerWeek: {c.CustomerData.MinOrdersPerWeek} (+{result - current})");
                        break;
                    }

                case 1: // Increase MaxOrdersPerWeek by 1
                    {
                        if (UnityEngine.Random.Range(0f, 1f) > 0.5f) break;
                        int baseMax = 7 * currMult;
                        int current = c.CustomerData.MaxOrdersPerWeek;
                        int result = current + 1;
                        if (current >= baseMax || result >= baseMax) break;
                        c.CustomerData.MaxOrdersPerWeek = result;
                        //MelonLogger.Msg($"MaxOrdersPerWeek: {c.CustomerData.MaxOrdersPerWeek} (+{result - current})");
                        break;
                    }

                // Spending habits
                case 2: // Increase MaxWeeklySpend by 1-10%
                    {
                        float baseMax = 8000f * currMult;
                        float current = c.CustomerData.MaxWeeklySpend;
                        float result = c.CustomerData.MaxWeeklySpend * (1.00f + (0.01f * currMult));
                        if (current >= baseMax) break;
                        c.CustomerData.MaxWeeklySpend = Mathf.Round(result);
                        //MelonLogger.Msg($"MaxWeeklySpend: {c.CustomerData.MaxWeeklySpend} (+{result - current})");
                        break;
                    }

                case 3: // Increase MinWeeklySpend by 1-10%
                    {
                        float baseMax = 2000f * currMult;
                        float current = c.CustomerData.MinWeeklySpend;
                        float max = c.CustomerData.MaxWeeklySpend;
                        float result = c.CustomerData.MinWeeklySpend * (1.00f + (0.01f * currMult));
                        if (current >= baseMax || result >= max) break;
                        c.CustomerData.MinWeeklySpend = Mathf.Round(result);
                        //MelonLogger.Msg($"MinWeeklySpend: {c.CustomerData.MinWeeklySpend} (+{result - current})");
                        break;
                    }
            }
            yield return null;
        }
        public static IEnumerator RandomFeenEffect(Customer c)
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
                c.NPC.Behaviour.CombatBehaviour.SetTarget(null, closest.NetworkObject);
                c.NPC.Behaviour.CombatBehaviour.SendEnable();
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
            yield return new WaitForSeconds(5f);

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

                        // avoid null reference if something happens to object, e.g. next call will be transform if object is deinstantiated its null
                        if (c == null || c?.NPC == null || c.NPC?.transform == null) continue;

                        if (Vector3.Distance(c.NPC.transform.position, player.transform.position) > currentConfig.feeningRadius)
                            continue;

                        string currentBehavior = "";
                        if (c.NPC.Behaviour.activeBehaviour != null)
                            currentBehavior = c.NPC.Behaviour.activeBehaviour.ToString();

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
                yield return new WaitForSeconds(0.3f);
                if (!registered) yield break;

                if (c.NPC.isInBuilding)
                    c.NPC.ExitBuilding();
                yield return new WaitForSeconds(0.5f);
                if (!registered) yield break;
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
