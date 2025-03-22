using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects;
using ImGuiNET;
using TrueNorthBoss.Windows;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;

namespace TrueNorthBoss;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;

    private const string CommandName = "/pmycommand";

    public Configuration Configuration { get; init; }
    public readonly WindowSystem WindowSystem = new("TrueNorthBoss");

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png"));

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        // Register the Draw UI function to render the arrow
        PluginInterface.UiBuilder.Draw += DrawUI;

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;

        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        MainWindow.Dispose();
        CommandManager.RemoveHandler(CommandName);

        // Unsubscribe from the draw event when the plugin is unloaded
        PluginInterface.UiBuilder.Draw -= DrawUI;
    }

    private void OnCommand(string command, string args)
    {
        ToggleMainUI();
    }

    private void DrawUI()
    {
        var target = TargetManager.Target;
        if (target == null) return;

        Vector3 targetPosition = target.Position;

        // 🎯 Smarter offset: Higher for dummies, lower for normal mobs
        float ringYOffset = target.Name.ToString().Contains("Dummy") ? 0.2f : 0.05f;
        float ringY = target.Position.Y - ringYOffset;

        // 💡 Dynamic radius based on hitbox
        float ringRadius = target.HitboxRadius;

        Vector3 centerOnRing = new Vector3(targetPosition.X, ringY, targetPosition.Z);
        Vector3 northOnRing = new Vector3(targetPosition.X, ringY, targetPosition.Z - ringRadius);

        Vector2 screenCenter, screenNorth;
        bool centerOnScreen = GameGui.WorldToScreen(centerOnRing, out screenCenter);
        bool northOnScreen = GameGui.WorldToScreen(northOnRing, out screenNorth);
        if (!centerOnScreen || !northOnScreen) return;

        var drawList = ImGui.GetForegroundDrawList();

        // 🎨 Bright cyan with glow
        Vector4 glowColorVec = new Vector4(0.0f, 1.0f, 1.0f, 0.3f);
        Vector4 mainColorVec = new Vector4(0.0f, 1.0f, 1.0f, 1.0f);
        uint glowColor = ImGui.GetColorU32(glowColorVec);
        uint mainColor = ImGui.GetColorU32(mainColorVec);

        for (float thickness = 10.0f; thickness >= 2.0f; thickness -= 2.0f)
            drawList.AddLine(screenCenter, screenNorth, glowColor, thickness);

        drawList.AddLine(screenCenter, screenNorth, mainColor, 4.0f);
    }

    public void ToggleConfigUI() => ConfigWindow.Toggle();
    public void ToggleMainUI() => MainWindow.Toggle();
}
