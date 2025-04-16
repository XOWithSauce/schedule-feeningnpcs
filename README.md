# Schedule I Feening NPCs Mod
- NEEDS MELON LOADER

- Forces your customers to be feening for more... They will surround you in groups.

### IMPORTANT!
- "alternate" or "alternate-beta" branch users download the FeeningNPCs-Mono 
- "default" or "beta" branch users download the FeeningNPCs-IL2CPP

### Installation Steps:

- Install Melon Loader from a Trusted Source like https://melonwiki.xyz/
- Manually download the correct .zip file and then unzip the file.
- Copy the DLL file and FeeningNPCs folder (with config.json) into the Mods folder and you are good to go

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
  "increaseEconomy": true
}
```
- maxFeensStack: How many nearby Customers can be selected as Feens at once
- feeningTreshold: The minimum time application waits to select new Feens
- feensClearThreshold: How often a Customer can become a Feen again
- feeningRadius: Minimum distance from player that Customer must be to become a Feen
- randomEffects: When true, rolls random events like angry noises, ragdolls, aggression changes, etc.
- increaseEconomy: When true, slowly starts increasing customer spending behaviour to compensate for the feening.

Note: The config.json file will get created automatically in the Mods/FeeningNPCs/config.json directory if missing.

## Changelog

### Version v1.2.2
- Removed potentially broken functions from the `randomEffects`
- Moved all Economy and Spending related functions to be their own logic
- Added new `increaseEconomy` config variable to allow disabling the Economy and Spending logic
- Moved Ragdolling and potential customer attack to their own coroutines
- Customer attacking you during feening now requires customer relation below 3.8 (game default 2.5 - max 5.0)
- Added safety checks to avoid getting null reference exceptions
- Increased the speed at which attacking and ragdolling coroutines evaluate
- Fixed il2cpp json config loading to use newtonsoft assembly

### Version v1.2
- Added `config.json` support
- Changed feening behaviour to not always apply aggression
- Changed feening behaviour to roll from a set of random events

### Version v1.1.1
- Stability and bug fixes
