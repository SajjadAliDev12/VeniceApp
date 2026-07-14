using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using VinceApp.Data.Models;
using static VinceApp.Data.Enums.Enums; // لاستخدام EnPrinter

namespace VinceApp.Services
{
    public class TicketPrinter : IDisposable
    {
        // تم إضافة وضع الطباعة للتمييز بين الأقسام
        public enum PrintMode { Customer, Kitchen, IceCream }

        private class ReceiptData
        {
            public string StoreName { get; set; }
            public string StoreAddress { get; set; }
            public string StorePhone { get; set; }
            public string Footer { get; set; }
            public string OrderNumber { get; set; }
            public DateTime Date { get; set; }
            public string TableText { get; set; }
            public string ParentOrderText { get; set; }
            public List<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
            public decimal Total { get; set; }
            public decimal Discount { get; set; }
            public decimal Final { get; set; }
            public string TargetPrinterName { get; set; }
        }

        private class ReceiptItem
        {
            public string Name { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public EnPrinter CategoryPrinterDestination { get; set; } // لمعرفة وجهة الصنف
        }

        private readonly Font fontTitle;
        private readonly Font fontHeader;
        private readonly Font fontBody;
        private readonly Font fontSmall;
        private readonly StringFormat fmtCenter;
        private readonly StringFormat fmtRight;
        private readonly StringFormat fmtLeft;

        private ReceiptData _currentData;
        private PrintMode _currentMode; // تغيير من bool إلى enum لدعم تعدد الطابعات

        public TicketPrinter()
        {
            fontTitle = new Font("Arial", 14, FontStyle.Bold);
            fontHeader = new Font("Arial", 10, FontStyle.Bold);
            fontBody = new Font("Arial", 9, FontStyle.Bold);
            fontSmall = new Font("Arial", 8, FontStyle.Regular);

            fmtCenter = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            fmtRight = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center };
            fmtLeft = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        }

