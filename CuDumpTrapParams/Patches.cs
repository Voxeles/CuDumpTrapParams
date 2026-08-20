using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace CuDumpTrapParams;

[HarmonyPatch(typeof(WorldGeneration), nameof(WorldGeneration.DistributeEntities))]
internal static class LogDistributeEntitiesPatch
{
    private static string _name;
    private static float _amountWanted;
    private static int _amountPlaced;
    
    [HarmonyPriority(Priority.VeryLow)]
    private static void Prefix(GameObject basObj)
    {
        _name = basObj.name;
        _amountWanted = 0f;
        _amountPlaced = 0;
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Stloc_0))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc.0)!")
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stsfld,
                    AccessTools.Field(typeof(LogDistributeEntitiesPatch), nameof(_amountWanted))))
            .MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Stloc_S && instr.operand is LocalBuilder { LocalIndex: 4 }))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc gameObject)!")
            .Advance(1)
            .Insert(
                new CodeInstruction(OpCodes.Ldsfld,
                    AccessTools.Field(typeof(LogDistributeEntitiesPatch), nameof(_amountPlaced))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stsfld,
                    AccessTools.Field(typeof(LogDistributeEntitiesPatch), nameof(_amountPlaced))))
            .InstructionEnumeration();
    }

    private static void Postfix()
    {
        Plugin.PrintOut($"- Distributed \"{_name}\" (Wanted {Mathf.CeilToInt(_amountWanted)}, placed {_amountPlaced})");
    }
}

[HarmonyPatch]
internal static class LogWorldGenParamsPatch
{
    [HarmonyPatch(typeof(WorldGeneration), nameof(WorldGeneration.WorldPlaceEntities))]
    [HarmonyPriority(Priority.VeryLow)]
    private static void Prefix()
    {
        var world = WorldGeneration.world;
        Plugin.PrintOut($"=== WORLD GENERATION PLACING ENTITIES INFO ===");
        Plugin.PrintOut($"Loot info:");
        Plugin.PrintOut($"- baseLootDensity: {WorldGeneration.GetRunSettingFloat("baselootdensity")}");
        Plugin.PrintOut($"- lootMultiplier: {WorldGeneration.GetRunSettingFloat("lootmultiplier")}");
        Plugin.PrintOut($"- lootRarityMultiplier: {world.lootRarityMultiplier}");
        Plugin.PrintOut($"- totalLootRarity: {world.totalLootRarity}");
        Plugin.PrintOut($"Trap info:");
        Plugin.PrintOut($"- baseTrapDensity: {WorldGeneration.GetRunSettingFloat("basetrapdensity")}");
        Plugin.PrintOut($"- trapIncrease: {WorldGeneration.GetRunSettingFloat("trapincrease")}");
        Plugin.PrintOut($"- trapRarityMultiplier: {world.trapRarityMultiplier}");
        Plugin.PrintOut($"- totalTrapRarity: {world.totalTrapRarity}");
        Plugin.PrintOut($"Placing Entities:");
        SaveExtraDistPatch.Reset();
    }

    [HarmonyPatch(typeof(WorldGeneration), nameof(WorldGeneration.FinishWorldGeneration))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.VeryLow)]
    private static void PrintExtra() => SaveExtraDistPatch.Print(WorldGeneration.world.biomeDepth);
}

[HarmonyPatch]
internal static class SaveExtraDistPatch
{
    // biomeDepth 0 & 1
    public static float BandagesWanted;
    public static int BandagesPlaced;
    public static float ClimbingRopesWanted;
    public static int ClimbingRopesPlaced;
    public static float DroppingsWanted;
    public static int DroppingsPlaced;
    // biomeDepth 0
    public static float FleshChunksWanted;
    public static int FleshChunksPlaced;
    // biomeDepth 2 & 3
    public static float OilPipesWanted;
    public static int OilPipesPlaced;
    public static float TurretsWanted;
    public static int TurretsPlaced;
    public static float StalactitesWanted;
    public static int StalactitesPlaced;
    // biomeDepth 4
    public static int SaladsPlaced;
    public static float WallFlowersWanted;
    public static int WallFlowersPlaced;

    public static void Reset()
    {
        BandagesWanted = 0;
        BandagesPlaced = 0;
        ClimbingRopesWanted = 0;
        ClimbingRopesPlaced = 0;
        DroppingsWanted = 0;
        DroppingsPlaced = 0;
        FleshChunksWanted = 0;
        FleshChunksPlaced = 0;
        OilPipesWanted = 0;
        OilPipesPlaced = 0;
        TurretsWanted = 0;
        TurretsPlaced = 0;
        StalactitesWanted = 0;
        StalactitesPlaced = 0;
        SaladsPlaced = 0;
        WallFlowersWanted = 0;
        WallFlowersPlaced = 0;
    }

