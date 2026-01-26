using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows;
using VinceApp.Data;

namespace VinceApp.Services
{
    public class TicketPrinter
    {
        // الخطوط
        private readonly Font fontTitle = new Font("Arial", 14, System.Drawing.FontStyle.Bold);
        private readonly Font fontHeader = new Font("Arial", 10, System.Drawing.FontStyle.Bold);
        private readonly Font fontBody = new Font("Arial", 9, System.Drawing.FontStyle.Bold);
        private readonly Font fontSmall = new Font("Arial", 8, System.Drawing.FontStyle.Regular);

        private int _orderId;
        private bool _isKitchen;

        // تنسيقات النصوص
        private readonly StringFormat fmtCenter;
        private readonly StringFormat fmtRight;
        private readonly StringFormat fmtLeft;

        public TicketPrinter()
        {
            fmtCenter = new StringFormat(StringFormatFlags.DirectionRightToLeft)
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            fmtRight = new StringFormat(StringFormatFlags.DirectionRightToLeft)
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center
            };

            fmtLeft = new StringFormat(StringFormatFlags.DirectionRightToLeft)
            {
                Alignment = StringAlignment.Far,
                LineAlignment = StringAlignment.Center
            };
        }

        public void Print(int orderId, bool isKitchenTicket)
        {
            _orderId = orderId;
            _isKitchen = isKitchenTicket;

            using (PrintDocument pDoc = new PrintDocument())
            {
                pDoc.PrintPage += new PrintPageEventHandler(pDoc_PrintPage);
                pDoc.PrinterSettings.PrintToFile = false;

                // (اختياري آمن) لتجنب قص بعض الطابعات الحرارية
                pDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

                try
                {
                    using (var context = new VinceSweetsDbContext())
                    {
                        var settings = context.AppSettings.FirstOrDefault();
                        // إذا كان هناك اسم طابعة محفوظ، نستخدمه
                        if (settings != null && !string.IsNullOrWhiteSpace(settings.PrinterName))
                        {
                            pDoc.PrinterSettings.PrinterName = settings.PrinterName;
                        }
                    }

                    // التحقق من صحة الطابعة قبل الإرسال
                    if (pDoc.PrinterSettings.IsValid)
                    {
                        pDoc.Print();
                    }
                    else
                    {
                        ToastControl.Show("خطأ في الطابعة", "لا يمكن الوصول للطابعة المحددة", ToastControl.NotificationType.Error);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "error with ticket Printer service");
                    ToastControl.Show("خطأ في الطابعة", "لا يمكن الطباعة", ToastControl.NotificationType.Error);
                }
                finally
                {
                    // فك الإيفنت لتجنب أي تسريب في حالات نادرة
                    pDoc.PrintPage -= new PrintPageEventHandler(pDoc_PrintPage);
                }
            }
        }

        private void pDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            float y = 10;
            int w = 270; // عرض الورقة

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    // 1. استخدام Include لجلب بيانات الطاولة مع الطلب لتجنب الأخطاء
                    var order = context.Orders
                                       .Include(o => o.Table)
                                       .FirstOrDefault(o => o.Id == _orderId);

                    if (order == null)
                    {
                        e.HasMorePages = false;
                        return;
                    }

                    // جلب الطلب الأب مع الطاولة الخاصة به أيضًا (للحماية)
                    var ParentOrder = order.ParentOrderId != null
                        ? context.Orders.Include(o => o.Table).FirstOrDefault(p => p.Id == order.ParentOrderId)
                        : null;

                    var settings = context.AppSettings.FirstOrDefault();

                    string storeName = settings?.StoreName ?? "Venice Sweets";
                    string storeAddress = settings?.StoreAddress ?? "العنوان غير محدد";
                    string storePhone = settings?.StorePhone ?? "";
                    string footer = settings?.ReceiptFooter ?? "شكراً لزيارتكم";

                    // ================= الرأس (Header) =================
                    if (!_isKitchen)
                    {
                        DrawFullLine(g, storeName, fontTitle, fmtCenter, w, ref y);
                        if (!string.IsNullOrEmpty(storeAddress))
                            DrawFullLine(g, storeAddress, fontSmall, fmtCenter, w, ref y);
                        if (!string.IsNullOrEmpty(storePhone))
                            DrawFullLine(g, $"هاتف: {storePhone}", fontSmall, fmtCenter, w, ref y);
                        DrawLineSeparator(g, w, ref y);
                    }
                    else
                    {
                        DrawFullLine(g, "** مطبخ **", fontTitle, fmtCenter, w, ref y);
                    }

                    // ================= بيانات الفاتورة =================
                    DrawFullLine(g, $"رقم الطلب: #{order.OrderNumber}", fontHeader, fmtRight, w, ref y);

                    // منطق تحديد النوع (تم تأمينه ضد القيم الفارغة)
                    string type;
                    if (order.TableId != null)
                    {
                        // استخدام (?. و ??) لتجنب الخطأ إذا كانت الطاولة محذوفة
                        string currentTableNum = order.Table?.TableNumber.ToString() ?? "??";

                        if (order.ParentOrderId != null && ParentOrder != null)
                        {
                            string parentTableNum = ParentOrder.Table?.TableNumber.ToString() ?? currentTableNum;
                            type = $"ملحق #{ParentOrder.OrderNumber} -طاولة {parentTableNum} - صالة";
                        }
                        else
                        {
                            type = $"صالة - طاولة {currentTableNum}";
                        }
                    }
                    else
                    {
                        type = (order.ParentOrderId != null && ParentOrder != null)
                            ? $"ملحق #{ParentOrder.OrderNumber} - سفري"
                            : "سفري";
                    }

