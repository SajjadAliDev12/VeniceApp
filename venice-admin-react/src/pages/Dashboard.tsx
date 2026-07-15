import { useEffect, useState } from 'react';
import { getSalesSummary } from '../services/api';
import type { SalesSummaryDto } from '../models/reports';

export default function Dashboard() {
    const [summary, setSummary] = useState<SalesSummaryDto | null>(null);

    useEffect(() => {
        getSalesSummary().then(setSummary).catch(err => console.error(err));
    }, []);

    if (!summary) return <p style={{ textAlign: 'center', padding: '20px' }}>جاري تحميل البيانات...</p>;

    return (
        <div style={{ padding: '20px', direction: 'rtl', fontFamily: 'sans-serif' }}>
            <h2 style={{ color: '#37474F', marginBottom: '20px' }}>📊 داشبورد حلويات البندقية</h2>
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '20px' }}>
                <div style={{ background: '#66BB6A', padding: '20px', borderRadius: '16px', color: 'white' }}>
                    <h4>📅 مبيعات اليوم</h4>
                    <h2>{summary.today.toLocaleString()} د.ع</h2>
                    <p>عدد الطلبات: {summary.todayOrders}</p>
                </div>
                <div style={{ background: '#29B6F6', padding: '20px', borderRadius: '16px', color: 'white' }}>
                    <h4>📅 آخر 7 أيام</h4>
                    <h2>{summary.week.toLocaleString()} د.ع</h2>
                    <p>عدد الطلبات: {summary.weekOrders}</p>
                </div>
                <div style={{ background: '#7E57C2', padding: '20px', borderRadius: '16px', color: 'white' }}>
                    <h4>📅 الشهر الحالي</h4>
                    <h2>{summary.month.toLocaleString()} د.ع</h2>
                    <p>عدد الطلبات: {summary.monthOrders}</p>
                </div>
            </div>
        </div>
    );
}