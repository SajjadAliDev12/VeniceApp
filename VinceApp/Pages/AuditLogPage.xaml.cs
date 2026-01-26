using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using VinceApp.Data;

namespace VinceApp.Pages
{
    public partial class AuditLogPage : Page
    {
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 0;

        // ✅ DTO خفيف للعرض فقط (بدون Changes)
        private class AuditLogRow
        {
            public int Id { get; set; }
            public DateTime Timestamp { get; set; }
            public string UserFullName { get; set; } = "";
            public string Action { get; set; } = "";
            public string TableName { get; set; } = "";
            public string RecordId { get; set; } = "";
        }

        public AuditLogPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // ✅ عرض فقط => بدون Tracking لتسريع الأداء
                    var query = context.AuditLogs
                        .AsNoTracking()
                        .AsQueryable();

                    // --- Filters ---

                    // 1) User name search
                    if (!string.IsNullOrWhiteSpace(txtUserSearch.Text))
                    {
                        var term = txtUserSearch.Text.Trim();
                        query = query.Where(x => EF.Functions.Like(x.UserFullName, $"%{term}%"));
                    }

                    // 2) Date range
                    if (dpFrom.SelectedDate.HasValue)
                    {
                        var from = dpFrom.SelectedDate.Value.Date;
                        query = query.Where(x => x.Timestamp >= from);
                    }

                    if (dpTo.SelectedDate.HasValue)
                    {
                        var to = dpTo.SelectedDate.Value.Date.AddDays(1); // end of day
                        query = query.Where(x => x.Timestamp < to);
                    }

                    // 3) Action filter
                    if (cmbAction.SelectedIndex > 0)
                    {
                        if (cmbAction.SelectedItem is ComboBoxItem selectedItem &&
                            selectedItem.Content != null)
                        {
                            string action = selectedItem.Content.ToString();
                            query = query.Where(x => x.Action == action);
                        }
                    }

                    // ✅ Count قبل Pagination
                    int totalCount = query.Count();
                    _totalPages = (int)Math.Ceiling((double)totalCount / _pageSize);
                    if (_totalPages == 0) _totalPages = 1;

                    if (_currentPage > _totalPages) _currentPage = _totalPages;
                    if (_currentPage < 1) _currentPage = 1;

                    // ✅ Projection صحيح متوافق مع الـ XAML:
                    // XAML يحتاج: Id, Timestamp, UserFullName, Action, TableName, RecordId
                    var list = query
                        .OrderByDescending(x => x.Timestamp)
                        .Skip((_currentPage - 1) * _pageSize)
                        .Take(_pageSize)
                        .Select(x => new AuditLogRow
                        {
                            Id = x.Id,
                            Timestamp = x.Timestamp,
                            UserFullName = x.UserFullName,
                            Action = x.Action,
                            TableName = x.TableName,
                            RecordId = x.RecordId
                        })
                        .ToList();

                    dgLogs.ItemsSource = list;

                    // تحديث الأزرار
                    txtPageInfo.Text = $"صفحة {_currentPage} من {_totalPages}";
                    btnPrev.IsEnabled = _currentPage > 1;
                    btnNext.IsEnabled = _currentPage < _totalPages;
                }
            }
            catch
            {
                ToastControl.Show("خطأ", "حدث خطأ أثناء التحميل", ToastControl.NotificationType.Error);
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadData();
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txtUserSearch.Text = "";
            dpFrom.SelectedDate = null;
            dpTo.SelectedDate = null;
            cmbAction.SelectedIndex = 0;
            _currentPage = 1;
            LoadData();
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadData();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadData();
            }
        }

        private void BtnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag != null)
                {
                    if (!int.TryParse(btn.Tag.ToString(), out int auditId))
                    {
                        ToastControl.Show("تنبيه", "معرّف السجل غير صحيح", ToastControl.NotificationType.Warning);
                        return;
                    }

                    using (var context = new VinceSweetsDbContext())
                    {
                        // ✅ جلب JSON فقط عند الطلب
                        var json = context.AuditLogs
                            .AsNoTracking()
                            .Where(x => x.Id == auditId)
                            .Select(x => x.Changes)
                            .FirstOrDefault();

                        ShowDetailsWindow(json);
                    }
                }
            }
            catch
            {
                ToastControl.Show("خطأ", "حدث خطأ أثناء عرض التفاصيل", ToastControl.NotificationType.Error);
            }
        }

        private void ShowDetailsWindow(string json)
        {
            var window = new Window
            {
                Title = "تفاصيل التغييرات",
                Width = 520,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FlowDirection = FlowDirection.RightToLeft,
                ResizeMode = ResizeMode.NoResize
            };

            var textBox = new TextBox
            {
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontSize = 14,
                Padding = new Thickness(10),
                Text = FormatJson(json),
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                TextWrapping = TextWrapping.NoWrap
            };

            window.Content = textBox;
            window.ShowDialog();
        }

        private string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "{}")
                return "لا توجد تغييرات مسجلة.";

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
                return JsonSerializer.Serialize(jsonElement, options);
            }
            catch
            {
                return json;
            }
        }
    }
}