        // تم تعديل المعامل ليدعم 3 حالات (زبون، مطبخ، آيسكريم)
        public bool Print(int orderId, PrintMode mode = PrintMode.Customer)
        {
            _currentMode = mode;
            _currentData = null;

            try
            {
                using (var context = new VinceSweetsDbContext())
                {
                    var settings = context.AppSettings.AsNoTracking().FirstOrDefault();
                    var localConfig = AppConfigService.ReadUserConfig();
                    string pName = "";

                    // 1. تحديد اسم الطابعة بناءً على النوع
                    switch (mode)
                    {
                        case PrintMode.Kitchen:
                            pName = localConfig["KitchenPrinter"]?.ToString();
                            break;
                        case PrintMode.IceCream:
                            pName = localConfig["IceCreamPrinter"]?.ToString();
                            break;
                        default:
                            pName = localConfig["PrinterName"]?.ToString();
                            break;
                    }

                    if (string.IsNullOrEmpty(pName)) return false;
                    if (pName == "None") return true;
                    // 2. جلب الطلب مع بيانات التصنيف لفلترة الأصناف
                    var order = context.Orders
                        .Include(o => o.OrderDetails)
                            .ThenInclude(d => d.Product)
                            .ThenInclude(p => p.Category)
                        .AsNoTracking()
                        .FirstOrDefault(o => o.Id == orderId);

                    if (order == null) return false;
                    if (order.OrderSource == OrderSource.EnToters)
                        return true;
                    var data = new ReceiptData
                    {
                        TargetPrinterName = pName,
                        StoreName = settings?.StoreName ?? "Venice Sweets",
                        StoreAddress = settings?.StoreAddress ?? "", 
                        StorePhone = settings?.StorePhone ?? "",
                        Footer = settings?.ReceiptFooter ?? "شكراً لزيارتكم",
                        OrderNumber = $"#{order.OrderNumber}",
                        Date = order.OrderDate ?? DateTime.Now,
                        Total = order.TotalAmount ?? 0,
                        Discount = order.DiscountAmount ?? 0,
                        Final = (order.TotalAmount ?? 0) - (order.DiscountAmount ?? 0)
                    };

                    // 3. فلترة الأصناف بناءً على وجهة الطباعة
                    var allItems = order.OrderDetails
                        .Where(x => !x.isDeleted)
                        .Select(i => new ReceiptItem
                        {
                            Name = i.ProductName,
                            Quantity = i.Quantity,
                            Price = i.Price,
                            CategoryPrinterDestination = i.Product.Category.Printer // الوجهة المخزنة في التصنيف
                        }).ToList();

                    if (mode == PrintMode.Customer)
                    {
                        data.Items = allItems; // وصل الزبون يطبع كل شيء
                    }
                    else if (mode == PrintMode.Kitchen)
                    {
                        data.Items = allItems.Where(i => i.CategoryPrinterDestination == EnPrinter.enKitchen).ToList();
                    }
                    else if (mode == PrintMode.IceCream)
                    {
                        data.Items = allItems.Where(i => i.CategoryPrinterDestination == EnPrinter.enIceCream).ToList();
                    }

                    // إذا كانت القائمة فارغة للقسم المحدد، لا نطبع شيئاً
                    if (data.Items.Count == 0 && mode != PrintMode.Customer) return true;

                    // معالجة الطاولة والملحق (نفس المنطق السابق)
                    if (order.TableId.HasValue)
                    {
                        var table = context.RestaurantTables.Find(order.TableId.Value);
                        data.TableText = table != null ? $"طاولة {table.TableNumber}" : "طاولة ?";
                    }
                    else data.TableText = "سفري";

                    if (order.ParentOrderId.HasValue)
                    {
                        var parentNum = context.Orders.Where(o => o.Id == order.ParentOrderId.Value).Select(o => o.OrderNumber).FirstOrDefault();
                        if (parentNum > 0) data.ParentOrderText = $"ملحق للطلب رقم {parentNum}";
                    }

                    _currentData = data;
                }

                using (PrintDocument pDoc = new PrintDocument())
                {
                    pDoc.PrintPage += pDoc_PrintPage;
                    pDoc.PrinterSettings.PrinterName = _currentData.TargetPrinterName;
                    if (pDoc.PrinterSettings.IsValid)
                    {
                        pDoc.Print();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "TicketPrinter Error");
                return false;
            }
        }

        private void pDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            if (_currentData == null) return;
            Graphics g = e.Graphics;
            float y = 10;
            int w = 270;

            // ------------------ (Header) ------------------
            if (_currentMode == PrintMode.Customer)
            {
                DrawFullLine(g, _currentData.StoreName, fontTitle, fmtCenter, w, ref y);

                // إضافة رسم العنوان أسفل الاسم
                if (!string.IsNullOrEmpty(_currentData.StoreAddress))
                    DrawFullLine(g, _currentData.StoreAddress, fontSmall, fmtCenter, w, ref y);

                if (!string.IsNullOrEmpty(_currentData.StorePhone))
                    DrawFullLine(g, _currentData.StorePhone, fontSmall, fmtCenter, w, ref y);

                DrawLineSeparator(g, w, ref y);
            }
            else
            {
                // تغيير العنوان بناءً على القسم
                string headerTitle = _currentMode == PrintMode.Kitchen ? "** تجهيز المطبخ **" : "** قسم الآيسكريم **";
                DrawFullLine(g, headerTitle, fontTitle, fmtCenter, w, ref y);
                DrawLineSeparator(g, w, ref y);
            }

            // ------------------ (Order Info) ------------------
            DrawFullLine(g, _currentData.OrderNumber, fontTitle, fmtCenter, w, ref y);
            DrawFullLine(g, _currentData.Date.ToString("dd/MM/yyyy hh:mm tt"), fontSmall, fmtRight, w, ref y);
            DrawFullLine(g, _currentData.TableText, fontHeader, fmtRight, w, ref y);

            if (!string.IsNullOrEmpty(_currentData.ParentOrderText))
                DrawFullLine(g, _currentData.ParentOrderText, fontHeader, fmtRight, w, ref y);

            DrawLineSeparator(g, w, ref y);

            // ------------------ (Details) ------------------
            if (_currentMode == PrintMode.Customer)
            {
                g.DrawString("الصنف", fontBody, Brushes.Black, new RectangleF(100, y, 170, 20), fmtRight);
                g.DrawString("السعر", fontBody, Brushes.Black, new RectangleF(50, y, 50, 20), fmtCenter);
                g.DrawString("العدد", fontBody, Brushes.Black, new RectangleF(0, y, 50, 20), fmtCenter);
                y += 20;
                g.DrawLine(Pens.Black, 0, y, w, y);
                y += 5;
            }

            foreach (var item in _currentData.Items)
            {
                float h = g.MeasureString(item.Name, (_currentMode == PrintMode.Customer ? fontSmall : fontHeader), w - 60).Height + 5;
                if (h < 25) h = 25;

                if (_currentMode == PrintMode.Customer)
                {
                    g.DrawString(item.Name, fontSmall, Brushes.Black, new RectangleF(100, y, 170, h), fmtRight);
                    g.DrawString($"{item.Price:0.#}", fontSmall, Brushes.Black, new RectangleF(50, y, 50, h), fmtCenter);
                    g.DrawString(item.Quantity.ToString(), fontBody, Brushes.Black, new RectangleF(0, y, 50, h), fmtCenter);
                }
                else
                {
                    g.DrawString(item.Name, fontHeader, Brushes.Black, new RectangleF(60, y, 210, h), fmtRight);
                    g.DrawString($"x{item.Quantity}", fontTitle, Brushes.Black, new RectangleF(0, y, 60, h), fmtLeft);
                }
                y += h;
            }

            DrawLineSeparator(g, w, ref y);

            // ------------------ (Totals) ------------------
            if (_currentMode == PrintMode.Customer)
            {
                if (_currentData.Discount > 0)
                {
                    DrawTwoCols(g, "المجموع:", $"{_currentData.Total:N0}", fontBody, w, ref y);
                    DrawTwoCols(g, "الخصم:", $"-{_currentData.Discount:N0}", fontBody, w, ref y);
                    DrawLineSeparator(g, w, ref y);
                }
                DrawTwoCols(g, "المبلغ الكلي:", $"{_currentData.Final:N0} د.ع", fontTitle, w, ref y);
                y += 20;
                DrawFullLine(g, _currentData.Footer, fontSmall, fmtCenter, w, ref y);
            }
        }

        // الدوال المساعدة تبقى كما هي...
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

        public void Dispose()
        {
            fontTitle?.Dispose(); fontHeader?.Dispose(); fontBody?.Dispose(); fontSmall?.Dispose();
            fmtCenter?.Dispose(); fmtRight?.Dispose(); fmtLeft?.Dispose();
        }
    }
}