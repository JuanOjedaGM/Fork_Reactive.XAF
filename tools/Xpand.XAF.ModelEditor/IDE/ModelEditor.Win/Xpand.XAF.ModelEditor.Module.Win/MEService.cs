﻿using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using akarnokd.reactive_extensions;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Base;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.BytesExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System.Diagnostics;
using Xpand.Extensions.StringExtensions;
using Xpand.XAF.ModelEditor.Module.Win.BusinessObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Controllers;
using Unit = System.Reactive.Unit;
using Window = DevExpress.ExpressApp.Window;

namespace Xpand.XAF.ModelEditor.Module.Win {
    public static class MEService {
        static readonly HttpClient HttpClient = new();

        static MEService() => HttpClient.DefaultRequestHeaders.Add("User-Agent",nameof(MEService));

        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => Observable.Defer(() => application.DeleteMESettings(true).ShowModelListView().RunME().ToUnit()
                    .Merge(application.CloseViewWhenNotSettings(MeInstallationPath))))
                .Merge(manager.WhenExtendingModel().Do(extenders => extenders.Add<IModelApplication,IModelApplicationME>()).ToUnit());

        private static IObservable<XafApplication> RunME(this IObservable<XafApplication> source)  
            => source.MergeIgnored(application => application.WhenViewOnFrame().WhenFrame(ViewType.ListView)
                .SelectMany(frame => frame.GetController<ListViewProcessCurrentObjectController>()
                    .WhenCustomProcessSelectedItem(true)
                    .Do(_ => {
                        _.controller.View.ObjectSpace.CommitChanges();
                        ((Window) frame).Close();
                    })
                    .Select(t => t.e.InnerArgs.SelectedObjects.Cast<XafModel>().First())
                    .Do(model => {
                        if (model.Project.CannotRunMEMessage != null) {
                            throw new CannotRunMEException(model.Project.CannotRunMEMessage);
                        }
                    })
                    .SelectMany(xafModel => xafModel.DownloadME(((IModelApplicationME)application.Model).ModelEditor).StartMEProcess(xafModel))));

        internal static XafApplication DeleteMESettings(this XafApplication application,bool setup=false) {
            var path = GetMESettingsPath();
            if (File.Exists(path)) {
                File.Delete(path);
            }

            if (setup) {
                if (File.Exists(GetReadyPath())) {
                    File.Delete(GetReadyPath());
                }
            }
            return application;
        }

        internal static IObservable<string> WhenMESettings() 
            => Observable.Using(() => new FileSystemWatcher(MeInstallationPath), watcher => {
                    watcher.EnableRaisingEvents = true;
                    var meSettingsPath = GetMESettingsPath();
                    return watcher.WhenEvent<FileSystemWatcher,FileSystemEventArgs>(nameof(watcher.Created)).Where(t => t.args.FullPath == meSettingsPath)
                        .SelectMany(_ => Observable.Defer(() => $"{JsonConvert.DeserializeObject<dynamic>(File.ReadAllText(meSettingsPath))?.Solution}".ReturnObservable()).Retry())
                        .Merge("".ReturnObservable().Do(_ => File.CreateText(GetReadyPath())).IgnoreElements());
                })
                .ObserveOn(SynchronizationContext.Current!)
                .TraceModelEditorWindowsFormsModule();

        internal static string GetReadyPath() => $"{AppDomain.CurrentDomain.ApplicationPath()}\\Ready.txt";
        internal static string GetMESettingsPath() => $"{MeInstallationPath}\\MESettings.json";

        private static IObservable<string> DownloadME(this XafModel xafModel, IModelME modelME) {
            var assemblyPath = xafModel.Project.AssemblyPath;
            if (!File.Exists(assemblyPath)) {
                throw new UserFriendlyException($"File {assemblyPath} not found, please build the project");
            }

            var versionsGroup = Directory.GetFiles(Path.GetDirectoryName(assemblyPath)!,"DevExpress*.dll")
                .GroupBy(s => Version.Parse(FileVersionInfo.GetVersionInfo(s).FileVersion!)).ToArray();
            if (versionsGroup.Length > 1) {
                throw new UserFriendlyException($"Multiple DevExpress versions found in {assemblyPath}");
            }
            if (!versionsGroup.Any()) {
                throw new UserFriendlyException($"Cannot find any DevExpress assembly in {assemblyPath}");
            }

            return versionsGroup.First().Key.RXXafReleaseVersion(modelME.DownloadPreRelease, modelME.NoFoundVersion())
                .SelectMany(version => {
                    var meType = "WinDesktop";
                    if (xafModel.Project.TargetFramework.StartsWith("net4")) {
                        meType = "Win";
                    }

                    var directory = $"{MeInstallationPath}\\{meType}\\{version}\\";
                    string filename=$"Xpand.XAF.ModelEditor.{meType}.exe";
                    var path = $"{directory}{filename}";
                    if (!File.Exists(path)) {
                        if (!Directory.Exists(directory)) {
                            Directory.CreateDirectory(directory);
                        }
                        return modelME.DownloadUrl.StringFormat($"{version}",meType).ReturnObservable()
                                .TraceModelEditorWindowsFormsModule(s => $"Download {s}")
                            .SelectMany(url => MEBytes(url)
                                    .ObserveOn(SynchronizationContext.Current!)
                                    .Do(bytes => {
                                        var pathToZip = $"{directory}Xpand.XAF.ModelEditor.{meType}.zip";
                                        bytes.Save(pathToZip);
                                        using var archive = ZipFile.OpenRead(pathToZip);
                                        archive.ExtractToDirectory(directory);
                                    }).To(path));
                    }

                    return path.ReturnObservable();
                });
        }

