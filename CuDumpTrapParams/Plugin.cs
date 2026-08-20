using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using HarmonyLib.Tools;

namespace CuDumpTrapParams;

[BepInPlugin(ModGUID, ModName, ModVersion)]
public class Plugin : BaseUnityPlugin
{
	public const string ModGUID = "vee.dump.trap.params";
	public const string ModName = "CuDumpTrapParams";
	public const string ModVersion = "1.0.0";

	internal new static ManualLogSource Logger;
	private readonly Harmony _harmony = new(ModGUID);
	public static Plugin Instance { get; private set; } = null!;
	
	internal static void PrintOut(string message)
	{
		ConsoleScript.instance.LogToConsole(message);
		Plugin.Logger.LogMessage(message);
	}

	public void Awake()
	{
		Logger = base.Logger;
		Instance = this;

		_harmony.PatchAll();

		Logger.LogInfo($"Plugin {ModName} is loaded!");
	}

	public void OnDestroy()
	{
		_harmony?.UnpatchSelf();
		Instance = null;
	}
}

