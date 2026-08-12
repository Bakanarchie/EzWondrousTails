using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using KamiToolKit;

namespace WondrousTailsSolver;

public sealed class WondrousTailsSolverPlugin : IDalamudPlugin {
    public WondrousTailsSolverPlugin(IDalamudPluginInterface pluginInterface, IFramework framework) {
        System.PerfectTails = new PerfectTails();
        System.AddonWeeklyBingoController = new AddonWeeklyBingoController(pluginInterface, framework);
    }

    public void Dispose() {
        System.AddonWeeklyBingoController.Dispose();
    }
}