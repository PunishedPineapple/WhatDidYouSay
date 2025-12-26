using System;

using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

namespace WhatDidYouSay;

public sealed class PluginUI : IDisposable
{
	public PluginUI( Plugin plugin, Configuration configuration, IDalamudPluginInterface pluginInterface )
	{
		mPlugin = plugin;
		mConfiguration = configuration;
		mPluginInterface = pluginInterface;

		Window_Settings = new( mPlugin, this, mConfiguration );
		Window_Settings_ZoneOverride = new( mPlugin, this, mConfiguration );
		Window_Debug = new( mPlugin, this, mConfiguration );

		mWindowSystem.AddWindow( Window_Settings );
		mWindowSystem.AddWindow( Window_Settings_ZoneOverride );
		mWindowSystem.AddWindow( Window_Debug );
	}

	public void Dispose()
	{
		mWindowSystem.RemoveAllWindows();

		Window_Settings?.Dispose();
		Window_Settings_ZoneOverride?.Dispose();
		Window_Debug?.Dispose();

		Window_Settings = null;
		Window_Settings_ZoneOverride = null;
		Window_Debug = null;
	}

	public void Draw() => mWindowSystem.Draw();

	private readonly Plugin mPlugin;
	private readonly IDalamudPluginInterface mPluginInterface;
	private readonly Configuration mConfiguration;
	private readonly WindowSystem mWindowSystem = new( Plugin.Name );

	internal Window_Settings Window_Settings { get; private set; }
	internal Window_Settings_ZoneOverride Window_Settings_ZoneOverride { get; private set; }
	internal Window_Debug Window_Debug { get; private set; }
}