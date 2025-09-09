using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;
using System.Windows; // để dùng Application.Current?.Dispatcher

namespace DataCenterManagement.ViewModels
{
    /// <summary>
    /// Base cho mọi ViewModel: cung cấp PropertyChanged (ObservableObject),
    /// trạng thái bận/rảnh, tiêu đề, status message và helpers chạy nền có cờ bận.
    /// </summary>
    public abstract partial class BaseViewModel : ObservableObject
    {
        /// <summary>
        /// Tiêu đề hiển thị của màn hình / view.
        /// </summary>
        [ObservableProperty]
        private string? title;

        /// <summary>
        /// Thông điệp trạng thái ngắn (ví dụ: "Đang tải...", "Lưu thành công").
        /// </summary>
        [ObservableProperty]
        private string? status;

        /// <summary>
        /// VM đang bận? Tự động notify IsNotBusy khi thay đổi.
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isBusy;

        /// <summary>
        /// Phủ định của IsBusy, dùng tiện trong binding.
        /// </summary>
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Gọi khi view xuất hiện (tuỳ view gọi thủ công).
        /// </summary>
        public virtual void OnAppearing()
        { }

        /// <summary>
        /// Gọi khi view biến mất (tuỳ view gọi thủ công).
        /// </summary>
        public virtual void OnDisappearing()
        { }

        /// <summary>
        /// Thiết lập nhanh Status (kèm Debug).
        /// </summary>
        protected void SetStatus(string? message)
        {
            Status = message;
            if (!string.IsNullOrWhiteSpace(message))
                Debug.WriteLine($"[Status] {GetType().Name}: {message}");
        }

        /// <summary>
        /// Chạy công việc async với cờ IsBusy; tự bắt/ghi log exception.
        /// Dùng cho các thao tác tải/lưu dài.
        /// </summary>
        /// <param name="work">Hàm async nhận CancellationToken.</param>
        /// <param name="token">Token ngoài (tuỳ chọn).</param>
        protected async Task RunBusyAsync(Func<CancellationToken, Task> work, CancellationToken token = default)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                await work(cts.Token);
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
                IsBusy = false;
            }
        }

        /// <summary>
        /// Đảm bảo chạy trên UI thread (Dispatcher).
        /// </summary>
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
    }
}