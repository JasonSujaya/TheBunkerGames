using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TheBunkerGames.Editor
{
    /// <summary>
    /// Editor tool that creates and populates ScriptableObject assets
    /// for the Player Action System (challenge data + story log).
    /// Menu: TheBunkerGames > Player Actions > Create All Assets
    /// </summary>
    public static class PlayerActionSetupTool
    {
        private const string SOFolder = "Assets/_Game/ScriptableObjects/PlayerActions";

        // =====================================================================
        // Menu Items
        // =====================================================================

        [MenuItem("TheBunkerGames/Player Actions/Create All Assets", priority = 100)]
        public static void CreateAllAssets()
        {
            EnsureFolders();
            CreateChallengePool();
            CreateStoryLog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlayerActionSetup] All assets created and populated!");
        }

        // =====================================================================
        // Challenge Pool
        // =====================================================================
        private static void CreateChallengePool()
        {
            string path = $"{SOFolder}/PlayerActionChallengePool.asset";
            var asset = ScriptableObject.CreateInstance<PlayerActionChallengePoolSO>();

            // --- EXPLORATION (15) ---
            var exploration = new List<PlayerActionChallenge>();
            exploration.Add(Make(PlayerActionCategory.Exploration, "Locked Door", "You've found a heavy locked door in the lower bunker level. It might lead to a supply cache. How do you get it open?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Collapsed Tunnel", "A section of the maintenance tunnel has caved in, blocking access to the water pump. How do you clear the debris?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Flooded Room", "The storage room is ankle-deep in murky water from a burst pipe. Important supplies are on the shelves. How do you retrieve them safely?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Jammed Air Vent", "The main ventilation shaft is clogged with dust and debris. Air quality is dropping. How do you fix the airflow?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Broken Generator", "The backup generator sputtered and died. Without it, you lose lighting and the water pump. How do you get it running again?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Contaminated Water Tank", "The main water tank has a strange discoloration. It might be contaminated. How do you handle the water situation?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Strange Noises Above", "You hear scratching and thumping sounds from the ceiling. Something is above the bunker. How do you investigate?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Rusted Hatch", "The emergency exit hatch is rusted shut. You might need it as an escape route. How do you try to free it?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Flickering Lights", "The electrical wiring is sparking in the main corridor. It's a fire hazard and could short out the whole system. How do you deal with it?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Rat Infestation", "Rats have gotten into the food storage area. They're chewing through packaging and contaminating supplies. How do you handle the pest problem?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Mysterious Radio Signal", "The old radio picked up a faint transmission. It could be survivors, military, or a trap. How do you respond?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Cracked Wall", "A large crack has appeared in the bunker wall. You can feel cold air seeping through. Is the structural integrity at risk? What do you do?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Medicine Cabinet", "You found a sealed medicine cabinet in an unused room, but it has a combination lock. How do you access it?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Blocked Drain", "The bunker's waste drain is completely blocked. Sewage is starting to back up. How do you unclog it before it becomes a health crisis?"));
            exploration.Add(Make(PlayerActionCategory.Exploration, "Solar Panel Access", "There's a solar panel array on the surface that could supplement power, but going outside is dangerous. How do you attempt to connect it?"));
            SetField(asset, "explorationChallenges", exploration);

            // --- DILEMMA (10) ---
            var dilemmas = new List<PlayerActionChallenge>();
            dilemmas.Add(Make(PlayerActionCategory.Dilemma, "Water Rationing", "Water supplies are running critically low. You can ration equally (everyone suffers a little) or prioritize the children (adults go thirsty). What do you decide?"));
            dilemmas.Add(Make(PlayerActionCategory.Dilemma, "Stranger at the Door", "Someone is banging on the bunker door begging for help. They claim to be injured. Letting them in risks your family's safety. Ignoring them means they might die. What do you do?"));
            dilemmas.Add(Make(PlayerActionCategory.Dilemma, "Stolen Supplies", "You discover that food has been disappearing. Circumstantial evidence points to a family member sneaking extra rations at night. How do you handle this?"));
            dilemmas.Add(Make(PlayerActionCategory.Dilemma, "Power Rationing", "The generator fuel is almost gone. You can keep the lights on (morale), run the water pump (hydration), or power the radio (information). You can only pick one. Which do you choose?"));
            dilemmas.Add(Make(PlayerActionCategory.Dilemma, "Sick Outsider", "A wounded stranger managed to get inside. They're clearly infected with something. Using your limited medicine on them means less for your family. What do you do?"));
            dilemmas.Add(Make(PlayerActionCategory.Dilemma, "The Last Antibiotics", "Two family members are getting sick. You only have enough antibiotics for one. Who gets the medicine?"));
            dilemmas.Add(Make(PlayerActionCategory.Dilemma, "Risky Trade", "A passing trader offers a large amount of food in exchange for your only weapon. Without the weapon you're defenseless, but without food you'll starve. What's your call?"));
            dilemmas.Add(Make(PlayerActionCategory.Dilemma, "Evacuation Rumor", "A radio broadcast claims evacuation helicopters are coming to a location 2 days' walk away. It could be real, or it could be a raider trap. Do you stay in the bunker or risk the journey?"));
            dilemmas.Add(Make(PlayerActionCategory.Dilemma, "The Confession", "A family member confesses they've been secretly communicating with outsiders via radio, potentially revealing your location. They say they were trying to find help. How do you respond?"));
            dilemmas.Add(Make(PlayerActionCategory.Dilemma, "Contaminated Food", "Half of your remaining food supply may have been exposed to contamination. Eating it risks sickness. Throwing it away means going hungry. What's the plan?"));
            SetField(asset, "dilemmaChallenges", dilemmas);

            // --- FAMILY (10) ---
            var family = new List<PlayerActionChallenge>();
            family.Add(MakeFamily("High Fever", "{target} has developed a high fever and is shivering uncontrollably. They need care and possibly medicine. How do you help them?"));
            family.Add(MakeFamily("Nightmares", "{target} hasn't been sleeping. They keep waking up screaming from nightmares about the outside world. Their sanity is slipping. How do you comfort them?"));
            family.Add(MakeFamily("Refusing to Eat", "{target} has stopped eating. They say they'd rather the others have their share. They're getting weaker by the day. How do you convince them to eat?"));
            family.Add(MakeFamily("Panic Attack", "{target} is having a severe panic attack. They're hyperventilating and saying the walls are closing in. How do you calm them down?"));
            family.Add(MakeFamily("Infected Wound", "{target} has a wound that's turning red and swollen. It looks infected. Without treatment it could get much worse. What do you do?"));
            family.Add(MakeFamily("Homesick and Hopeless", "{target} has completely lost hope. They keep talking about how pointless it is to keep trying. Their despair is affecting everyone. How do you lift their spirits?"));
            family.Add(MakeFamily("Family Conflict", "{target} got into a heated argument with another family member. Tensions are high and they're refusing to speak to each other. How do you mediate?"));
            family.Add(MakeFamily("Dehydration", "{target} is showing signs of severe dehydration: dry lips, dizziness, confusion. They need water urgently, but supplies are limited. How do you handle this?"));
            family.Add(MakeFamily("Broken Bone", "{target} fell and may have broken their arm. They're in significant pain. You have limited medical supplies. How do you treat the injury?"));
            family.Add(MakeFamily("Wants to Leave", "{target} wants to leave the bunker alone to search for help. It's dangerous outside, but they're determined. How do you respond to their plan?"));
            SetField(asset, "familyRequestChallenges", family);

            // Save
            var existing = AssetDatabase.LoadAssetAtPath<PlayerActionChallengePoolSO>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                Object.DestroyImmediate(asset);
                Debug.Log($"[PlayerActionSetup] Updated: {path} ({exploration.Count} + {dilemmas.Count} + {family.Count} challenges)");
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[PlayerActionSetup] Created: {path} ({exploration.Count} + {dilemmas.Count} + {family.Count} challenges)");
            }
        }

        // =====================================================================
        // Story Log
        // =====================================================================
        private static void CreateStoryLog()
        {
            string path = $"{SOFolder}/StoryLog.asset";
            if (AssetDatabase.LoadAssetAtPath<StoryLogSO>(path) != null) { Debug.Log($"[PlayerActionSetup] StoryLog already exists."); return; }
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<StoryLogSO>(), path);
            Debug.Log($"[PlayerActionSetup] Created: {path}");
        }

        // =====================================================================
        // Helpers
        // =====================================================================
        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/_Game", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder(SOFolder))
                AssetDatabase.CreateFolder("Assets/_Game/ScriptableObjects", "PlayerActions");
        }

        private static PlayerActionChallenge Make(PlayerActionCategory cat, string title, string desc)
        {
            var c = new PlayerActionChallenge();
            SetField(c, "category", cat);
            SetField(c, "title", title);
            SetField(c, "description", desc);
            SetField(c, "targetCharacter", "");
            return c;
        }

        private static PlayerActionChallenge MakeFamily(string title, string desc)
        {
            return Make(PlayerActionCategory.FamilyRequest, title, desc);
        }

        private static void SetField(object obj, string name, object value)
        {
            var f = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null) f.SetValue(obj, value);
        }
    }
}
