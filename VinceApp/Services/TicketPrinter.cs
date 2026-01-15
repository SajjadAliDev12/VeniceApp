using System;
using System.Linq;
using System.Windows;
using VinceApp.Data;
using System.Drawing;
using System.Drawing.Printing;

namespace VinceApp.Services
{
    public class TicketPrinter
    {
        // الخطوط
        private Font fontTitle = new Font("Arial", 14, System.Drawing.FontStyle.Bold);
        private Font fontHeader = new Font("Arial", 10, System.Drawing.FontStyle.Bold);
        private Font fontBody = new Font("Arial", 9, System.Drawing.FontStyle.Bold); // جعلت الخط عريضاً قليلاً للوضوح
        private Font fontSmall = new Font("Arial", 8, System.Drawing.FontStyle.Regular);

        private int _orderId;
        private bool _isKitchen;

        // تنسيقات النصوص
        private StringFormat fmtCenter;
        private StringFormat fmtRight;
        private StringFormat fmtLeft;

        public TicketPrinter()
        {
            // إعداد التنسيقات مع دعم العربية
            fmtCenter = new StringFormat(StringFormatFlags.DirectionRightToLeft);
            fmtCenter.Alignment = StringAlignment.Center;
            fmtCenter.LineAlignment = StringAlignment.Center; // توسيط عمودي أيضاً

            fmtRight = new StringFormat(StringFormatFlags.DirectionRightToLeft);
            fmtRight.Alignment = StringAlignment.Near; // يمين
            fmtRight.LineAlignment = StringAlignment.Center;

            fmtLeft = new StringFormat(StringFormatFlags.DirectionRightToLeft);
            fmtLeft.Alignment = StringAlignment.Far; // يسار
            fmtLeft.LineAlignment = StringAlignment.Center;
        }

        public void Print(int orderId, bool isKitchenTicket)
        {
            _orderId = orderId;
            _isKitchen = isKitchenTicket;

            PrintDocument pDoc = new PrintDocument();
            pDoc.PrintPage += new PrintPageEventHandler(pDoc_PrintPage);
            pDoc.PrinterSettings.PrintToFile = false;

            try
            {
                pDoc.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("تأكد من الطابعة.\n" + ex.Message);
            }
        }

        private void pDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            float y = 10;
            int w = 270; // عرض الورقة

