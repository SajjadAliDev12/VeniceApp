using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VinceApp.Data;
using System.Text.Json; // ضروري
using System.Collections.Generic;

namespace VinceApp.Pages
{
    public partial class AuditLogPage : Page
    {
        private int _currentPage = 1;
        private int _pageSize = 20;
        private int _totalPages = 0;

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
                    var query = context.AuditLogs.AsQueryable();

                    // --- تطبيق الفلاتر ---

                    // 1. بحث بالاسم
                    if (!string.IsNullOrWhiteSpace(txtUserSearch.Text))
                    {
                        query = query.Where(x => x.UserFullName.Contains(txtUserSearch.Text));
                    }

                    // 2. بحث بالتاريخ
                    if (dpFrom.SelectedDate.HasValue)
                    {
                        var from = dpFrom.SelectedDate.Value.Date;
                        query = query.Where(x => x.Timestamp >= from);
                    }
                    if (dpTo.SelectedDate.HasValue)
                    {
                        var to = dpTo.SelectedDate.Value.Date.AddDays(1); // لنهاية اليوم
                        query = query.Where(x => x.Timestamp < to);
                    }

                    // 3. نوع الحدث
                    if (cmbAction.SelectedIndex > 0) // تجاهل "الكل"
                    {
                        var selectedItem = cmbAction.SelectedItem as ComboBoxItem;
                        string action = selectedItem.Content.ToString();
                        query = query.Where(x => x.Action == action);
                    }

                    // --- التصفح ---
                    int totalCount = query.Count();
                    _totalPages = (int)Math.Ceiling((double)totalCount / _pageSize);
                    if (_totalPages == 0) _totalPages = 1;

                    var list = query.OrderByDescending(x => x.Timestamp)
                                    .Skip((_currentPage - 1) * _pageSize)
                                    .Take(_pageSize)
                                    .ToList();

                    dgLogs.ItemsSource = list;

                    // تحديث الأزرار
                    txtPageInfo.Text = $"صفحة {_currentPage} من {_totalPages}";
                    btnPrev.IsEnabled = _currentPage > 1;
                    btnNext.IsEnabled = _currentPage < _totalPages;
                }
            }
            catch (Exception ex)
            {
                ToastControl.Show("خطأ", "حدث خطأ أثناء التحميل ", ToastControl.NotificationType.Error);
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
            if (_currentPage > 1) { _currentPage--; LoadData(); }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages) { _currentPage++; LoadData(); }
        }

        private void BtnViewDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string jsonChanges = btn.Tag.ToString();
                ShowDetailsWindow(jsonChanges);
            }
        }

        // دالة لعرض نافذة التفاصيل بشكل منسق
        private void ShowDetailsWindow(string json)
        {
            var window = new Window
            {
                Title = "تفاصيل التغييرات",
                Width = 400,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FlowDirection = FlowDirection.RightToLeft,
                ResizeMode = ResizeMode.NoResize
            };

            var textBox = new TextBox
            {
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontSize = 14,
                Padding = new Thickness(10),
                Text = FormatJson(json), // تنسيق النص
                FontFamily = new System.Windows.Media.FontFamily("Consolas") // خط ثابت العرض للقراءة
            };

            window.Content = textBox;
            window.ShowDialog();
        }

        // دالة مساعدة لتجميل شكل الـ JSON
        private string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "{}") return "لا توجد تغييرات مسجلة.";
            try
            {
                // تحويل الـ JSON إلى كائن ديناميكي ثم إعادة كتابته بتنسيق مقروء (Indented)
                var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
                return JsonSerializer.Serialize(jsonElement, options);
            }
            catch
            {
                return json; // في حال فشل التنسيق نعرض النص كما هو
            }
        }
    }
}