﻿using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using Microsoft.CognitiveServices.Speech;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.Attributes;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.Xpo.BaseObjects;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Reactive.Services.Actions;
using Xpand.XAF.Modules.Speech.BusinessObjects;

namespace Xpand.XAF.Modules.Speech.Services {
    public static class AccountService {
        public static IObservable<Unit> ConnectAccount(this ApplicationModulesManager manager) 
            => manager.UpdateVoices();

        private static IObservable<Unit> UpdateVoices(this ApplicationModulesManager manager) 
            => manager.UpdateVoicesOnViewRefresh();

        private static IObservable<Unit> UpdateVoicesOnViewRefresh(this ApplicationModulesManager manager) 
            => manager.WhenApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechAccount),ViewType.DetailView)
                .SelectUntilViewClosed(frame => frame.GetController<RefreshController>().RefreshAction.WhenConcatRetriedExecution(_ =>frame.View.CurrentObject.To<SpeechAccount>().UpdateVoices() )).ToUnit());

        public static SpeechAccount DefaultAccount(this IObjectSpace space,XafApplication application) 
            => space.FindObject<SpeechAccount>(CriteriaOperator.Parse(application.Model.SpeechModel().DefaultAccountCriteria));

        public static IObservable<Unit> Speak(this SpeechAccount defaultAccount, IModelSpeech speechModel) 
            => Observable.Using(() => new SpeechSynthesizer(defaultAccount.SpeechConfig()), synthesizer
                => synthesizer.SpeakSsmlAsync(Clipboard.GetText()).ToObservable().ObserveOnContext()
                    .DoWhen(_ => !new DirectoryInfo(speechModel.DefaultStorageFolder).Exists,
                        _ => Directory.CreateDirectory(speechModel.DefaultStorageFolder))
                    .SelectMany(result => {
                        var lastSpeak = defaultAccount.ObjectSpace.GetObjectsQuery<TextToSpeech>().Max(speech => speech.Oid) + 1;
                        var path = $"{speechModel.DefaultStorageFolder}\\{lastSpeak}.wav";
                        return File.WriteAllBytesAsync(path, result.AudioData).ToObservable().ObserveOnContext()
                            .SelectMany(_ => {
                                var textToSpeech = defaultAccount.ObjectSpace.CreateObject<TextToSpeech>();
                                textToSpeech.Duration = result.AudioDuration;
                                textToSpeech.File = textToSpeech.CreateObject<FileLinkObject>();
                                textToSpeech.File.FileName = Path.GetFileName(path);
                                textToSpeech.File.FullName = path;
                                return textToSpeech.Commit();
                            });
                    }));

        public static SpeechConfig SpeechConfig(this SpeechAccount account,SpeechLanguage recognitionLanguage=null,[CallerMemberName]string callerMember="") {
            var speechConfig = Microsoft.CognitiveServices.Speech.SpeechConfig.FromSubscription(account.Subscription, account.Region);
            speechConfig.SpeechRecognitionLanguage = $"{recognitionLanguage?.Name}";
            speechConfig.EnableAudioLogging();
            var path = $"{AppDomain.CurrentDomain.ApplicationPath()}\\Logs\\{nameof(SpeechConfig)}{callerMember}.log";
            if (!File.Exists(path)) {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            }
            speechConfig.SetProperty(PropertyId.Speech_LogFilename, path);
            return speechConfig;
        }

        public static SpeechTranslationConfig TranslationConfig(this SpeechAccount account) 
            => SpeechTranslationConfig.FromSubscription(account.Subscription, account.Region);
        
        private static void EnsureVoice(this SpeechAccount account,VoiceInfo voiceInfo) {
            var speechVoice = account.ObjectSpace.EnsureObject<SpeechVoice>(voice => voice.Account!=null&& voice.Account.Oid==account.Oid&&voice.Name==voiceInfo.Name,inTransaction:true);
            speechVoice.Gender = voiceInfo.Gender;
            speechVoice.Language=account.ObjectSpace.EnsureObject<SpeechLanguage>(language => language.Name==voiceInfo.Locale,language => language.Name=voiceInfo.Locale,true);
            speechVoice.Name = voiceInfo.LocalName;
            speechVoice.ShortName = voiceInfo.ShortName;
            speechVoice.VoicePath = voiceInfo.VoicePath;
            speechVoice.VoiceType = voiceInfo.VoiceType;
            speechVoice.Account=account;
        }

        private static IObservable<Unit> UpdateVoicesOnCommit(this ApplicationModulesManager manager) 
            => manager.WhenSpeechApplication(application => application.WhenCommitted<SpeechAccount>(ObjectModification.New).ToObjects()
                    .SelectMany(UpdateVoices))
                .ToUnit();

        private static IObservable<VoiceInfo[]> UpdateVoices(this SpeechAccount account) 
            => Observable.Using(() => new SpeechSynthesizer(account.SpeechConfig()),synthesizer => synthesizer.GetVoicesAsync().ToObservable()
                .ObserveOnContext().SelectMany(result => result.Voices.ToNowObservable()
                    .Do(account.EnsureVoice ).BufferUntilCompleted().Do(_ => {
                        account.CommitChanges();
                        account.ObjectSpace.Refresh();
                    })));
    }
}