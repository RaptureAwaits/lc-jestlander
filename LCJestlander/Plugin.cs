using BepInEx;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using System;
using BepInEx.Logging;

namespace LCJestlander {
    [BepInPlugin(mod_guid, mod_name, mod_version)]
    public class JestlanderBase : BaseUnityPlugin {
		private const string mod_guid = "raptureawaits.jestlander";
		private const string mod_name = "Jestlander";
		private const string mod_version = "1.0.0";

		internal static JestlanderBase instance;
		internal static ManualLogSource modlog;
		
		public static AssetBundle new_sounds;
		internal static AudioClip[] windup_audio;
		internal static AudioClip[] pop_audio;
		internal static AudioClip[] scream_audio;
		
		private readonly Harmony harmony = new(mod_guid);
		
        private void Awake() {
			if (instance == null) {
				instance = this;
			}
			modlog = BepInEx.Logging.Logger.CreateLogSource(mod_guid + "_base");

			string mod_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			new_sounds = AssetBundle.LoadFromFile(Path.Combine(mod_dir, "jestlander_sounds"));
			if (new_sounds == null) {
				modlog.LogError("Failed to load AssetBundle.");
				return;
			}
			
			windup_audio = new_sounds.LoadAssetWithSubAssets<AudioClip>("assets\\audio\\windup.wav");
			pop_audio = new_sounds.LoadAssetWithSubAssets<AudioClip>("assets\\audio\\silence.wav");
			scream_audio = new_sounds.LoadAssetWithSubAssets<AudioClip>("assets\\audio\\scream.wav");
			
			harmony.PatchAll(typeof(Patches.JesterAIPatch));
            modlog.LogInfo($"Plugin {mod_guid} is loaded!");
        }
    }
}

namespace LCJestlander.Patches {
	[HarmonyPatch(typeof(JesterAI))]
	internal class JesterAIPatch {
		internal static ManualLogSource modlog = JestlanderBase.modlog;

		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		static void AudioPatch(ref AudioClip ___popUpSFX, ref AudioClip ___screamingSFX) {
			AudioClip[] pop_replace = JestlanderBase.pop_audio;
			if (pop_replace != null && pop_replace.Length > 0) {
				AudioClip _ = pop_replace[0];
				___popUpSFX = _;
			}

			AudioClip[] scream_replace = JestlanderBase.scream_audio;
			if (scream_replace != null && scream_replace.Length > 0) {
				AudioClip _ = scream_replace[0];
				___screamingSFX = _;
			}
		}

		[HarmonyPatch("SetJesterInitialValues")]
		[HarmonyPostfix]
		static void AudioPatch(ref AudioClip ___popGoesTheWeaselTheme, ref float ___popUpTimer) {
			AudioClip[] windup_replace = JestlanderBase.windup_audio;
			if (windup_replace != null && windup_replace.Length > 0) {
				AudioClip windup_clip = windup_replace[0];

				float second_offset = windup_clip.length - ___popUpTimer;
				int sample_offset = (int)(second_offset / windup_clip.length * windup_clip.samples);

				int trim_samples = windup_clip.samples - sample_offset;
				float[] samples = new float[trim_samples * windup_clip.channels];
				windup_clip.GetData(samples, sample_offset);

				
				AudioClip cropped_windup = AudioClip.Create(windup_clip.name + "_cropped", trim_samples, windup_clip.channels, windup_clip.frequency, false);
				cropped_windup.SetData(samples, 0);
				
				___popGoesTheWeaselTheme = cropped_windup;
			}
		}
	}
}