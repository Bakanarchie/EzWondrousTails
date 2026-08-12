using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Controllers;
using KamiToolKit.Enums;
using KamiToolKit.Extensions;
using KamiToolKit.Nodes;

namespace WondrousTailsSolver;

public unsafe class AddonWeeklyBingoController : IDisposable {
    private readonly AddonController<AddonWeeklyBingo> controller;
    private readonly IFramework framework;
    private TextNode? probabilityTextNode;

    public AddonWeeklyBingoController(IDalamudPluginInterface pluginInterface, IFramework framework) {
        this.framework = framework;
        KamiToolKitLibrary.Initialize(pluginInterface);

        controller = new AddonController<AddonWeeklyBingo> {
            AddonName = "WeeklyBingo",
            OnSetup = AttachNodes,
            OnRefresh = AddonRefresh,
            OnUpdate = AddonRefresh,
            OnFinalize = DetachNodes,
        };

        // Plugin construction happens off the main thread; KamiToolKit's controller
        // requires the framework thread, so hop over and wait for it to run.
        framework.Run(controller.Enable).Wait();
    }

    public void Dispose()
        => framework.Run(controller.Dispose).Wait();

    private void AttachNodes(AddonWeeklyBingo* addon) {
        var existingTextNode = addon->GetTextNodeById(34);
        if (existingTextNode is null) return;
        
        
        // Shrink existing node, the game doesn't need that space anyway.
        existingTextNode->SetHeight((ushort)(existingTextNode->GetHeight() * 2.0f / 3.0f));

        // Add new custom text node to ui
        probabilityTextNode = new TextNode {
            NodeFlags = NodeFlags.Enabled | NodeFlags.Visible,
            Size = new Vector2(existingTextNode->GetWidth(), existingTextNode->GetHeight()),
            Position = new Vector2(existingTextNode->GetXFloat(), existingTextNode->GetYFloat() + existingTextNode->GetHeight()),
            TextColor = existingTextNode->TextColor.ToVector4(),
            TextOutlineColor = existingTextNode->EdgeColor.ToVector4(),
            BackgroundColor = existingTextNode->BackgroundColor.ToVector4(),
            FontSize = existingTextNode->FontSize,
            LineSpacing = existingTextNode->LineSpacing,
            CharSpacing = existingTextNode->CharSpacing,
            TextFlags = TextFlags.MultiLine | (TextFlags)existingTextNode->TextFlags,
            String = System.PerfectTails.SolveAndGetProbabilitySeString().TextValue,
        };

        probabilityTextNode.AttachNode((AtkResNode*)existingTextNode, NodePosition.AfterTarget);
    }
    
    private void AddonRefresh(AddonWeeklyBingo* addon) {
        foreach (var index in Enumerable.Range(0, 16)) {
            System.PerfectTails.GameState[index] = PlayerState.Instance()->IsWeeklyBingoStickerPlaced(index);
        }

        if (probabilityTextNode is not null) {
            var existingTextNode = addon->GetTextNodeById(34);
            if (existingTextNode is null) return;
            var nodeText = SeString.Parse(existingTextNode->NodeText);

            var lineBreakIndex = -1;
            for (var index = 0; index < nodeText.Payloads.Count; index++)
            {
                if (index > 0)
                {
                    var previousPayload = nodeText.Payloads[index - 1];
                    var payload = nodeText.Payloads[index];

                    if (previousPayload.Type is PayloadType.NewLine && payload.Type is PayloadType.NewLine)
                    {
                        lineBreakIndex = index - 1;
                        break;
                    }
                }
            }

            if (lineBreakIndex is not -1)
            {
                var newString = new SeStringBuilder();

                for (var index = 0; index < lineBreakIndex; index++)
                {
                    newString.Add(nodeText.Payloads[index]);
                }
                existingTextNode->SetText(newString.Encode());
            }

            probabilityTextNode.String = System.PerfectTails.SolveAndGetProbabilitySeString().TextValue;
        }
    }
    
    private void DetachNodes(AddonWeeklyBingo* addon) {
        var existingTextNode = addon->GetTextNodeById(34);
        if (existingTextNode is not null) {
            existingTextNode->SetHeight((ushort)(existingTextNode->GetHeight() * 3.0f / 2.0f));
        }

        probabilityTextNode?.Dispose();
        probabilityTextNode = null;
    }
}