import { useEffect, useState } from 'react';
import { getSalesSummary } from '../services/api';
import type { SalesSummaryDto } from '../models/reports';

export default function Dashboard() {
    const [summary, setSummary] = useState<SalesSummaryDto | null>(null);

    useEffect(() => {
        getSalesSummary().then(setSummary).catch(err => console.error(err));
    }, []);

    if (!summary)
        return (
            <div
                style={{
                    textAlign: 'center',
                    padding: '60px',
                    fontSize: '18px',
                    color: '#666'
                }}
            >
                جاري تحميل البيانات...
            </div>
        );

    const cardStyle = {
        background: "#fff",
        borderRadius: "18px",
        padding: "24px",
        boxShadow: "0 10px 30px rgba(0,0,0,.08)",
        transition: ".3s",
        cursor: "default"
    } as const;

    return (
        <div
            style={{
                padding: "30px",
                direction: "rtl",
                fontFamily: "Cairo, sans-serif",
                background: "#f4f6f9",
                minHeight: "100vh"
            }}
        >
            <div style={{ marginBottom: "30px" }}>
                <h1
                    style={{
                        margin: 0,
                        color: "#263238",
                        fontSize: "32px",
                        fontWeight: "bold"
                    }}
                >
                    📊 داشبورد حلويات البندقية
                </h1>

                <p
                    style={{
                        marginTop: "8px",
                        color: "#757575",
                        fontSize: "15px"
                    }}
                >
                    نظرة سريعة على أداء المبيعات الحالية
                </p>
            </div>

            <div
                style={{
                    display: "grid",
                    gridTemplateColumns: "repeat(auto-fit,minmax(300px,1fr))",
                    gap: "24px"
                }}
            >
                {/* Today */}
                <div
                    style={{
                        ...cardStyle,
                        borderTop: "6px solid #4CAF50"
                    }}
                >
                    <div
                        style={{
                            fontSize: "42px",
                            marginBottom: "15px"
                        }}
                    >
                        💰
                    </div>

                    <div
                        style={{
                            color: "#777",
                            fontSize: "15px"
                        }}
                    >
                        مبيعات اليوم
                    </div>

                    <div
                        style={{
                            fontSize: "34px",
                            fontWeight: "bold",
                            color: "#2E7D32",
                            margin: "15px 0"
                        }}
                    >
                        {summary.today.toLocaleString()} د.ع
                    </div>

                    <div
                        style={{
                            color: "#666",
                            fontSize: "15px"
                        }}
                    >
                        عدد الطلبات
                    </div>

                    <div
                        style={{
                            marginTop: "6px",
                            fontWeight: "bold",
                            fontSize: "22px"
                        }}
                    >
                        {summary.todayOrders}
                    </div>
                </div>

                {/* Week */}
                <div
                    style={{
                        ...cardStyle,
                        borderTop: "6px solid #2196F3"
                    }}
                >
                    <div
                        style={{
                            fontSize: "42px",
                            marginBottom: "15px"
                        }}
                    >
                        📈
                    </div>

                    <div
                        style={{
                            color: "#777",
                            fontSize: "15px"
                        }}
                    >
                        آخر 7 أيام
                    </div>

                    <div
                        style={{
                            fontSize: "34px",
                            fontWeight: "bold",
                            color: "#1565C0",
                            margin: "15px 0"
                        }}
                    >
                        {summary.week.toLocaleString()} د.ع
                    </div>

                    <div
                        style={{
                            color: "#666",
                            fontSize: "15px"
                        }}
                    >
                        عدد الطلبات
                    </div>

                    <div
                        style={{
                            marginTop: "6px",
                            fontWeight: "bold",
                            fontSize: "22px"
                        }}
                    >
                        {summary.weekOrders}
                    </div>
                </div>

                {/* Month */}
                <div
                    style={{
                        ...cardStyle,
                        borderTop: "6px solid #7E57C2"
                    }}
                >
                    <div
                        style={{
                            fontSize: "42px",
                            marginBottom: "15px"
                        }}
                    >
                        🏆
                    </div>

                    <div
                        style={{
                            color: "#777",
                            fontSize: "15px"
                        }}
                    >
                        الشهر الحالي
                    </div>

                    <div
                        style={{
                            fontSize: "34px",
                            fontWeight: "bold",
                            color: "#6A1B9A",
                            margin: "15px 0"
                        }}
                    >
                        {summary.month.toLocaleString()} د.ع
                    </div>

                    <div
                        style={{
                            color: "#666",
                            fontSize: "15px"
                        }}
                    >
                        عدد الطلبات
                    </div>

                    <div
                        style={{
                            marginTop: "6px",
                            fontWeight: "bold",
                            fontSize: "22px"
                        }}
                    >
                        {summary.monthOrders}
                    </div>
                </div>
            </div>
        </div>
    );
}