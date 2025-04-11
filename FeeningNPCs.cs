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
        public const string Version = "1.2";
        public const string DownloadLink = null;
    }


    [System.Serializable]
    public class ModConfig
    {
        public int maxFeensStack = 3; // How many feens can be selected at once to request
        public int feeningTreshold = 50; // minimum time app has to wait for new feens event
        public int feensClearThreshold = 1440; // how often recent feens are cleared
        public int feeningRadius = 10; // max distance away from player
        public bool randomEffects = true; // when true rolls miscellanious actions and property changes
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
        List<object> coros = new();
        List<Customer> feens = new();
        private bool registered = false;
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
            for (; ; )
            {
                yield return new WaitForSeconds(currentConfig.feensClearThreshold);
                // MelonLogger.Msg("Clear Feens");
                feens.Clear();
            }
        }
        private IEnumerator RandomFeenEffect(Customer c)
        {
            int roll = UnityEngine.Random.Range(0, 14);
            switch (roll)
            {
                case 0:
                    c.NPC.OverrideAggression(1f);
                    try
                    {
                        GameObject newObj = GameObject.Instantiate(UnityEngine.Object.FindObjectOfType<AvatarMeleeWeapon>().gameObject, c.transform);
                        if (newObj == null) break;

                        if (!newObj.activeInHierarchy)
                            newObj.SetActive(true);

                        c.NPC.behaviour.CombatBehaviour.DefaultWeapon = newObj.GetComponent<AvatarMeleeWeapon>();
                        c.NPC.SetEquippable_Networked(null, c.NPC.behaviour.CombatBehaviour.DefaultWeapon.AssetPath);
                        //MelonLogger.Msg("NPC Wielding: " + c.NPC.behaviour.CombatBehaviour.DefaultWeapon.AssetPath);
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Msg(ex);
                    }
                    break;

                case 1:
                    c.CustomerData.MinOrdersPerWeek = 4;
                    //MelonLogger.Msg("MinOrdersPerWeek:" + c.CustomerData.MinOrdersPerWeek);
                    break;

                case 2:
                    c.CustomerData.MaxWeeklySpend = 10000f;
                    //MelonLogger.Msg("MaxWeeklySpend:" + c.CustomerData.MaxWeeklySpend);
                    break;

                case 3:
                    c.CustomerData.MaxOrdersPerWeek = 30;
                    //MelonLogger.Msg("MaxOrdersPerWeek:" + c.CustomerData.MaxOrdersPerWeek);
                    break;

                case 4:
                    c.CustomerData.CallPoliceChance = 1f;
                    //MelonLogger.Msg("CallPoliceChance:" + c.CustomerData.CallPoliceChance);
                    break;

                case 5:
                    c.CustomerData.MinWeeklySpend = 3000f;
                    //MelonLogger.Msg("MinWeeklySpend:" + c.CustomerData.MinWeeklySpend);
                    break;

                case 6:
                    c.CustomerData.CallPoliceChance = 0.5f;
                    //MelonLogger.Msg("CallPoliceChance:" + c.CustomerData.CallPoliceChance);
                    break;

                case 7:
                    c.NPC.PlayVO(EVOLineType.VeryHurt);
                    c.NPC.Avatar.EmotionManager.AddEmotionOverride("Annoyed", "product_request_fail", 30f, 1);
                    //MelonLogger.Msg("Emotion");
                    break;

                case 8:
                    c.NPC.OverrideAggression(0.5f);
                    //MelonLogger.Msg("OverrideAggr:" + c.NPC.Aggression);
                    break;

                case 9:
                    c.CustomerData.MaxWeeklySpend = 3000f;
                    //MelonLogger.Msg("MaxWeeklySpend:" + c.CustomerData.MaxWeeklySpend);
                    break;

                case 10:
                    c.CustomerData.MinWeeklySpend = 500f;
                    //MelonLogger.Msg("MinWeeklySpend:" + c.CustomerData.MinWeeklySpend);
                    break;

                case 11:
                    c.NPC.Avatar.Effects.SetSicklySkinColor(true);
                    //MelonLogger.Msg("SicklySkin");
                    break;

                case 12:
                    try
                    {
                        GameObject newObj = GameObject.Instantiate(UnityEngine.Object.FindObjectOfType<AvatarMeleeWeapon>().gameObject, c.transform);
                        if (newObj == null) break;

                        if (!newObj.activeInHierarchy)
                            newObj.SetActive(true);

                        c.NPC.behaviour.CombatBehaviour.DefaultWeapon = newObj.GetComponent<AvatarMeleeWeapon>();
                        c.NPC.SetEquippable_Networked(null, c.NPC.behaviour.CombatBehaviour.DefaultWeapon.AssetPath);
                        //MelonLogger.Msg("NPC Wielding: " + c.NPC.behaviour.CombatBehaviour.DefaultWeapon.AssetPath);
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Msg(ex);
                    }
                    yield return new WaitForSeconds(1f);
                    Player.GetClosestPlayer(c.transform.position, out float num1);
                    int maxIter = 20;
                    int i = 0;
                    while (num1 > 10)
                    {
                        if (i == maxIter) break;
                        yield return new WaitForSeconds(1f);
                        Player.GetClosestPlayer(c.transform.position, out num1);
                        i++;
                    }
                    float num2;
                    c.NPC.behaviour.CombatBehaviour.SetTarget(null, Player.GetClosestPlayer(c.transform.position, out num2, null).NetworkObject);
                    
                    c.NPC.behaviour.CombatBehaviour.Enable_Networked(null);
                    break;

                case 13:
                    yield return new WaitForSeconds(1f);
                    Player.GetClosestPlayer(c.transform.position, out float num3);
                    int maxIter2 = 10;
                    int j = 0;
                    while (num3 > 10)
                    {
                        if (j == maxIter2) break;
                        yield return new WaitForSeconds(1f);
                        Player.GetClosestPlayer(c.transform.position, out num1);
                        j++;
                    }
                    c.NPC.Movement.ActivateRagdoll(c.NPC.transform.position, Vector3.forward, 3f);
                    break;
            }

            yield return null;
        }

        private IEnumerator ChangeBehv()
        {
            for (; ; )
            {
                yield return new WaitForSeconds(1f);
                yield return new WaitForSeconds(UnityEngine.Random.Range(currentConfig.feeningTreshold, currentConfig.feeningTreshold*2));
                //MelonLogger.Msg("Evaluate Nearby Feens");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);
                if (players.Length == 0) { continue; }
                Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];
                List<Customer> nearbyCustomers = new();

                foreach (Customer c in cmers)
                {
                    float dist = Vector3.Distance(c.NPC.transform.position, randomPlayer.transform.position);
                    string currentBehavior = c.NPC.behaviour.activeBehaviour.ToString();
                    if (dist < currentConfig.feeningRadius && !currentBehavior.Contains("Request product from player") && !feens.Contains(c) && c.NPC.RelationData.Unlocked)
                    {
                        nearbyCustomers.Add(c);
                        if (nearbyCustomers.Count == currentConfig.maxFeensStack)
                            break;
                    }
                }

                if (nearbyCustomers.Count > 0)
                {
                    //MelonLogger.Msg($"Nearby Feening: {nearbyCustomers.Count}");
                    MelonCoroutines.Start(this.SetFeening(nearbyCustomers, randomPlayer));
                }

            }
        }

        private IEnumerator SetFeening(List<Customer> cs, Player randomPlayer)
        {
            foreach (Customer c in cs)
            {
                yield return new WaitForSeconds(2f);
                if (!c.NPC.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: currentConfig.feeningRadius))
                {
                    continue;
                }
                if (currentConfig.randomEffects)
                    MelonCoroutines.Start(RandomFeenEffect(c));
                c.RequestProduct(randomPlayer);
                feens.Add(c);
                //MelonLogger.Msg("NearbyFeeningDone");
            }

            yield return null;
        }

        

    }
}
