using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace WhatDidYouSay;

internal class DalamudAPI
{
	[PluginService] internal static IFramework Framework { get; private set; } = null!;
	[PluginService] internal static IClientState ClientState { get; private set; } = null!;
	[PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
	[PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
	[PluginService] internal static IDataManager DataManager { get; private set; } = null!;
	[PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;
	[PluginService] internal static ICondition Condition { get; private set; } = null!;
	[PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
	[PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
}