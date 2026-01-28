using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms; // قد نحتاجه للمسافات ولكن ليس ضرورياً هنا
using VinceApp.Data.Models;

namespace VinceApp.Services
{
    // ✅ 1. إضافة IDisposable لتنظيف الخطوط
    public class TicketPrinter : IDisposable
    {
        // الخطوط
        private readonly Font fontTitle;
        private readonly Font fontHeader;
        private readonly Font fontBody;
        private readonly Font fontSmall;

        // تنسيقات النصوص
        private readonly StringFormat fmtCenter;
        private readonly StringFormat fmtRight;
        private readonly StringFormat fmtLeft;

        private int _orderId;
        private bool _isKitchen;

        public TicketPrinter()
        {
            // تعريف الخطوط
            fontTitle = new Font("Arial", 14, System.Drawing.FontStyle.Bold);
            fontHeader = new Font("Arial", 10, System.Drawing.FontStyle.Bold);
            fontBody = new Font("Arial", 9, System.Drawing.FontStyle.Bold);
            fontSmall = new Font("Arial", 8, System.Drawing.FontStyle.Regular);

            // تعريف التنسيقات
            fmtCenter = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            fmtRight = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            fmtLeft = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        }

        public void Print(int orderId, bool isKitchenTicket)
        {
            _orderId = orderId;
            _isKitchen = isKitchenTicket;

            // ✅ استخدام using لضمان حذف كائن الطباعة
            using (PrintDocument pDoc = new PrintDocument())
            {
                pDoc.PrintPage += new PrintPageEventHandler(pDoc_PrintPage);
                pDoc.PrinterSettings.PrintToFile = false;
                pDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                try
                {
                    string printerName = null;
                    using (var context = new VinceSweetsDbContext())
                    {
                        // جلب اسم الطابعة بسرعة (Select لتقليل البيانات المسحوبة)
                        printerName = context.AppSettings
                                             .Select(s => s.PrinterName)
                                             .FirstOrDefault();
                    }

                    if (!string.IsNullOrWhiteSpace(printerName))
                    {
                        pDoc.PrinterSettings.PrinterName = printerName;
                    }

                    if (pDoc.PrinterSettings.IsValid)
                    {
                        pDoc.Print();
                    }
                    else
                    {
                        ToastControl.Show("خطأ", "لم يتم العثور على الطابعة المحددة", ToastControl.NotificationType.Error);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "TicketPrinter Error");
                    ToastControl.Show("فشل الطباعة", "تأكد من توصيل الطابعة", ToastControl.NotificationType.Error);
                }
                finally
                {
                    pDoc.PrintPage -= new PrintPageEventHandler(pDoc_PrintPage);
                }
            }
        }

        private void pDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            float y = 10;

            // ✅ عرض الورقة (80mm ≈ 280-300 units)
            // إذا كانت الطابعة 58mm اجعلها 180-200
            int w = 270;

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // جلب الطلب مع التفاصيل والعلاقات الضرورية فقط
                    var order = context.Orders
                        .Include(o => o.Table)
                        .Include(o => o.OrderDetails) // ✅ نجلب التفاصيل هنا مباشرة
                        .AsNoTracking() // ✅ أسرع للقراءة فقط
                        .FirstOrDefault(o => o.Id == _orderId);

                    if (order == null) return;

                    // بيانات المتجر
                    var settings = context.AppSettings.FirstOrDefault();
                    string storeName = settings?.StoreName ?? "Venice Sweets";
                    string storePhone = settings?.StorePhone ?? "";
                    string footer = settings?.ReceiptFooter ?? "شكراً لزيارتكم";

                    // ------------------ (Header) ------------------
                    if (!_isKitchen)
                    {
                        DrawFullLine(g, storeName, fontTitle, fmtCenter, w, ref y);
                        if (!string.IsNullOrEmpty(storePhone))
                            DrawFullLine(g, storePhone, fontSmall, fmtCenter, w, ref y);

                        DrawLineSeparator(g, w, ref y);
                    }
                    else
                    {
                        DrawFullLine(g, "** طلب مطبخ **", fontTitle, fmtCenter, w, ref y);
                        DrawLineSeparator(g, w, ref y);
                    }

                    // ------------------ (Order Info) ------------------
                    DrawFullLine(g, $"#{order.OrderNumber}", fontTitle, fmtCenter, w, ref y);
                    DrawFullLine(g, DateTime.Now.ToString("dd/MM/yyyy HH:mm"), fontSmall, fmtRight, w, ref y);

