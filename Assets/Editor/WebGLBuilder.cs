using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuilder
{
    [MenuItem("Juego/🚀 Construir WebGL para itch.io (1 Clic)")]
    public static void ConstruirWebGLParaItch()
    {
        AssetDatabase.Refresh();
        string projectPath = Directory.GetCurrentDirectory();
        string buildWebDir = Path.Combine(projectPath, "BuildWeb");
        string zipPath = Path.Combine(projectPath, "BuildWeb.zip");
        string downloadsZip = @"C:\Users\ADMIN\Downloads\juego.zip";

        // Desactivar Burst para evitar el error de AOT Linker en WebGL
        EditorPrefs.SetBool("BurstDisableSafetyChecks", true);
        EditorPrefs.SetBool("BurstEnableCompilation", false);

        // 1. Configurar compresión en Disabled según guía de itch.io
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 600;
        PlayerSettings.runInBackground = true;

        // 2. Escenas incluidas: Menú Principal, Nivel 1, Nivel 2 y Nivel 3 (si existe)
        List<string> sceneList = new List<string>
        {
            "Assets/Scenes/MenuPrincipal.unity",
            "Assets/Scenes/Nivel1.unity",
            "Assets/Scenes/Nivel 2.unity"
        };

        if (File.Exists("Assets/Scenes/Nivel 3.unity"))
        {
            sceneList.Add("Assets/Scenes/Nivel 3.unity");
        }

        string[] scenes = sceneList.ToArray();

        foreach (string scene in scenes)
        {
            if (!File.Exists(scene))
            {
                Debug.LogError($"[WebGLBuilder] No se encontró la escena: {scene}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Error de Escenas", $"No se encontró la escena: {scene}", "Aceptar");
                }
                return;
            }
        }

        if (Directory.Exists(buildWebDir))
        {
            try { Directory.Delete(buildWebDir, true); } catch { }
        }
        Directory.CreateDirectory(buildWebDir);

        if (File.Exists(zipPath))
        {
            try { File.Delete(zipPath); } catch { }
        }

        Debug.Log($"<color=cyan>[WebGLBuilder] Iniciando compilación de WebGL ({scenes.Length} escenas)...</color>");

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildWebDir,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"<color=green>[WebGLBuilder] Build WebGL completado con éxito ({summary.totalSize / 1024 / 1024} MB). Empaquetando ZIP...</color>");

            EmpaquetarZip(buildWebDir, zipPath);

            try
            {
                File.Copy(zipPath, downloadsZip, true);
                Debug.Log($"<color=green>[WebGLBuilder] Copiado juego.zip actualizado a: {downloadsZip}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[WebGLBuilder] Aviso al copiar a Descargas: " + ex.Message);
            }

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("¡Build WebGL Listo!", 
                    $"El juego se ha compilado y empaquetado correctamente en:\n\n{zipPath}\n\nListo para subir a itch.io.", "Abrir Carpeta");
                EditorUtility.RevealInFinder(zipPath);
            }
        }
        else
        {
            Debug.LogError($"<color=red>[WebGLBuilder] Error al compilar: {summary.result}. Revisa la consola.</color>");
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Error en Build", "Hubo un error durante la compilación. Revisa la consola de Unity.", "Aceptar");
            }
        }
    }

    private static void EmpaquetarZip(string sourceDir, string zipPath)
    {
        if (File.Exists(zipPath)) File.Delete(zipPath);

        using (ZipArchive archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            foreach (FileInfo file in dir.GetFiles("*", SearchOption.AllDirectories))
            {
                string relPath = file.FullName.Substring(dir.FullName.Length + 1).Replace('\\', '/');
                archive.CreateEntryFromFile(file.FullName, relPath, System.IO.Compression.CompressionLevel.Optimal);
            }
        }

        Debug.Log($"<color=green>[WebGLBuilder] Archivo ZIP creado con éxito en: {zipPath}</color>");
    }
}
