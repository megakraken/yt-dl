using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace yt_dl {
    class Program {
        // GitHub API rejects requests without a user-agent header...
        const string userAgent =
            "Mozilla/5.0 (Windows NT 6.1; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.117 Safari/537.36";

        const string ytdl = "yt-dlp.exe";

        static string GetYtDlExe() {
            return Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                ytdl
            );
        }

        static dynamic GetGitHubRelease() {
            // Use .NET Json stuff so that we don't have to add a dependency to an external dll.
            const string releaseUrl =
                "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest";
            using (var wc = new WebClient()) {
                wc.Headers["user-agent"] = userAgent;
                return new JavaScriptSerializer().DeserializeObject(
                    wc.DownloadString(releaseUrl)
                );
            }
        }

        static void UpdateYoutubeDl() {
            try {
                var path = GetYtDlExe();
                var version = File.Exists(path) ?
                    FileVersionInfo.GetVersionInfo(path).FileVersion : null;
                var release = GetGitHubRelease();
                var tag = release["tag_name"].ToString();
                if (tag != version) {
                    Console.WriteLine($"Downloading new yt-dlp version {tag} ...");
                    dynamic[] assets = release["assets"];
                    var exe = assets.FirstOrDefault(a => a["name"].ToString() == ytdl);
                    if (exe == null)
                        throw new Exception("Couldn't find .exe asset in JSON manifest.");
                    using(var wc = new WebClient())
                        wc.DownloadFile(exe["browser_download_url"].ToString(), path);
                }
            } catch (Exception e) {
                // Use a message box instead of writing to console, so that when the program is
                // invoked in the background by some GUI application such as MPC-HC the user will
                // at least be told that something went wrong.
                MessageBox.Show(e.ToString(), "Error updating yt-dlp", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        static void RunYoutubeDl(string[] args) {
            var openDir = false;
            var @params = new List<string>() {
                "--no-check-certificates",
                "--no-mtime"
            };
            foreach(var arg in args) {
                if (arg == "--mp3") {
                    @params.Add("--extract-audio");
                    @params.Add("--embed-thumbnail");
                    @params.Add("--audio-format mp3");
                } else if (arg == "--mp4") {
                    @params.Add("--recode-video mp4");
                } else if (arg == "--open") {
                    openDir = true;
                } else if (arg == "--no-ffmpeg" || arg == "--no-add-path") {
                    // just strip this off.
                } else {
                    @params.Add(arg);
                }
            }
            // If no URL given, try to read it from clipboard.
            if (args.All(arg => arg.StartsWith("-"))) {
                var text = Clipboard.GetText();
                if (text.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                    @params.Add(text);
            }
            var proc = new Process();
            proc.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            proc.StartInfo.FileName = GetYtDlExe();
            proc.StartInfo.Arguments = string.Join(" ", @params);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.OutputDataReceived += (sender, __args) => {
                Console.WriteLine(__args.Data);
            };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            if (openDir)
                Process.Start(Directory.GetCurrentDirectory());
            Environment.ExitCode = proc.ExitCode;
        }

        static void EnsureFFmpeg() {
            var path = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "ffmpeg.exe"
            );
            // don't do anything if we already have it.
            if (File.Exists(path))
                return;
            Console.WriteLine("Downloading ffmpeg...this may take a moment");
            const string releaseUrl =
                "https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest";
            using (var wc = new WebClient()) {
                wc.Headers["user-agent"] = userAgent;
                dynamic blob = new JavaScriptSerializer().DeserializeObject(
                    wc.DownloadString(releaseUrl)
                );
                // Asset we want is named "ffmpeg-N-*-win64-gpl.zip"
                dynamic[] assets = blob["assets"];
                var file = assets.FirstOrDefault(a => Regex.IsMatch(a["name"],
                    @"ffmpeg-N-.+-win64-gpl\.zip"));
                if (file == null)
                    throw new Exception("Couldn't find asset in JSON manifest.");
                using (Stream s = wc.OpenRead(file["browser_download_url"])) {
                    using (var zip = new ZipArchive(s)) {
                        var exe = zip.Entries.FirstOrDefault(e => e.Name == "ffmpeg.exe");
                        if (exe == null)
                            throw new Exception("Couldn't find ffmpeg.exe in archive.");
                        exe.ExtractToFile(path);
                    }
                }
            }
        }

        // Add executing directory to user's path so you can just invoke yt-dl from
        // everywhere.
        static void AddToPath() {
            var u = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
            var p = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (u.Contains(p))
                return;
            var sb = new StringBuilder(u);
            if (!u.EndsWith(";"))
                sb.Append(";");
            sb.Append(p);
            Environment.SetEnvironmentVariable("PATH", sb.ToString(),
                EnvironmentVariableTarget.User);
        }

        [STAThread]
        static void Main(string[] args) {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            UpdateYoutubeDl();
            if (!args.Contains("--no-ffmpeg"))
                EnsureFFmpeg();
            if (!args.Contains("--no-add-path"))
                AddToPath();
            RunYoutubeDl(args);
        }
    }
}
