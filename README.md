# Schedule I Feening NPCs Mod
- NEEDS MELON LOADER

- Forces your customers to be feening for more... They will surround you in groups.

### IMPORTANT!
- "alternate" or "alternate-beta" branch users download the FeeningNPCs-Mono 
- "default" or "beta" branch users download the FeeningNPCs-IL2CPP

### Installation Steps:

1. Install Melon Loader from a trusted source like [MelonWiki](https://melonwiki.xyz/).
2. Copy the DLL file and the `FeeningNPCs` folder (with `config.json`) into the `Mods` folder.
3. You are good to go!

### Optional Configuration Steps:

- Open the FeeningNPCs Folder and it has a file called config.json
- Open the config.json file, its contents by default are:
```
{
  "maxFeensStack": 3,
  "feeningTreshold": 60,
  "feensClearThreshold": 1440,
  "feeningRadius": 30,
  "randomEffects": true,
  "increaseEconomy": true,
  "economyMultiplier": 10,
  "ignoreExistingDeals": true
}
```

- maxFeensStack: How many nearby Customers can be selected as Feens at once
- feeningTreshold: The minimum time application waits to select new Feens
- feensClearThreshold: How often a Customer can become a Feen again
- feeningRadius: Minimum distance from player that Customer must be to become a Feen
- randomEffects: When true, rolls random events like angry noises, ragdolls, aggression changes, etc.
- increaseEconomy: When true, slowly starts increasing customer spending behaviour to compensate for the feening.
- economyMultiplier: value between 1-10, how fast economy increases (1 slow increase, 10 mod default increase)
- ignoreExistingDeals: when true, customers with existing deals can also become feens

Note: The config.json file will get created automatically in the Mods/FeeningNPCs/config.json directory if missing.
