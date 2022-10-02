using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.UIElements;


namespace LonelyVertex.Importers
{
    [ScriptedImporter(1, new[] {"scss", "sass"})]
    public class SassImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var path = Path.GetDirectoryName(ctx.assetPath);
            if (path == null)
            {
                return;
            }

            // Try to resolve dependencies and bail if we can't
            if (!ResolveDependencies(ctx, path))
            {
                ImportAsTextFile(ctx);
                
                return;
            }

            // Don't import the rest if the file starts with '_' (sass practice for imports)
            if (Path.GetFileName(ctx.assetPath).StartsWith("_"))
            {
                ImportAsTextFile(ctx);
                
                return;
            }

            var tempPath = Path.GetTempFileName();
            try
            {
                RunSassCompiler(ctx, tempPath);

                var compiledStylesheet = File.ReadAllText(tempPath);

                var instance = ScriptableObject.CreateInstance<StyleSheet>();
                instance.hideFlags = HideFlags.NotEditable;

                try
                {
                    RunStyleSheetImporter(ctx, instance, compiledStylesheet);

                    ctx.AddObjectToAsset("stylesheet", instance);
                    ctx.SetMainObject(instance);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError(e);
                }
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        private static bool ResolveDependencies(AssetImportContext ctx, string path)
        {
            // Parse all imports
            var imports = new HashSet<string>();
            // Only supporting imports encapsulated in '
            var regex = new Regex(@"'([^']*)'", RegexOptions.Singleline);

            using (var file = File.OpenText(ctx.assetPath))
            {
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine();
                    if (line == null || !line.StartsWith("@import"))
                    {
                        continue;
                    }

                    var importFilename = regex.Match(line).Groups[1].Value;

                    var resolvedImportPath = ResolveImportPath(path, importFilename);
                    if (string.IsNullOrEmpty(resolvedImportPath))
                    {
                        return false;
                    }
                    imports.Add(resolvedImportPath);
                }
            }

            foreach (var import in imports)
            {
                ctx.DependsOnSourceAsset(import);
            }

            return true;
        }

        private static string ResolveImportPath(string basePath, string filename)
        {
            // Early exit if we have full name with extension and underscore
            var path = Path.Combine(basePath, filename);
            if (File.Exists(path))
            {
                return path;
            }

            // Add .scss and .sass extension if filename doesn't have extension already
            var potentialNames = new List<string>();

            var extension = Path.GetExtension(filename);
            if (string.IsNullOrEmpty(extension))
            {
                potentialNames.Add($"{filename}.scss");
                potentialNames.Add($"{filename}.sass");
            }
            else
            {
                potentialNames.Add(filename);
            }

            // Check potential names with _ and without, checking _ first as there is bigger chance of that import 
            foreach (var name in potentialNames)
            {
                path = Path.Combine(basePath, $"_{name}");
                if (File.Exists(path))
                {
                    return path;
                }

                path = Path.Combine(basePath, name);
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        private static void RunSassCompiler(AssetImportContext ctx, string tempPath)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = "sass",
                Arguments = $"--style expanded --no-source-map {ctx.assetPath} {tempPath}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var process = new Process()
            {
                StartInfo = startInfo
            };
            process.Start();

            // Output is ignored
            process.StandardOutput.ReadToEnd();

            process.WaitForExit();
        }

        private static void RunStyleSheetImporter(AssetImportContext ctx, StyleSheet instance, string compiledStylesheet)
        {
            // We have to resort to using Reflection to get StyleSheetImporterImpl currently. Looking for a better way
            var type = Type.GetType(
                "UnityEditor.UIElements.StyleSheets.StyleSheetImporterImpl, UnityEditor.UIElementsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            if (string.IsNullOrEmpty(compiledStylesheet) || type == null)
            {
                return;
            }

            var sheetImporterImpl = Activator.CreateInstance(type, ctx);

            var method = type.GetMethod("Import", BindingFlags.Instance | BindingFlags.Public);
            if (method != null)
            {
                method.Invoke(sheetImporterImpl, new object[] {instance, compiledStylesheet});
            }
        }

        private static void ImportAsTextFile(AssetImportContext ctx)
        {
            var textAsset = new TextAsset(File.ReadAllText(ctx.assetPath));

            ctx.AddObjectToAsset("text", textAsset);
            ctx.SetMainObject(textAsset);
        }
    }
}
