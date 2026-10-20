using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;

using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("EncoderSurface")]
public sealed class TEncoderVerification
{
    [Fact]
    public void AudioVerify_Cancelled_RestoresVerificationControl()
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                LSTrialOutlet trial = new(
                    (_, _) => Task.FromException<(bool, string)>(new OperationCanceledException()),
                    _ => { },
                    _ => true,
                    (_, _) => Task.FromException<(bool, string)>(new OperationCanceledException()),
                    _ => { },
                    _ => true,
                    (_, _) => { });
                LSRepertoireOutlet repertoire = new(
                    LRepertoireVideo.LRepertoireEncodersRead,
                    LRepertoireCatalog.LRepertoireContainerNames,
                    LRepertoireVideo.LRepertoireContainerCheck,
                    LRepertoireVideo.LRepertoireTokenResolve,
                    LCapability.LCapabilityRead,
                    LRepertoireAudioCatalog.LRepertoireAudioCandidates,
                    LRepertoireCatalog.LRepertoireAudioCheck,
                    LRepertoireAudioCatalog.LRepertoireAudioResolve,
                    LSCodec.LSCodecAudioRead,
                    LSCodec.LSCodecAudioRead);
                var draft = new CSEncoderDraft(
                    Cadroue.Tests.Interface.TInterface.TEncoderOpenerCreate()(string.Empty),
                    trial,
                    repertoire,
                    false);
                try
                {
                    var audio = new QSEncoderAudio(draft.CSEncoderDraftAudio);
                    string ready = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify");
                    var verification = new Button { Content = ready, IsEnabled = true };
                    MethodInfo validate = typeof(QSEncoderAudio).GetMethod(
                        "QSEncoderAudioValidate", BindingFlags.Instance | BindingFlags.NonPublic)!;

                    ((Task)validate.Invoke(audio, [verification, new Progress<double>()])!).GetAwaiter().GetResult();

                    Assert.True(verification.IsEnabled);
                    Assert.Equal(ready, verification.Content);
                }
                finally
                {
                    draft.CSEncoderDraftClose();
                }
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }

    [Fact]
    public void PictureVerify_Cancelled_RestoresVerificationControl()
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                LSTrialOutlet trial = new(
                    (_, _) => Task.FromException<(bool, string)>(new OperationCanceledException()),
                    _ => { },
                    _ => true,
                    (_, _) => Task.FromException<(bool, string)>(new OperationCanceledException()),
                    _ => { },
                    _ => true,
                    (_, _) => { });
                LSRepertoireOutlet repertoire = new(
                    LRepertoireVideo.LRepertoireEncodersRead,
                    LRepertoireCatalog.LRepertoireContainerNames,
                    LRepertoireVideo.LRepertoireContainerCheck,
                    LRepertoireVideo.LRepertoireTokenResolve,
                    LCapability.LCapabilityRead,
                    LRepertoireAudioCatalog.LRepertoireAudioCandidates,
                    LRepertoireCatalog.LRepertoireAudioCheck,
                    LRepertoireAudioCatalog.LRepertoireAudioResolve,
                    LSCodec.LSCodecAudioRead,
                    LSCodec.LSCodecAudioRead);
                var draft = new CSEncoderDraft(
                    Cadroue.Tests.Interface.TInterface.TEncoderOpenerCreate()(string.Empty),
                    trial,
                    repertoire,
                    false);
                try
                {
                    var video = new QSEncoderVideo(
                        draft.CSEncoderDraftVideo, draft.CSEncoderDraftTrial, draft);
                    var picture = (QSEncoderPicture)RuntimeHelpers.GetUninitializedObject(typeof(QSEncoderPicture));
                    FieldInfo field = typeof(QSEncoderPicture).GetField(
                        "qsEncoderPictureVideo", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    field.SetValue(picture, video);
                    string ready = QLocalizationCatalog.QLocalizationTextRead("Encoder.Button.Verify");
                    var verification = new Button { Content = ready, IsEnabled = true };
                    MethodInfo validate = typeof(QSEncoderPicture).GetMethod(
                        "QSEncoderPictureValidate", BindingFlags.Instance | BindingFlags.NonPublic)!;

                    ((Task)validate.Invoke(picture, [verification, new Progress<double>()])!).GetAwaiter().GetResult();

                    Assert.True(verification.IsEnabled);
                    Assert.Equal(ready, verification.Content);
                }
                finally
                {
                    draft.CSEncoderDraftClose();
                }
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
