using System.Windows.Controls;
using Community.PowerToys.Run.Plugin.LaTeX_OCR.Properties;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Logger;

namespace Community.PowerToys.Run.Plugin.LaTeX_OCR
{
    public class Main : IPlugin, IPluginI18n, IContextMenu, ISettingProvider, IReloadable, IDisposable, IDelayedExecutionPlugin
    {
        private const string Setting = nameof(Setting);
        // current value of the setting
        private bool _setting;
        private PluginInitContext _context;
        private string _iconPath;
        private bool _disposed;
        public string Name => Resources.plugin_name;
        public string Description => Resources.plugin_description;
        public static string PluginID => "9cded42dc3c64e46b305be1db5da1261";

        
        public List<Result> Query(Query query)
        {
            string latexResult = string.Empty;
            try
            {
                // base paths
                string pluginDir = _pluginDirectory;
                string scriptPath = Path.Combine(pluginDir, "python", "run_ocr.py");

                // pick python exe by priority
                string pyFromVenv = Path.Combine(pluginDir, "python", "venv", "Scripts", "python.exe");
                string pyFromEnv = Environment.GetEnvironmentVariable("POWERRUN_LATEXOCR_PY");
                string pythonExe = null;

                if (File.Exists(pyFromVenv))
                    pythonExe = pyFromVenv;
                else if (!string.IsNullOrWhiteSpace(pyFromEnv) && File.Exists(pyFromEnv))
                    pythonExe = pyFromEnv;
                else
                    pythonExe = "py";

                // if "py" is not available, Process will fallback later; try "python" second
                if (pythonExe == "py")
                {
                    try
                    {
                        var test = new ProcessStartInfo { FileName = "py", Arguments = "--version", UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
                        using var ptest = Process.Start(test);
                        ptest.WaitForExit(1500);
                        if (ptest.ExitCode != 0) pythonExe = "python";
                    }
                    catch { pythonExe = "python"; }
                }

                var psi = new ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"\"{scriptPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    // ensure relative paths under python run from plugin folder
                    WorkingDirectory = Path.Combine(pluginDir, "python")
                };

                // route caches to plugin-local folders to avoid user-profile absolute deps
                string cacheDir = Path.Combine(pluginDir, "cache");
                Directory.CreateDirectory(cacheDir);
                psi.EnvironmentVariables["HF_HOME"] = Path.Combine(cacheDir, "hf");
                psi.EnvironmentVariables["TRANSFORMERS_CACHE"] = Path.Combine(cacheDir, "hf", "transformers");
                psi.EnvironmentVariables["TORCH_HOME"] = Path.Combine(cacheDir, "torch");
                psi.EnvironmentVariables["XDG_CACHE_HOME"] = Path.Combine(cacheDir, "xdg");

                using (var process = Process.Start(psi))
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    latexResult = string.IsNullOrEmpty(output) ? error : (output + error);
                }
            }
            catch (Exception e)
            {
                Log.Exception("Query execution failed", e, GetType());
                latexResult = "Error occurred. Check PowerToys log.";
            }

            if (string.IsNullOrWhiteSpace(latexResult))
            {
                latexResult = "[No result]";
            }

            var results = new List<Result>();
            results.Add(new Result
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
                    }
                    catch { return false; }
                    return true;
                },
            });
            return results;
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

        public void Init(PluginInitContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());
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
                _iconPath = "Images/LaTeX-OCR.light.png";
            }
            else
            {
                _iconPath = "Images/LaTeX-OCR.dark.png";
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
