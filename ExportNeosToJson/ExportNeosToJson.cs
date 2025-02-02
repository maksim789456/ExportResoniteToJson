using FrooxEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Elements.Core;
using Newtonsoft.Json;
using ResoniteModLoader;

namespace ExportNeosToJson
{
    public class ExportNeosToJson : ResoniteMod
    {
        public override string Name => "ExportNeosToJson";
        public override string Author => "runtime";
        public override string Version => "2.1.2";
        public override string Link => "https://github.com/zkxs/ExportNeosToJson";

        public override void OnEngineInit()
        {
            Harmony harmony = new Harmony("net.michaelripley.ExportNeosToJson");
            FieldInfo formatsField = AccessTools.DeclaredField(typeof(ModelExportable), "formats");
            if (formatsField == null)
            {
                Error("could not read ModelExportable.formats");
                return;
            }

            // inject addional formats
            List<string> modelFormats = new List<string>((string[])formatsField.GetValue(null));
            modelFormats.Add("JSON");
            modelFormats.Add("BSON");
            modelFormats.Add("7ZBSON");
            modelFormats.Add("LZ4BSON");
            modelFormats.Add("BRSON");
            formatsField.SetValue(null, modelFormats.ToArray());

            MethodInfo exportModelOriginal = AccessTools.DeclaredMethod(typeof(ModelExporter),
                nameof(ModelExporter.ExportModel), new Type[]
                {
                    typeof(Slot),
                    typeof(string),
                    typeof(Predicate<Component>)
                });
            if (exportModelOriginal == null)
            {
                Error("Could not find ModelExporter.ExportModel(Slot, string, Predicate<Component>)");
                return;
            }

            MethodInfo exportModelPrefix =
                AccessTools.DeclaredMethod(typeof(ExportNeosToJson), nameof(ExportModelPrefix));
            harmony.Patch(exportModelOriginal, prefix: new HarmonyMethod(exportModelPrefix));
            harmony.PatchAll();

            Msg("Hook installed successfully");
        }

        private static bool ExportModelPrefix(Slot slot, string targetFile, Predicate<Component> filter,
            ref Task<bool> __result)
        {
            string extension = Path.GetExtension(targetFile).Substring(1).ToUpper();
            SavedGraph graph;
            switch (extension)
            {
                case "JSON":
                    graph = slot.SaveObject(DependencyHandling.CollectAssets);
                    __result = ExportJson(graph, targetFile);
                    return false; // skip original function
                case "7ZBSON":
                    graph = slot.SaveObject(DependencyHandling.CollectAssets);
                    __result = Export7zbson(graph, targetFile);
                    return false; // skip original function
                case "LZ4BSON":
                    graph = slot.SaveObject(DependencyHandling.CollectAssets);
                    __result = ExportLz4bson(graph, targetFile);
                    return false; // skip original function
                case "BSON":
                    graph = slot.SaveObject(DependencyHandling.CollectAssets);
                    __result = ExportBson(graph, targetFile);
                    return false; // skip original function
                case "BRSON":
                    graph = slot.SaveObject(DependencyHandling.CollectAssets);
                    __result = ExportBrson(graph, targetFile);
                    return false; // skip original function
                default:
                    return true; // call original function
            }
        }

        private static async Task<bool> ExportJson(SavedGraph graph, string targetFile)
        {
            await new ToBackground();
            using (var fs = File.CreateText(targetFile))
            {
                var wr = new JsonTextWriter(fs);
                wr.Indentation = 2;
                wr.Formatting = Formatting.Indented;
                var writeFunc = AccessTools.Method(typeof(DataTreeConverter), "Write");
                writeFunc.Invoke(null, new object[] { graph.Root, wr });
            }

            Msg(string.Format("exported {0}", targetFile));
            return true;
        }

        private static async Task<bool> ExportBson(SavedGraph graph, string targetFile)
        {
            await new ToBackground();
            using (FileStream fileStream = File.OpenWrite(targetFile))
            {
                DataTreeConverter.ToRawBSON(graph.Root, fileStream);
            }

            Msg(string.Format("exported {0}", targetFile));
            return true; // call original function
        }

        private static async Task<bool> ExportLz4bson(SavedGraph graph, string targetFile)
        {
            await new ToBackground();
            using (FileStream fileStream = File.OpenWrite(targetFile))
            {
                DataTreeConverter.ToLZ4BSON(graph.Root, fileStream);
            }

            Msg(string.Format("exported {0}", targetFile));
            return true;
        }

        private static async Task<bool> Export7zbson(SavedGraph graph, string targetFile)
        {
            await new ToBackground();
            using (FileStream fileStream = File.OpenWrite(targetFile))
            {
                DataTreeConverter.To7zBSON(graph.Root, fileStream);
            }

            Msg(string.Format("exported {0}", targetFile));
            return true;
        }

        private static async Task<bool> ExportBrson(SavedGraph graph, string targetFile)
        {
            await new ToBackground();
            using (FileStream fileStream = File.OpenWrite(targetFile))
            {
                DataTreeConverter.ToBRSON(graph.Root, fileStream);
            }

            Msg(string.Format("exported {0}", targetFile));
            return true;
        }
    }
}