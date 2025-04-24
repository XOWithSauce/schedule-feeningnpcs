## Changelog

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