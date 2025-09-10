using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace DataCenterManagement.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private string? title;

        [ObservableProperty]
        private string? status;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isBusy;

        public bool IsNotBusy => !IsBusy;

        private readonly SemaphoreSlim _busySemaphore = new(1, 1);

        public virtual void OnAppearing()
        { }

        public virtual void OnDisappearing()
        { }

        protected void SetStatus(string? message)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                Status = message;
                if (!string.IsNullOrWhiteSpace(message))
                    Debug.WriteLine($"[Status] {GetType().Name}: {message}");
            }
            else
            {
                dispatcher.InvokeAsync(() =>
                {
                    Status = message;
                    if (!string.IsNullOrWhiteSpace(message))
                        Debug.WriteLine($"[Status] {GetType().Name}: {message}");
                });
            }
        }

        protected async Task RunBusyAsync(Func<CancellationToken, Task> work, CancellationToken token = default)
        {
            if (!_busySemaphore.Wait(0))
                return;

            try
            {
                await RunOnUiAsync(() => IsBusy = true);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                await work(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                SetStatus("Đã huỷ.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                SetStatus("Có lỗi xảy ra.");
            }
            finally
            {
                await RunOnUiAsync(() => IsBusy = false);
                _busySemaphore.Release();
            }
        }

        protected static Task RunOnUiAsync(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                action();
                return Task.CompletedTask;
            }
            return dispatcher.InvokeAsync(action).Task;
        }

        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                OnPropertyChanged(propertyName);
            }
            else
            {
                dispatcher.InvokeAsync(() => OnPropertyChanged(propertyName));
            }
        }

        protected void RaisePropertiesChanged(params string[] propertyNames)
        {
            if (propertyNames == null || propertyNames.Length == 0) return;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
            {
                foreach (var p in propertyNames) OnPropertyChanged(p);
            }
            else
            {
                dispatcher.InvokeAsync(() =>
                {
                    foreach (var p in propertyNames) OnPropertyChanged(p);
                });
            }
        }
    }
}