        private static IObservable<byte[]> MEBytes(string url) 
            => File.Exists(url) ? File.ReadAllBytes(url).ReturnObservable() : HttpClient.GetByteArrayAsync(url).ToObservable();

        private static Func<Version, IObservable<Version>> NoFoundVersion(this IModelME modelME) 
            => dxVersion =>modelME.DevVersion==null? Observable.Throw<Version>(new VersionNotFoundException(
                $"Version {dxVersion} not on GitHub. Consider the ${nameof(IModelME.DownloadPreRelease)}, or the ({nameof(IModelME.DevVersion)}, {nameof(IModelME.DownloadUrl)}) model attributes")):modelME.DevVersion.ReturnObservable();

        private static IObservable<Version> RXXafReleaseVersion(this Version dxVersion, bool preRelease, Func<Version,IObservable<Version>> noFound) 
            => HttpClient.GetStringAsync("https://api.github.com/repos/expandframework/reactive.xaf/tags").ToObservable()
                .SelectMany(s => JsonConvert.DeserializeObject<JArray>(s)!.Select(token => Version.Parse($"{token["name"]}")))
                .FirstAsync(version => (version.Revision == 0 && !preRelease)||preRelease)
                .Where(version => $"{version.Minor}" == $"{dxVersion.Major}{dxVersion.Minor}")
                .SwitchIfEmpty(noFound(dxVersion))
                .FirstAsync()
                .ObserveOn(SynchronizationContext.Current!)
                .TraceModelEditorWindowsFormsModule(version => $"Locating {dxVersion} release = {version}");

        public static string MeInstallationPath =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\\Xpand.XAF.ModelEditor.Win";

        static IObservable<Unit> StartMEProcess(this IObservable<string> source, XafModel xafModel) 
            => source.SelectMany(mePath => {
	            var assemblyDir = Path.GetDirectoryName(xafModel.Project.AssemblyPath);
	            var destFileName = $"{assemblyDir}\\{Path.GetFileName(mePath)}";
                KillProcess(destFileName);
                return Directory.GetFiles(Path.GetDirectoryName(mePath)!,"*.*").Do(s => File.Copy(s,$"{assemblyDir}\\{Path.GetFileName(s)}",true)).ToArray()
	                .ToObservable().ToUnit().Concat(xafModel.StartME(xafModel.Project.Path, destFileName));
            }).ToUnit();

        private static IObservable<Unit> StartME(this XafModel xafModel, string fullPath, string destFileName) {
            string debugMe = xafModel.Debug ? "d" : null;
            string arguments = String.Format("{0} {4} \"{1}\" \"{3}\" \"{2}\"", debugMe, xafModel.Project.AssemblyPath,
                fullPath, xafModel.Path, xafModel.Project.IsApplicationProject);
            var process = new Process() {
	            StartInfo = new ProcessStartInfo(destFileName,arguments) {
                    WorkingDirectory = Path.GetDirectoryName(destFileName)!
	            }
            };
            Tracing.Tracer.LogSeparator("Xpand Model Editor");
            Tracing.Tracer.LogValue("MEPath",destFileName);
            Tracing.Tracer.LogValue("MEArgs",arguments);
            process.StartWithEvents();
            return process.WhenErrorDataReceived().WhenNotDefault()
	            .Do(s => throw new Exception(s)).ToUnit();
        }

        static void KillProcess(string path){
            const string wmiQueryString = "SELECT ProcessId, ExecutablePath, CommandLine FROM Win32_Process";
            using var searcher = new ManagementObjectSearcher(wmiQueryString);
            using var results = searcher.Get();
            var query = Process.GetProcesses()
                .Join(results.Cast<ManagementObject>(), p => p.Id, mo => (int) (uint) mo["ProcessId"],
                    (p, mo) => new{
                        Process = p,
                        Path = (string) mo["ExecutablePath"],
                        CommandLine = (string) mo["CommandLine"],
                    });
            foreach (var item in query.Where(arg => arg.Path==path)) {
                item.Process.Kill();
            }
        }
    }

    internal class CannotRunMEException : UserFriendlyException {
        public CannotRunMEException(string message):base(message) { }
    }
}