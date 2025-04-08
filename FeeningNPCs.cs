using MelonLoader;
using System.Collections;
using ScheduleOne.PlayerScripts;
using ScheduleOne.Economy;
using UnityEngine;
using ScheduleOne.Persistence;
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
        public const string Version = "1.1";
        public const string DownloadLink = null;
    }
    public class FeeningNPCs : MelonMod
    {
        Customer[] cmers;
        List<object> coros = new();
        Dictionary<Customer, float> feens = new();
        private bool registered = false;
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
                    registered = true;
                }
                
            }
            else
            {
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
            this.cmers = UnityEngine.Object.FindObjectsOfType<Customer>(true);

            coros.Add(MelonCoroutines.Start(this.ChangeBehv()));
            coros.Add(MelonCoroutines.Start(this.ClearFeens()));
        }
        private IEnumerator ClearFeens()
        {
            for (; ; )
            {
                yield return new WaitForSeconds(1440f);
                // MelonLogger.Msg("Clear Feens");
                foreach (var feen in feens)
                {
                    Customer customer = feen.Key;
                    float originalAggression = feen.Value;
                    customer.NPC.OverrideAggression(originalAggression);
                }

                feens.Clear();
            }
        }

        private IEnumerator ChangeBehv()
        {
            for (; ; )
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(60f, 180f));
                MelonLogger.Msg("Evaluate Nearby Feens");
                Player[] players = UnityEngine.Object.FindObjectsOfType<Player>(true);
                if (players.Length == 0) { continue; }
                Player randomPlayer = players[UnityEngine.Random.Range(0, players.Length)];
                List<Customer> nearbyCustomers = new();

                foreach (Customer c in cmers)
                {
                    float dist = Vector3.Distance(c.NPC.transform.position, randomPlayer.transform.position);
                    string currentBehavior = c.NPC.behaviour.activeBehaviour.ToString();
                    if (dist < 10f && !currentBehavior.Contains("Request product from player") && !feens.Keys.Contains(c) && c.NPC.RelationData.Unlocked)
                    {
                        nearbyCustomers.Add(c);
                        if (nearbyCustomers.Count == 3)
                            break;
                    }
                }

                if (nearbyCustomers.Count > 0)
                {
                    MelonLogger.Msg($"Nearby Feening: {nearbyCustomers.Count}");
                    MelonCoroutines.Start(this.SetFeening(nearbyCustomers, randomPlayer));
                }

            }
        }

        private IEnumerator SetFeening(List<Customer> cs, Player randomPlayer)
        {
            foreach (Customer c in cs)
            {
                yield return new WaitForSeconds(2f);
                if (!c.NPC.Movement.CanGetTo(randomPlayer.transform.position, proximityReq: 30f))
                {
                    continue;
                }

                float origAggr = c.NPC.Aggression;
                c.NPC.OverrideAggression(1f);
                c.RequestProduct(randomPlayer);
                feens.Add(c, origAggr);
                MelonLogger.Msg("NearbyFeeningDone");
            }

            yield return null;
        }

    }
}
