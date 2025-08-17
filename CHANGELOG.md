## Changelog
### Version v1.3.0
- Added a feature to persist economy in individual saves and allow for gradual buildup of customer spending
- Made persistance write the full Customer economy data to a file in FeeningNPCs/Economy/example_organisation.json. Added configuration value "persistEconomy" which by default is true.
- Added a new feature RandomBonus and RandomBonusChance -> At each handover has a chance to award player with extra cash reward. Cash rewards scale directly with the deal price.
- Added a feature to RandomBonus, where player gets +1xp for each rewarded time.
- Changed the Default configuration value for "ignoreExistingDeals" to false due to causing a bug where any outstanding deals would be completed if they start feening and player gives them product.
- Changed the JSON Serialization library to JSONConvert from JSONUtility in both branches Mono and IL2Cpp. Previous library could not handle persistance serialization.
- Handovers now award player with extra XP based on the economyMultiplier, random value from 2-12 extra xp is picked at each handover. If a hired dealer completes handover then there is 80% chance to award +2 extra XP!

- Lowered Maximum and Minimum weekly order increments to be 1 instead of 1-3 to promote longevity with persistance mechanism. Additionally these weekly order increments have 50% chance to fail to prevent too fast growth and improve replay value.
- Lowered the cap to which Maximum and Minimum Weekly spendings can increase to, from 5mil and 1.666mil down to 80k and 20k respectively.
- Further tied the economy growth of the customer to internal variables. Higher Customer dependence will now increase likelihood of higher economy growth. Higher Addiction and Relationship also increase the maximum and minimum spending caps.
- Added a new feature where economy growth starts to slow down towards end game based on player networth, lifetime earnings and cash balance, again to promote longevity and persistance.

- Added 2 harmony patches for Exiting to main menu and also Loading last save upon death. These fix a common bug where coroutine would not properly suspend when it should.
- Exiting to Main Menu from the world is the only way to save the economy to the .json file as of now. Saving / Sleeping does not save economy automatically.

- Fixed a bug where the calculation for economy growth would set the max and min weekly spendings to maximum value instantly.
- Fixed miscellanious variable and class namings to be up to date with the latest game update and allow for forwards compatibility.

### Version v1.2.3
- Added new Economy Multiplier configuration parameter
- Added new Ignore Existing Deals configuration parameter
- Fixed the logic that breaks coroutines by patching the Exit to Menu function
- Added new Patch for Process Handover to increase economy on each deal during the session
- The Process Handover increase is enabled only if Increase Economy is true
- The Process Handover increase is tied directly to the economy multiplier, player earnings and balance and customer current spending habits
- Capped the Orders per week increases to be in 1-3 range based on Economy Multiplier
- Changed the Max and Min weekly spending to use higher cap for spending habits per session
- Changed the "Player" to be the Local Player instead of randomly selected from an array of all player objects
- Changed evaluation logic for selecting new feens to check earlier for distance (less unnecessary calcs)
- Changed evaluation logic for selecting new feens to account for existing deals
- Changed evaluation logic for selecting new feens to account for NPC Movement status

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