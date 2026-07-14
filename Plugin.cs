using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AGDDebugHUD
{
    [BepInPlugin("com.unii.debugHud", "Debug HUD", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> EnabledConfig;
        public static ConfigEntry<KeyCode> ToggleKeyConfig;
        
        public static ConfigEntry<bool> EnableGameSectionConfig;
        public static ConfigEntry<bool> EnableFPSConfig;
        public static ConfigEntry<bool> EnableHUDFPSConfig;
        
        public static ConfigEntry<bool> EnableNetworkSectionConfig;
        public static ConfigEntry<bool> EnablePingConfig;
        
        public static ConfigEntry<bool> EnableMovementSectionConfig;
        public static ConfigEntry<bool> EnableVelocityConfig;
        public static ConfigEntry<bool> EnablePositionConfig;
        
        public static ConfigEntry<bool> EnablePhysicsSectionConfig;
        public static ConfigEntry<bool> EnableRagdollStateConfig;

        public static ConfigEntry<bool> EnableStatusSectionConfig;
        public static ConfigEntry<bool> EnableStateConfig;
        public static ConfigEntry<bool> EnableStatusEffectsConfig;

        private void Awake()
        {
            EnabledConfig = Config.Bind(
                "General",
                "Enabled",
                true,
                "Enable or disable the Debug HUD overlay."
            );
            
            ToggleKeyConfig = Config.Bind(
                "General", 
                "ToggleKey",
                KeyCode.F3,
                "Key to toggle the debug HUD on/off."
            );

            
            // Game Section
            EnableGameSectionConfig = Config.Bind(
                "DefaultLines",
                "EnableGameSection",
                true,
                "Enable the values under the Game header. Overrides EnableFPS and EnableHUDFPS."
            );
            
            EnableFPSConfig = Config.Bind(
                "DefaultLines",
                "EnableFPS",
                true,
                "Enable showing FPS and frame time under the Game header."
            );

            EnableHUDFPSConfig = Config.Bind(
                "DefaultLines",
                "EnableHUDFPS",
                true,
                "Enable showing HUD specific frame time and FPS impact under the Game header."
            );

            // Network Section
            EnableNetworkSectionConfig = Config.Bind(
                "DefaultLines",
                "EnableNetworkSection",
                true,
                "Enable the values under the Network header. Overrides EnablePing."
            );
            
            EnablePingConfig = Config.Bind(
                "DefaultLines",
                "EnablePing",
                true,
                "Enable showing Ping under the Network header."
            );
            
            // Movement Section
            EnableMovementSectionConfig = Config.Bind(
                "DefaultLines",
                "EnableMovementSection",
                true,
                "Enable the values under the Movement header. Overrides EnableVelocity and EnablePosition."
            );
            
            EnableVelocityConfig = Config.Bind(
                "DefaultLines",
                "EnableVelocity",
                true,
                "Enable showing player velocity under the Movement header."
            );
            
            EnablePositionConfig = Config.Bind(
                "DefaultLines",
                "EnablePosition",
                true,
                "Enable showing player coordinates under the Movement header."
            );
            
            // Physics Section
            EnablePhysicsSectionConfig = Config.Bind(
                "DefaultLines",
                "EnablePhysicsSection",
                true,
                "Enable the values under the Physics header. Overrides EnableRagdollState."
            );
            
            EnableRagdollStateConfig = Config.Bind(
                "DefaultLines",
                "EnableRagdollState",
                true,
                "Enable showing the player's ragdoll state under the Physics header."
            );
            
            // Status Section
            EnableStatusSectionConfig = Config.Bind(
                "DefaultLines",
                "EnableStatusSection",
                true,
                "Enable the values under the Status header. Overrides EnableState and EnableStatusEffects."
            );
            
            EnableStateConfig = Config.Bind(
                "DefaultLines",
                "EnableState",
                true,
                "Enable showing the player's current state under the Status header."
            );
            
            EnableStatusEffectsConfig = Config.Bind(
                "DefaultLines",
                "EnableStatusEffects",
                true,
                "Enable showing active status effects under the Status header."
            );
            
            Logger.LogInfo("Debug HUD plugin loaded successfully!");

            var harmony = new Harmony("com.unii.debugHud");
            harmony.PatchAll();
        }
    }
}