    public static void Print(int biomeDepth)
    {
        switch (biomeDepth)
        {
            case 0:
                Plugin.PrintOut($"- Distributed \"fleshchunk\" (Wanted {Mathf.CeilToInt(FleshChunksWanted)}, placed {FleshChunksPlaced})");
                goto case 1;
            case 1: 
                Plugin.PrintOut($"- Distributed \"bandage\" (Wanted {Mathf.CeilToInt(BandagesWanted)}, placed {BandagesPlaced})");
                Plugin.PrintOut($"- Distributed \"climbingropeextended\" (Wanted {Mathf.CeilToInt(ClimbingRopesWanted)}, placed {ClimbingRopesPlaced})");
                Plugin.PrintOut($"- Distributed \"droppings\" (Wanted {Mathf.CeilToInt(DroppingsWanted)}, placed {DroppingsPlaced})");
                break;
            case 2 or 3:
                Plugin.PrintOut($"- Distributed \"oilpipe\" (Wanted {Mathf.CeilToInt(OilPipesWanted)}, placed {OilPipesPlaced})");
                Plugin.PrintOut($"- Distributed \"turret\" (Wanted {Mathf.CeilToInt(TurretsWanted)}, placed {TurretsPlaced})");
                Plugin.PrintOut($"- Distributed \"stalactite\" (Wanted {Mathf.CeilToInt(StalactitesWanted)}, placed {StalactitesPlaced})");
                break;
            case 4:
                Plugin.PrintOut($"- Distributed \"thornbackelder\" (Placed {SaladsPlaced})");
                Plugin.PrintOut($"- Distributed \"wallflower\" (Wanted {Mathf.CeilToInt(WallFlowersWanted)}, placed {WallFlowersPlaced})");
                break;
            default:
                break;
        }

        return;
    }
    
    
    private static MethodBase TargetMethod()
    {
        var target = AccessTools.FirstInner(typeof(WorldGeneration), t => t.Name.Contains("<WorldPlaceEntities>d__"));
        return AccessTools.Method(target, "MoveNext");
    }

    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codeMatcher = new CodeMatcher(instructions);

        codeMatcher
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldc_R4, 0.0f),
                new CodeMatch(OpCodes.Stloc_2))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc.2 init)!")
            .Advance(2);
        
        codeMatcher
            .MatchForward(false, 
                new CodeMatch(OpCodes.Stloc_2))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc.2)!")
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(BandagesWanted))))
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "bandage"))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (ldstr bandage)!")
            .Insert(
                new CodeInstruction(OpCodes.Ldsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(BandagesPlaced))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(BandagesPlaced))));
        
        codeMatcher
            .MatchForward(false, 
                new CodeMatch(OpCodes.Stloc_2))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc.2)!")
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(ClimbingRopesWanted))))
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "climbingropeextended"))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (ldstr climbingropeextended)!")
            .Insert(
                new CodeInstruction(OpCodes.Ldsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(ClimbingRopesPlaced))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(ClimbingRopesPlaced))));
        
        codeMatcher
            .MatchForward(false, 
                new CodeMatch(OpCodes.Stloc_2))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc.2)!")
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(DroppingsWanted))))
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "droppings"))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (ldstr droppings)!")
            .Insert(
                new CodeInstruction(OpCodes.Ldsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(DroppingsPlaced))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(DroppingsPlaced))));
        
        codeMatcher
            .MatchForward(false, 
                new CodeMatch(OpCodes.Stloc_2))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc.2)!")
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(FleshChunksWanted))))
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "fleshchunk"))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (ldstr fleshchunk)!")
            .Insert(
                new CodeInstruction(OpCodes.Ldsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(FleshChunksPlaced))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(FleshChunksPlaced))));

        codeMatcher
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldc_R4, 0.0f),
                new CodeMatch(instr => instr.opcode == OpCodes.Stloc_S && instr.operand is LocalBuilder { LocalIndex: 17 } ))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc 17 init)!")
            .Advance(1);
        
        codeMatcher
            .MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Stloc_S && instr.operand is LocalBuilder { LocalIndex: 17 } ))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc 17)!")
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(OilPipesWanted))))
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "oilpipe"))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (ldstr oilpipe)!")
            .Insert(
                new CodeInstruction(OpCodes.Ldsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(OilPipesPlaced))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(OilPipesPlaced))));
        
        codeMatcher
            .MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Stloc_S && instr.operand is LocalBuilder { LocalIndex: 17 }))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc 17)!")
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(TurretsWanted))))
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "turret"))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (ldstr turret)!")
            .Insert(
                new CodeInstruction(OpCodes.Ldsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(TurretsPlaced))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(TurretsPlaced))));
        
        codeMatcher
            .MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Stloc_S && instr.operand is LocalBuilder { LocalIndex: 17 }))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc 17)!")
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(StalactitesWanted))))
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "stalactite"))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (ldstr stalactite)!")
            .Insert(
                new CodeInstruction(OpCodes.Ldsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(StalactitesPlaced))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(StalactitesPlaced))));
        
        codeMatcher
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "thornbackelder"))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (ldstr thornbackelder)!")
            .Advance(1)
            .Insert(
                new CodeInstruction(OpCodes.Ldsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(SaladsPlaced))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(SaladsPlaced))));
        
        codeMatcher
            .MatchForward(false,
                new CodeMatch(instr => instr.opcode == OpCodes.Stloc_S && instr.operand is LocalBuilder { LocalIndex: 38 }))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (stloc 38)!")
            .Insert(
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(WallFlowersWanted))))
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldstr, "wallflower"))
            .ThrowIfInvalid(
                $"{typeof(LogDistributeEntitiesPatch)}.{nameof(Transpiler)} could not find a match (ldstr wallflower)!")
            .Insert(
                new CodeInstruction(OpCodes.Ldsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(WallFlowersPlaced))),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Add),
                new CodeInstruction(OpCodes.Stsfld, 
                    AccessTools.Field(typeof(SaveExtraDistPatch), nameof(WallFlowersPlaced))));
        
        return codeMatcher.InstructionEnumeration();
    }
}