            using (var context = new VinceSweetsDbContext())
            {
                var order = context.Orders.Find(_orderId);
                var ParentOrder = context.Orders.Find(order.ParentOrderId);
                if (order == null) return;

                // ================= الرأس =================
                if (!_isKitchen)
                {
                    DrawFullLine(g, "Venice Sweets", fontTitle, fmtCenter, w, ref y);
                    DrawFullLine(g, "شارع الامام علي (ع)", fontSmall, fmtCenter, w, ref y);
                    DrawLineSeparator(g, w, ref y);
                }
                else
                {
                    DrawFullLine(g, "** مطبخ **", fontTitle, fmtCenter, w, ref y);
                }

                // ================= بيانات الفاتورة =================
                DrawFullLine(g, $"رقم: #{order.OrderNumber}", fontHeader, fmtRight, w, ref y);
                string type;
                if(order.TableId != null)
                {
                    if (order.ParentOrderId != null)
                        type = $"ملحق الطلب رقم #{ParentOrder.OrderNumber} - صالة";
                    else
                        type = "صالة";
                }
                else
                {
                    if (order.ParentOrderId != null)
                        type = $"ملحق الطلب رقم #{ParentOrder.OrderNumber} - سفري";
                    else
                        type = "سفري";
                }
                DrawFullLine(g, $"النوع: {type}", fontHeader, fmtRight, w, ref y);
                DrawFullLine(g, $"التاريخ: {order.OrderDate:HH:mm dd/MM}", fontSmall, fmtRight, w, ref y);
                DrawLineSeparator(g, w, ref y);

                // ================= جدول المنتجات (التغيير هنا) =================
                var details = context.OrderDetails.Where(d => d.OrderId == _orderId).ToList();

                if (!_isKitchen)
                {
                    // رسم عناوين الأعمدة
                    // تقسيم العرض (270): المجموع(60) | السعر(60) | العدد(30) | المنتج(120)
                    // ملاحظة: الرسم يبدأ من اليسار (0)، لكن الترتيب العربي من اليمين

                    float h = 20; // ارتفاع سطر العناوين

                    // 1. عنوان المنتج (أقصى اليمين)
                    g.DrawString("المنتج", fontBody, Brushes.Black, new RectangleF(150, y, 120, h), fmtRight);
                    // 2. عنوان العدد
                    g.DrawString("عدد", fontBody, Brushes.Black, new RectangleF(120, y, 30, h), fmtCenter);
                    // 3. عنوان السعر
                    g.DrawString("السعر", fontBody, Brushes.Black, new RectangleF(60, y, 60, h), fmtCenter);
                    // 4. عنوان المجموع (أقصى اليسار)
                    g.DrawString("المجموع", fontBody, Brushes.Black, new RectangleF(0, y, 60, h), fmtLeft);

                    y += h;
                    DrawLineSeparator(g, w, ref y);
                }

                foreach (var item in details)
                {
                    float h = 25; // ارتفاع السطر

                    if (!_isKitchen)
                    {
                        // 1. المنتج
                        g.DrawString(item.ProductName, fontSmall, Brushes.Black, new RectangleF(150, y, 120, h), fmtRight);

                        // 2. العدد
                        g.DrawString(item.Quantity.ToString(), fontBody, Brushes.Black, new RectangleF(120, y, 30, h), fmtCenter);

                        // 3. السعر الفردي
                        g.DrawString($"{item.Price:N0}", fontSmall, Brushes.Black, new RectangleF(60, y, 60, h), fmtCenter);

                        // 4. المجموع الفرعي (السعر * العدد)
                        decimal subTotal = item.Price * item.Quantity;
                        g.DrawString($"{subTotal:N0}", fontBody, Brushes.Black, new RectangleF(0, y, 60, h), fmtLeft);
                    }
                    else
                    {
                        // للمطبخ: بسيط كما كان
                        g.DrawString(item.ProductName, fontHeader, Brushes.Black, new RectangleF(50, y, 220, h), fmtRight);
                        g.DrawString($"x{item.Quantity}", fontTitle, Brushes.Black, new RectangleF(0, y, 50, h), fmtLeft);
                    }
                    y += h;
                }

                DrawLineSeparator(g, w, ref y);

                // ================= المجموع الكلي =================
                if (!_isKitchen)
                {
                    decimal total = order.TotalAmount ?? 0;

                    // مربع للإجمالي
                    RectangleF rectTotal = new RectangleF(0, y, w, 30);
                    g.DrawString("الإجمالي النهائي:", fontHeader, Brushes.Black, rectTotal, fmtRight);
                    g.DrawString($"{total:N0} د.ع", fontTitle, Brushes.Black, rectTotal, fmtLeft);

                    y += 40;
                    DrawFullLine(g, "شكراً لزيارتكم", fontSmall, fmtCenter, w, ref y);
                }
            }
        }

        // دوال مساعدة مختصرة
        private void DrawFullLine(Graphics g, string text, Font f, StringFormat fmt, int w, ref float y)
        {
            SizeF size = g.MeasureString(text, f);
            RectangleF rect = new RectangleF(0, y, w, size.Height + 5);
            g.DrawString(text, f, Brushes.Black, rect, fmt);
            y += size.Height + 5;
        }

        private void DrawLineSeparator(Graphics g, int w, ref float y)
        {
            g.DrawLine(Pens.Black, 0, y, w, y);
            y += 10;
        }
    }
}