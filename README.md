# Schedule I Feening NPCs Mod
- NEEDS MELON LOADER

- Forces your customers to be feening for more... They will surround you in groups.
- Allows for manual configuration of Weekly spending habits per customer for all customers

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
  "feensClearThreshold": 900,
  "feeningRadius": 30,
  "randomEffects": true,
  "increaseEconomy": true,
  "economyMultiplier": 10,
  "ignoreExistingDeals": false,
  "randomBonus": true,
  "randomBonusChance": 0.3,
  "persistEconomy": true
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
- randomBonus: when True rolls a random cash + xp bonus when handing over product, Bonus price is always relative to your handed over product price.
- randomBonusChance: value between 0.1 - 1.0 (1.0 is always give random bonus)
- persistEconomy: When true, your customers spending habits are saved to a JSON file and every time the save is loaded, your customers spending habits are automatically applied. The JSON file name is your Organisation name in the save. You Save the current Economy by Exiting the Game to Main Menu -> Automatically saves economy file to: `Mods/FeeningNPCs/Economy/your_organisation.json`. 


> Note: The config.json file will get created automatically in the Mods/FeeningNPCs/config.json directory if missing.

> Note: The Economy directory and your Customers per Save file will get created to Mods/FeeningNPCs/Economy/your_organisation.json
> You can delete this file JSON file at any time to reset economy. You can manually configure individual customers here.