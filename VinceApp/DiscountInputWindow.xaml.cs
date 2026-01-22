using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VinceApp
{
    public partial class DiscountInputWindow : Window
    {
        // القيمة النهائية للخصم التي سيأخذها الكود الخارجي
        public decimal DiscountValue { get; private set; } = 0;

        private readonly decimal _maxAmount; // المبلغ الإجمالي للفاتورة
        private string _input = "0"; // النص المدخل حالياً

        public DiscountInputWindow(decimal maxAmount)
        {
            InitializeComponent();
            _maxAmount = maxAmount;

            // تحديث العرض الأولي
            UpdateDisplay();
        }

        // دالة تحديث الشاشة والتحقق من القيمة
        private void UpdateDisplay()
        {
            if (decimal.TryParse(_input, out decimal val))
            {
                // عرض الرقم بتنسيق العملة (بدون كسور إذا كان N0)
                txtDisplay.Text = val.ToString("N0");

                // التحقق: هل الخصم أكبر من المبلغ؟
                if (val > _maxAmount)
                {
                    txtError.Text = "تنبيه: الخصم أكبر من قيمة الفاتورة!";
                    txtDisplay.Foreground = Brushes.Red;
                }
                else
                {
                    txtError.Text = ""; // مسح الخطأ
                    txtDisplay.Foreground = Brushes.DarkRed; // اللون الطبيعي للخصم
                }
            }
        }

        private void Num_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                string number = btn.Content.ToString();

                // منع كتابة أصفار متكررة في البداية
                if (_input == "0" && number == "0") return;

                // إذا كان الرقم الحالي 0، نستبدله بالرقم الجديد، وإلا نضيفه بجانبه
                if (_input == "0") _input = number;
                else _input += number;

                // حماية إضافية: منع كتابة أرقام خيالية الطول
                if (_input.Length > 9)
                {
                    _input = _input.Substring(0, 9);
                    return;
                }

                UpdateDisplay();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _input = "0";
            UpdateDisplay();
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            // حذف آخر رقم (Syntax C# الحديث)
            if (_input.Length > 1)
            {
                _input = _input[..^1];
            }
            else
            {
                _input = "0";
            }
            UpdateDisplay();
        }

        // زر التأكيد
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (decimal.TryParse(_input, out decimal val))
            {
                // منع التأكيد إذا كانت القيمة غير منطقية
                if (val > _maxAmount)
                {
                    ToastControl.Show("خطأ في الإدخال", "عفواً، قيمة الخصم أكبر من إجمالي الفاتورة!", ToastControl.NotificationType.Info);
                    
                    return;
                }

                // منع الخصم السالب (احتياط)
                if (val < 0)
                {
                    ToastControl.Show("خطأ", "لا يمكن أن يكون الخصم بالسالب", ToastControl.NotificationType.Warning);
                    
                    return;
                }

                DiscountValue = val;
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ==========================================
        // إضافة مقترحة: دالة لحساب النسبة المئوية
        // يمكنك ربطها بزر مستقبلاً (مثلاً زر 10% أو 50%)
        // ==========================================
        private void ApplyPercentage(decimal percent)
        {
            if (_maxAmount > 0)
            {
                decimal discount = _maxAmount * (percent / 100);
                // تقريب الرقم لإزالة الكسور الصغيرة جداً
                discount = Math.Round(discount, 0);

                _input = ((int)discount).ToString();
                UpdateDisplay();
            }
        }

        // مثال لاستخدام دالة النسبة (لو أضفت زر اسمه btn10Percent)
        /*
        private void Btn10Percent_Click(object sender, RoutedEventArgs e)
        {
            ApplyPercentage(10);
        }
        */
    }
}