                    DrawFullLine(g, $"النوع: {type}", fontHeader, fmtRight, w, ref y);

                    // ✅ تعديل مهم: OrderDate قد تكون null وتكسر التنسيق
                    string dateText = (order.OrderDate.HasValue)
                        ? order.OrderDate.Value.ToString("HH:mm dd/MM")
                        : DateTime.Now.ToString("HH:mm dd/MM");

                    DrawFullLine(g, $"التاريخ: {dateText}", fontSmall, fmtRight, w, ref y);
                    DrawLineSeparator(g, w, ref y);

                    // ================= جدول المنتجات =================
                    // ✅ تعديل آمن: تجاهل المحذوفات (إذا عندك isDeleted)
                    var details = context.OrderDetails
                                         .Where(d => d.OrderId == _orderId && !d.isDeleted)
                                         .ToList();

                    if (!_isKitchen)
                    {
                        float h = 20;
                        g.DrawString("المنتج", fontBody, Brushes.Black, new RectangleF(150, y, 120, h), fmtRight);
                        g.DrawString("عدد", fontBody, Brushes.Black, new RectangleF(120, y, 30, h), fmtCenter);
                        g.DrawString("السعر", fontBody, Brushes.Black, new RectangleF(60, y, 60, h), fmtCenter);
                        g.DrawString("المجموع", fontBody, Brushes.Black, new RectangleF(0, y, 60, h), fmtLeft);
                        y += h;
                        DrawLineSeparator(g, w, ref y);
                    }

                    foreach (var item in details)
                    {
                        float h = 25;
                        if (!_isKitchen)
                        {
                            g.DrawString(item.ProductName, fontSmall, Brushes.Black, new RectangleF(150, y, 120, h), fmtRight);
                            g.DrawString(item.Quantity.ToString(), fontBody, Brushes.Black, new RectangleF(120, y, 30, h), fmtCenter);
                            g.DrawString($"{item.Price:N0}", fontSmall, Brushes.Black, new RectangleF(60, y, 60, h), fmtCenter);
                            decimal subTotalItem = item.Price * item.Quantity;
                            g.DrawString($"{subTotalItem:N0}", fontBody, Brushes.Black, new RectangleF(0, y, 60, h), fmtLeft);
                        }
                        else
                        {
                            g.DrawString(item.ProductName, fontHeader, Brushes.Black, new RectangleF(50, y, 220, h), fmtRight);
                            g.DrawString($"x{item.Quantity}", fontTitle, Brushes.Black, new RectangleF(0, y, 50, h), fmtLeft);
                        }
                        y += h;
                    }

                    DrawLineSeparator(g, w, ref y);

                    // ================= المجموع الكلي والخصم =================
                    if (!_isKitchen)
                    {
                        decimal subTotal = order.TotalAmount ?? 0;
                        decimal discount = 0;

                        // التأكد من القيمة بشكل آمن
                        if (order.DiscountAmount > 0)
                            discount = order.DiscountAmount;

                        decimal finalTotal = subTotal - discount;

                        if (discount > 0)
                        {
                            // طباعة المجموع الفرعي
                            RectangleF rectSub = new RectangleF(0, y, w, 25);
                            g.DrawString("المجموع الفرعي:", fontBody, Brushes.Black, rectSub, fmtRight);
                            g.DrawString($"{subTotal:N0}", fontBody, Brushes.Black, rectSub, fmtLeft);
                            y += 25;

                            // طباعة الخصم
                            RectangleF rectDisc = new RectangleF(0, y, w, 25);
                            g.DrawString("قيمة الخصم:", fontBody, Brushes.Black, rectDisc, fmtRight);
                            g.DrawString($"-{discount:N0}", fontBody, Brushes.Black, rectDisc, fmtLeft);
                            y += 25;

                            g.DrawLine(Pens.Gray, 50, y, w - 50, y);
                            y += 5;
                        }

                        RectangleF rectTotal = new RectangleF(0, y, w, 35);
                        g.DrawString("الصافي النهائي:", fontHeader, Brushes.Black, rectTotal, fmtRight);

                        // عرض الصافي إذا كان هناك خصم، أو المجموع العادي إذا لم يوجد
                        decimal amountToShow = (discount > 0) ? finalTotal : subTotal;
                        g.DrawString($"{amountToShow:N0} د.ع", fontTitle, Brushes.Black, rectTotal, fmtLeft);

                        y += 45;

                        DrawFullLine(g, footer, fontSmall, fmtCenter, w, ref y);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "error in pDoc_PrintPage");
                ToastControl.Show("خطأ بالطباعة", "حدث خطأ أثناء تجهيز الفاتورة", ToastControl.NotificationType.Error);
            }
            finally
            {
                e.HasMorePages = false;
            }
        }

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