                    // نوع الطلب
                    string typeText = "سفري";
                    if (order.TableId.HasValue)
                    {
                        string tblNum = order.Table != null ? order.Table.TableNumber.ToString() : "?";
                        typeText = $"طاولة {tblNum}";
                    }
                    DrawFullLine(g, typeText, fontHeader, fmtRight, w, ref y);

                    DrawLineSeparator(g, w, ref y);

                    // ------------------ (Details) ------------------
                    // تفاصيل المنتجات (مفلترة من المحذوفات)
                    var items = order.OrderDetails.Where(x => !x.isDeleted).ToList();

                    // رأس الجدول
                    if (!_isKitchen)
                    {
                        g.DrawString("الصنف", fontBody, Brushes.Black, new RectangleF(100, y, 170, 20), fmtRight);
                        g.DrawString("سعر", fontBody, Brushes.Black, new RectangleF(50, y, 50, 20), fmtCenter);
                        g.DrawString("ك", fontBody, Brushes.Black, new RectangleF(0, y, 50, 20), fmtCenter);
                        y += 20;
                        g.DrawLine(Pens.Black, 0, y, w, y);
                        y += 5;
                    }

                    foreach (var item in items)
                    {
                        float h = g.MeasureString(item.ProductName, (!_isKitchen ? fontSmall : fontHeader), w - 60).Height + 5;
                        if (h < 25) h = 25;

                        if (!_isKitchen)
                        {
                            // اسم المنتج
                            g.DrawString(item.ProductName, fontSmall, Brushes.Black, new RectangleF(100, y, 170, h), fmtRight);
                            // السعر
                            g.DrawString($"{item.Price:0.#}", fontSmall, Brushes.Black, new RectangleF(50, y, 50, h), fmtCenter);
                            // الكمية
                            g.DrawString(item.Quantity.ToString(), fontBody, Brushes.Black, new RectangleF(0, y, 50, h), fmtCenter);
                        }
                        else // للمطبخ (خط كبير واسم فقط)
                        {
                            g.DrawString(item.ProductName, fontHeader, Brushes.Black, new RectangleF(60, y, 210, h), fmtRight);
                            g.DrawString($"x{item.Quantity}", fontTitle, Brushes.Black, new RectangleF(0, y, 60, h), fmtLeft);
                        }
                        y += h;
                    }

                    DrawLineSeparator(g, w, ref y);

                    // ------------------ (Totals) ------------------
                    if (!_isKitchen)
                    {
                        decimal total = order.TotalAmount ?? 0;
                        decimal discount = order.DiscountAmount;
                        decimal final = total - discount;

                        if (discount > 0)
                        {
                            DrawTwoCols(g, "المجموع:", $"{total:N0}", fontBody, w, ref y);
                            DrawTwoCols(g, "الخصم:", $"-{discount:N0}", fontBody, w, ref y);
                            DrawLineSeparator(g, w, ref y);
                        }

                        // الصافي بخط كبير
                        DrawTwoCols(g, "الصافي:", $"{final:N0}", fontTitle, w, ref y);

                        y += 20;
                        DrawFullLine(g, footer, fontSmall, fmtCenter, w, ref y);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Printing Draw Error");
            }
        }

        // دوال مساعدة للرسم
        private void DrawFullLine(Graphics g, string text, Font f, StringFormat fmt, int w, ref float y)
        {
            float h = g.MeasureString(text, f, w).Height;
            g.DrawString(text, f, Brushes.Black, new RectangleF(0, y, w, h), fmt);
            y += h + 2;
        }

        private void DrawTwoCols(Graphics g, string label, string value, Font f, int w, ref float y)
        {
            g.DrawString(label, f, Brushes.Black, new RectangleF(w / 2, y, w / 2, 25), fmtRight);
            g.DrawString(value, f, Brushes.Black, new RectangleF(0, y, w / 2, 25), fmtLeft);
            y += 25;
        }

        private void DrawLineSeparator(Graphics g, int w, ref float y)
        {
            y += 3;
            g.DrawLine(Pens.Black, 0, y, w, y);
            y += 5;
        }

        // ✅ 2. تنفيذ Dispose لتحرير الذاكرة
        public void Dispose()
        {
            fontTitle?.Dispose();
            fontHeader?.Dispose();
            fontBody?.Dispose();
            fontSmall?.Dispose();
            fmtCenter?.Dispose();
            fmtRight?.Dispose();
            fmtLeft?.Dispose();
        }
    }
}