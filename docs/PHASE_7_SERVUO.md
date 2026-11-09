# ServUO source comparison — Phase 7

This release consulted the actual pinned ServUO sources included under vendor/ServUO, rather than relying on remembered UO behavior. manifests/phase7-servuo-sources.json records the exact bytes. Server adaptations are GPL-2.0; the original content contracts and rendering/UI remain MIT. No EA quest prose, cliloc text, imagery or sound is imported into the playable package.

| Bundled ServUO source | Adopted behavior / deliberate adaptation |
| --- | --- |
| Scripts/Quests/BaseQuest.cs | Explicit acceptance, all-objective completion, later reward claim, one-time completion and backpack-to-ground reward fallback. Original Greyhaven quest data and command protocol. |
| Scripts/Services/MondainsLegacyQuests/QuestObjectives.cs, ObtainObjective | Player explicitly marks obtained items as quest items; counts use quantities. Greyhaven derives counts from carried marked objects to prevent stale-count turn-ins, and consumes exactly the requested units. |
| Server/Skills.cs; Config/PlayerCaps.cfg | Real skill IDs, tenths, Up/Down/Locked, 100.0 individual and 700.0 total caps; 225 total/125 individual stat caps. Only eleven relevant skills are exposed, not UO's full skill catalog. |
| Scripts/Misc/SkillCheck.cs | Skill challenge chance, fractional gains, gain probability based on remaining individual/total cap, small novice gains, decrease donors at total cap, five-percent stat-gain attempts. Persisted xorshift RNG substitutes for ServUO Utility.Random and the single-player host owns checks. No anti-macro system; source configuration disables it by default. |
| Scripts/Mobiles/Normal/BaseCreature.cs, teaching methods | Teacher skill divided by three, at most 42.0, gain lock required, one gold-equivalent per tenth. Silver marks replace gold. The button requests 1.0 and the host charges the actual partial amount at the ceiling. |
| Scripts/Mobiles/NPCs/BaseVendor.cs | Sixty-minute restock and nearby service. Greyhaven replenishes a small fixed stock list rather than implementing UO's full buy/sell economy. |
| Scripts/Mobiles/AI/BaseAI.cs, DoActionWander and WalkRandomInHome | Home-relative collision-valid stepping inspired town walking. Work/evening/home schedules are original additions; service residents remain stationary for this demo. |
| Scripts/Items/Tools/Clocks.cs | Five real seconds per world minute. Greyhaven uses one island clock, omits UO's map-coordinate longitude offset, and pauses the clock with the campaign. |
| Scripts/Items/Equipment/Weapons/BaseWeapon.cs | AOS accuracy ratio (attack+20)/(2*(defense+20)); STR/Tactics/Anatomy damage contributions. Original monster defense values and normalized original damage maintain the existing demo's baseline. Shield parry is simplified. |
| Scripts/Spells/Base/MagerySpell.cs | Circle-related Magery check around average 100/7 times zero-based circle, with a ±20 range. Greyhaven's eight original spells receive demo circle mappings; UO reagents, inscription, casting schools and full spell effects are outside scope. EvalInt/INT damage scaling is a Greyhaven adaptation. |
| Scripts/Skills/Meditation.cs | Free hands, skill/mana-based success and ten-second retry. Greyhaven asks the player to put equipment into the backpack rather than moving it automatically; extra mana recovery is simplified. |
| Scripts/Items/Resource/Bandage.cs | AOS self-heal delay ceil(11-DEX/20), clamped 4–8 seconds; Healing success and Anatomy/Healing amount with hit slips. Greyhaven supports self-bandaging, not veterinary, resurrection bandages or healing other players. |
| Scripts/Misc/Titles.cs | Fame 0–32000, karma ±32000, diminishing fame rewards. Quest reputation is adapted; the Lantern Warden title is original, not the full UO notoriety/crime/title system. |

The finite island intentionally credits previously defeated quest encounters and discovered landmarks on acceptance. This differs from repeatable MMO kill counting and makes existing saves and one-time enemies playable. The bundled legacy ServUO scripts are reference and source dependencies; the original-content host does not execute the entire legacy shard script catalog.
