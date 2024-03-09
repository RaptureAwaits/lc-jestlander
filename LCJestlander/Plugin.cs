using BepInEx;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using BepInEx.Logging;

namespace LCJestlander {
    [BepInPlugin(mod_guid, mod_name, mod_version)]
    public class JestlanderBase : BaseUnityPlugin {
		private const string mod_guid = "raptureawaits.jestlander";
		private const string mod_name = "Jestlander";
		private const string mod_version = "1.2.0";

		internal static JestlanderBase instance;
		internal static ManualLogSource modlog;
		
		public static AssetBundle new_sounds;
		internal static AudioClip[] windup_asset;
		internal static AudioClip[] pop_asset;
		internal static AudioClip[] scream_asset;
		
		private readonly Harmony harmony = new(mod_guid);
		
        private void Awake() {
			if (instance == null) {
				instance = this;
			}
			modlog = BepInEx.Logging.Logger.CreateLogSource("Jestlander");

			string mod_dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			new_sounds = AssetBundle.LoadFromFile(Path.Combine(mod_dir, "jestlander_sounds"));
			if (new_sounds == null) {
				modlog.LogError("Failed to load AssetBundle.");
				return;
			}
			
			windup_asset = new_sounds.LoadAssetWithSubAssets<AudioClip>("assets\\audio\\windup.wav");
			pop_asset = new_sounds.LoadAssetWithSubAssets<AudioClip>("assets\\audio\\pop.wav");
			scream_asset = new_sounds.LoadAssetWithSubAssets<AudioClip>("assets\\audio\\scream.wav");
			
			harmony.PatchAll(typeof(Patches.JesterAIPatch));
            modlog.LogInfo($"Plugin {mod_guid} is loaded!");
        }
    }
}

namespace LCJestlander.Patches {
	[HarmonyPatch(typeof(JesterAI))]
	internal class JesterAIPatch {
		internal static ManualLogSource modlog = JestlanderBase.modlog;
		internal static float second_offset;

		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		static void AudioPatch(ref AudioClip ___popUpSFX, ref AudioClip ___screamingSFX) {
			AudioClip[] pop_list = JestlanderBase.pop_asset;
			if (pop_list != null && pop_list.Length > 0) {
				AudioClip _ = pop_list[0];
				___popUpSFX = _;
			} else {
				modlog.LogError("Failed to load pop replacement audio data from extracted assets.");
			}

			AudioClip[] scream_list = JestlanderBase.scream_asset;
			if (scream_list != null && scream_list.Length > 0) {
				AudioClip _ = scream_list[0];
				___screamingSFX = _;
			} else {
				modlog.LogError("Failed to load scream replacement audio data from extracted assets.");
			}
		}

		[HarmonyPatch("SetJesterInitialValues")]
		[HarmonyPostfix]
		static void AudioPatch(ref AudioClip ___popGoesTheWeaselTheme, ref float ___popUpTimer) {
			AudioClip[] windup_list = JestlanderBase.windup_asset;
			if (windup_list != null && windup_list.Length > 0) {
				AudioClip windup_clip = windup_list[0];

				if (GameNetworkManager.Instance.isHostingGame) {
					modlog.LogInfo("Player is host, adjusting windup clip...");
					second_offset = windup_clip.length - 5f - ___popUpTimer;
				} else {
					modlog.LogInfo("Player is not host, using static windup clip.");
					second_offset = windup_clip.length - 40f;
				}

				int sample_offset = (int)(second_offset / windup_clip.length * windup_clip.samples);
				int trim_samples = windup_clip.samples - sample_offset;
				float[] samples = new float[trim_samples * windup_clip.channels];
				windup_clip.GetData(samples, sample_offset);
				AudioClip new_clip = AudioClip.Create(windup_clip.name + "_cropped", trim_samples, windup_clip.channels, windup_clip.frequency, false);
				new_clip.SetData(samples, 0);

				___popGoesTheWeaselTheme = new_clip;
			} else {
				modlog.LogError("Failed to load windup replacement audio data from extracted assets.");
			}
		}
	}
}