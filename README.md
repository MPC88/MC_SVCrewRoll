# MC_SVCrewRoll

Backup your save. You can delete the mod .dll and the save data, but your crew will be forever changed.

Requires BepInEx - https://github.com/BepInEx/BepInEx/releases/tag/v5.4.21

Place both:  
mc_svcrewroll  
mc_svcrewroll.dll  

in .\Star Valor\BepInEx\plugins\

Configurable:

Base skill reroll cost (default: 50000).  
Base bonus reroll cost (default: 75000).  
Whether a skill's level is retained when rerolled (default: on).  
% chance of allowing a duplicate skill (default: 0.1% - supports 3 decimal places, anything more is effectively rounded up).  
Restricted skill rolling (default: enabled - if disabled, duplicate and triplicate skill rolls are unrestricted).  
Possible bonuses popup delay (default: 1s)  

settings in .\Star Valor\BepInEx\config\mc.starvalor.crewroll.cfg after first time launching game with mod installed.  

Use:

New menu in station.  
List of crew contains any crew in station or ship cargo (regardless of location). Assigned crew not listed. Unique crew not listed.  
"Reroll skills" button rerolls unlocked skills.  
Dice icon will reroll unlocked bonuses for a skill.  
Lock icon will lock or unlock a skill or bonus.  
Existing skill "+" icon will add or level a bonus where the skill has capacity for a new bonus.  
Add skill "+" icon shown only if crew has sufficient rarity for another skill, but hasn't learnt one yet.  

Costs:

Lock or unlock a skill or bonus OR reroll bonuses = e^(0.3 x level) x base cost rounded to integer  
Adding a skill = crew level / 10 (or 1 if below level 10) x base cost x number of skills crew already has  
Reroll skills = sum of all individual skill costs. A skill's cost is added twice if that skill is locked.  
