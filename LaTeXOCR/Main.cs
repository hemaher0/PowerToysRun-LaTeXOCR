using System;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows;
using System.IO;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Community.PowerToys.Run.Plugin.LaTeXOCR.Properties;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.LaTeXOCR
{
    public class Main : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IReloadable, IDisposable, IDelayedExecutionPlugin
    {
        private string? _pluginDirectory;
        private PluginInitContext? _context;
        private string? _iconPath;
        private bool _disposed;
        private string _modelPath = "Not Initialized";

        private bool _useGpu;
        private const string UseGpuKey = "UseGpu";
        public string Name => Resources.plugin_name;
        public string Description => Resources.plugin_description;
        public static string PluginID => "9cded42dc3c64e46b305be1db5da1261";
        

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Textbox,
                Key = "ModelPathDisplay",
                DisplayLabel = "Model Storage Path",
                DisplayDescription = "The folder where AI models are downloaded and stored.",
                TextValue = GetModelPath(),
            },
            new PluginAdditionalOption()
            {
                PluginOptionType = PluginAdditionalOption.AdditionalOptionType.Checkbox,
                Key = UseGpuKey,
                DisplayLabel = "Use GPU Acceleration (NVIDIA CUDA)",
                DisplayDescription = "Requires a compatible NVIDIA GPU and CUDA setup. Restart PowerToys after changing.",
                Value = _useGpu,
            },
        };

        public void UpdateSettings(PowerLauncherPluginSettings settings)
        {
            _useGpu = settings?.AdditionalOptions?.FirstOrDefault(x => x.Key == UseGpuKey)?.Value ?? false;
        }

        public List<Result> Query(Query query)
        {
            string venvPythonPath = Path.Combine(_pluginDirectory, "python", "venv", "Scripts", "python.exe");

            bool packagesInstalled = File.Exists(venvPythonPath);
            bool modelsExist = ModelsExist();

            if (!packagesInstalled || !modelsExist)
            {
                string title = !packagesInstalled ? "Setup required for LaTeX OCR" : "Model download required";
                string subtitle = !packagesInstalled ? "Press Enter to install Python packages." : "Press Enter to download model files.";

                return new List<Result>
                {
                    new Result
                    {
                        Title = title,
                        SubTitle = subtitle,
                        IcoPath = _iconPath,
                        Action = _ =>
                        {
                            if (!packagesInstalled)
                            {
                                bool packageSuccess = RunScript("setup.bat", "Package setup");
                                if (!packageSuccess) return true;
                            }

                            if (!ModelsExist())
                            {
                                bool downloadSuccess = RunScript(Path.Combine("python", "download_model.py"), "Model download", true);
                                if (!downloadSuccess) return true;
                                MoveModelFiles();
                            }
                            
                            _context.API.ShowMsg("Setup Successful", "LaTeX OCR plugin is ready to use.", _iconPath);
                            return true;
                        }
                    }
                };
            }

            string latexResult = RunOcr(venvPythonPath);

            if (string.IsNullOrWhiteSpace(latexResult))
            {
                latexResult = "[No result]";
            }

            return new List<Result>
            {
                new Result
                {
                    Title = latexResult.Trim(),
                    SubTitle = "Press Enter to copy to clipboard",
                    IcoPath = _iconPath,
                    Action = _ =>
                    {
                        try
                        {
                            var staThread = new Thread(() => Clipboard.SetText(latexResult.Trim()));
                            staThread.SetApartmentState(ApartmentState.STA);
                            staThread.Start();
                            staThread.Join();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Log.Exception("Failed to copy result to clipboard", ex, GetType());
                            return false;
                        }
                    }
                }
            };
        }

        private bool ModelsExist()
        {
            return File.Exists(Path.Combine(GetModelPath(), "checkpoints", "weights.pth"));
        }

        private bool RunScript(string scriptName, string taskName, bool isPythonScript = false)
        {
            string scriptPath;
            ProcessStartInfo psi;

            if (isPythonScript)
            {
                string pythonExe = Path.Combine(_pluginDirectory, "python", "venv", "Scripts", "python.exe");
                scriptPath = Path.Combine(_pluginDirectory, scriptName);
                psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"\"{scriptPath}\"",
                    WorkingDirectory = Path.GetDirectoryName(scriptPath),
                    CreateNoWindow = false,
                };
            }
            else
            {
                scriptPath = Path.Combine(_pluginDirectory, "python", scriptName);
                psi = new ProcessStartInfo
                {
                    FileName = scriptPath,
                    WorkingDirectory = Path.GetDirectoryName(scriptPath),
                    CreateNoWindow = false,
                };
            }
            
            try
            {
                using (var process = Process.Start(psi))
                {
                    if (process == null)
                    {
                        _context.API.ShowMsg($"{taskName} Failed", $"Could not start process: {scriptName}", _iconPath);
                        return false;
                    }
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        _context.API.ShowMsg($"{taskName} Failed", $"Script exited with code {process.ExitCode}.", _iconPath);
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Exception($"Failed to run script {scriptName}", e, GetType());
                _context.API.ShowMsg($"{taskName} Error", e.Message, _iconPath);
                return false;
            }
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            var contextMenus = new List<ContextMenuResult>();
            contextMenus.Add(new ContextMenuResult
            {
                PluginName = this.GetType().Name,
                Title = "Open Model Path Folder",
                Glyph = "\xE8DE", // A folder icon, for example
                FontFamily = "Segoe Fluent Icons, Segoe MDL2 Assets",
                Action = _ =>
                {
                    OpenModelPath();
                    return true;
                }
            });
            
            return contextMenus;
        }

        private string RunOcr(string pythonExe)
        {
            string scriptPath = Path.Combine(_pluginDirectory, "python", "run_ocr.py");
            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\" \"{_modelPath}\" {_useGpu}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.Combine(_pluginDirectory, "python")
            };

            using var process = Process.Start(psi);

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder(); 
            process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit(30000);
            string output = outputBuilder.ToString();
            string error = errorBuilder.ToString();
            return string.IsNullOrWhiteSpace(output) ? error.Trim() : output.Trim();
        }

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            _pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _modelPath = GetModelPath();
            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        private void MoveModelFiles()
        {
            if (string.IsNullOrEmpty(_pluginDirectory)) return;

            string sourcePath = Path.Combine(_pluginDirectory, "python", "venv", "Lib", "site-packages", "pix2tex", "model");
            string destinationPath = GetModelPath();

            if (!Directory.Exists(sourcePath))
            {
                Log.Error($"Source model path not found: {sourcePath}", GetType());
                return;
            }

            Directory.CreateDirectory(destinationPath);
            
            string checkpointsSource = Path.Combine(sourcePath, "checkpoints");
            string checkpointsDest = Path.Combine(destinationPath, "checkpoints");
            if(Directory.Exists(checkpointsSource))
            {
                 CopyDirectory(checkpointsSource, checkpointsDest, true);
            }

            string settingsSource = Path.Combine(sourcePath, "settings");
            string settingsDest = Path.Combine(destinationPath, "settings");
            if(Directory.Exists(settingsSource))
            {
                CopyDirectory(settingsSource, settingsDest, true);
            }
            
            string tokenizerSource = Path.Combine(sourcePath, "dataset", "tokenizer.json");
            string tokenizerDest = Path.Combine(destinationPath, "tokenizer.json");
            if(File.Exists(tokenizerSource))
            {
                File.Copy(tokenizerSource, tokenizerDest, true);
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true); // true to overwrite
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        private string GetModelPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Community.PowerToys.Run.Plugin.LaTeXOCR", "models");
        }
        private void OpenModelPath()
        {
            string modelPath = GetModelPath();
            if (Directory.Exists(modelPath))
            {
                Process.Start("explorer.exe", modelPath);
            }
            else
            {
                _context.API.ShowMsg("Model Path Not Found", "Please run the setup first to download the models.", _iconPath);
            }
        }

        private bool RunSetupScript()
        {
            if (string.IsNullOrEmpty(_pluginDirectory))
            {
                Log.Error("Plugin directory is not initialized.", GetType());
                return false;
            }

            string setupScriptPath = Path.Combine(_pluginDirectory, "python", "setup.bat");
            if (!File.Exists(setupScriptPath))
            {
                Log.Error($"Setup script not found at {setupScriptPath}", GetType());
                return false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = setupScriptPath,
                WorkingDirectory = Path.GetDirectoryName(setupScriptPath),
                CreateNoWindow = false,
            };
            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    Log.Error("Failed to start setup process.", GetType());
                    return false;
                }

                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }

        // TODO: return delayed query results (optional)
        public List<Result> Query(Query query, bool delayedExecution)
        {
            ArgumentNullException.ThrowIfNull(query);

            var results = new List<Result>();

            // empty query
            if (string.IsNullOrEmpty(query.Search))
            {
                return results;
            }

            return results;
        }

        public string GetTranslatedPluginTitle()
        {
            return Resources.plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Resources.plugin_description;
        }

        private void OnThemeChanged(Theme oldTheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                _iconPath = "Images/LaTeXOCR.light.png";
            }
            else
            {
                _iconPath = "Images/LaTeXOCR.dark.png";
            }
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void ReloadData()
        {
            if (_context is null)
            {
                return;
            }

            UpdateIconPath(_context.API.GetCurrentTheme());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_context != null && _context.API != null)
                {
                    _context.API.ThemeChanged -= OnThemeChanged;
                }

                _disposed = true;
            }
        }
    }
}
