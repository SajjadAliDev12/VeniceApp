import React, { useEffect, useState } from 'react';
import { getProducts, getCategories, addProduct, updateProduct, deleteProduct } from '../services/api';
import type { Product, Category } from '../models/products';

export default function Products() {
    const [products, setProducts] = useState<Product[]>([]);
    const [categories, setCategories] = useState<Category[]>([]);
    const [loading, setLoading] = useState(true);
    
    // إعدادات الـ Modal (نافذة الإضافة والتعديل)
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editingProduct, setEditingProduct] = useState<Product | null>(null);
    
    // قيم الحقول المدخلة
    const [name, setName] = useState('');
    const [price, setPrice] = useState(0);
    const [categoryId, setCategoryId] = useState(0);
    const [isAvailable, setIsAvailable] = useState(true);
    const [isKitchenItem, setIsKitchenItem] = useState(false);

    // إضافة State محلي لمعرفة حجم الشاشة من أجل تكييف بعض الخصائص ديناميكياً
    const [isMobile, setIsMobile] = useState(false);

    useEffect(() => {
        loadData();
        
        // التحقق من حجم الشاشة وتحديث الحالة
        const handleResize = () => {
            setIsMobile(window.innerWidth < 768);
        };
        handleResize(); // تشغيل أولي
        window.addEventListener('resize', handleResize);
        return () => window.removeEventListener('resize', handleResize);
    }, []);

    const loadData = async () => {
        try {
            const [prodData, catData] = await Promise.all([getProducts(), getCategories()]);
            setProducts(prodData);
            setCategories(catData);
            setLoading(false);
        } catch (err) {
            console.error('خطأ في جلب البيانات من السيرفر', err);
            setLoading(false);
        }
    };

    const openAddModal = () => {
        setEditingProduct(null);
        setName('');
        setPrice(0);
        setCategoryId(categories[0]?.id || 0);
        setIsAvailable(true);
        setIsKitchenItem(false);
        setIsModalOpen(true);
    };

    const openEditModal = (p: Product) => {
        setEditingProduct(p);
        setName(p.name);
        setPrice(p.price);
        setCategoryId(p.categoryId);
        setIsAvailable(p.isAvailable);
        setIsKitchenItem(p.isKitchenItem);
        setIsModalOpen(true);
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        const payload = { name, price, categoryId, isAvailable, isKitchenItem };
        try {
            if (editingProduct) {
                await updateProduct(editingProduct.id, payload);
            } else {
                await addProduct(payload);
            }
            setIsModalOpen(false);
            loadData();
        } catch (err) {
            alert('فشلت العملية، يرجى التحقق من المدخلات');
        }
    };

    const handleDelete = async (id: number) => {
        if (window.confirm('هل أنت متأكد من حذف أو تعطيل هذا المنتج؟')) {
            try {
                await deleteProduct(id);
                loadData();
            } catch (err) {
                console.error(err);
            }
        }
    };

if (loading)
    return (
        <div
            style={{
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center',
                minHeight: '60vh',
                direction: 'rtl',
                fontFamily: 'Cairo, sans-serif',
                color: '#64748B',
                fontSize: '18px',
                padding: '20px',
                textAlign: 'center'
            }}
        >
            ⏳ جاري تحميل قائمة المنتجات...
        </div>
    );

return (
    <div
        style={{
            padding: isMobile ? '16px' : '30px',
            direction: 'rtl',
            fontFamily: 'Cairo, sans-serif',
            background: '#F8FAFC',
            minHeight: '100vh',
            boxSizing: 'border-box'
        }}
    >
        {/* Header */}
        <div
            style={{
                display: 'flex',
                flexDirection: isMobile ? 'column' : 'row',
                justifyContent: 'space-between',
                alignItems: isMobile ? 'stretch' : 'center',
                gap: '15px',
                marginBottom: '25px'
            }}
        >
            <div>
                <h2
                    style={{
                        margin: 0,
                        color: '#1E293B',
                        fontSize: isMobile ? '22px' : '30px',
                        fontWeight: 700,
                        lineHeight: 1.3
                    }}
                >
                    🛍️ إدارة قوائم المنتجات
                </h2>

                <p
                    style={{
                        marginTop: '6px',
                        marginBottom: 0,
                        color: '#64748B',
                        fontSize: isMobile ? '14px' : '16px'
                    }}
                >
                    إدارة المنتجات والأسعار والتصنيفات داخل النظام
                </p>
            </div>

            <button
                onClick={openAddModal}
                style={{
                    background: '#2563EB',
                    color: '#fff',
                    border: 'none',
                    borderRadius: '12px',
                    padding: '12px 22px',
                    fontWeight: 'bold',
                    fontSize: isMobile ? '15px' : '16px',
                    cursor: 'pointer',
                    boxShadow: '0 8px 18px rgba(37,99,235,.25)',
                    textAlign: 'center',
                    whiteSpace: 'nowrap'
                }}
            >
                ➕ إضافة منتج جديد
            </button>
        </div>

        {/* Table Container with scroll support for small screens */}
        <div
            style={{
                background: '#fff',
                borderRadius: '18px',
                border: '1px solid #E2E8F0',
                boxShadow: '0 10px 30px rgba(15,23,42,.08)',
                width: '100%',
                overflowX: 'auto',
                WebkitOverflowScrolling: 'touch' // تمرير ناعم على أجهزة iOS
            }}
        >
            <table
                style={{
                    width: '100%',
                    borderCollapse: 'collapse',
                    textAlign: 'right',
                    minWidth: '600px' // يضمن عدم انضغاط الأعمدة بشكل سيء على شاشات الهواتف الضيقة
                }}
            >
                <thead>
                    <tr
                        style={{
                            background: '#F1F5F9',
                            color: '#334155'
                        }}
                    >
                        <th style={{ padding: isMobile ? '14px 12px' : '18px' }}>اسم المادة</th>
                        <th style={{ padding: isMobile ? '14px 12px' : '18px' }}>التصنيف</th>
                        <th style={{ padding: isMobile ? '14px 12px' : '18px' }}>السعر</th>
                        <th style={{ padding: isMobile ? '14px 12px' : '18px' }}>الحالة</th>
                        <th style={{ padding: isMobile ? '14px 12px' : '18px', textAlign: 'center' }}>
                            العمليات
                        </th>
                    </tr>
                </thead>

                <tbody>
                    {products.map(p => (
                        <tr
                            key={p.id}
                            style={{
                                borderTop: '1px solid #EEF2F7',
                                background: p.isAvailable ? '#fff' : '#FEF2F2'
                            }}
                        >
                            <td
                                style={{
                                    padding: isMobile ? '14px 12px' : '18px',
                                    fontWeight: 'bold',
                                    color: '#1E293B'
                                }}
                            >
                                {p.name}
                            </td>

                            <td
                                style={{
                                    padding: isMobile ? '14px 12px' : '18px',
                                    color: '#64748B'
                                }}
                            >
                                {p.categoryName}
                            </td>

                            <td
                                style={{
                                    padding: isMobile ? '14px 12px' : '18px',
                                    color: '#15803D',
                                    fontWeight: 'bold',
                                    whiteSpace: 'nowrap'
                                }}
                            >
                                {p.price.toLocaleString()} د.ع
                            </td>

                            <td style={{ padding: isMobile ? '14px 12px' : '18px' }}>
                                <span
                                    style={{
                                        display: 'inline-block',
                                        padding: '4px 10px',
                                        borderRadius: '999px',
                                        fontWeight: 600,
                                        fontSize: '12px',
                                        background: p.isAvailable
                                            ? '#DCFCE7'
                                            : '#FEE2E2',
                                        color: p.isAvailable
                                            ? '#15803D'
                                            : '#B91C1C',
                                        whiteSpace: 'nowrap'
                                    }}
                                >
                                    {p.isAvailable ? 'متوفر' : 'غير متوفر'}
                                </span>
                            </td>

                            <td
                                style={{
                                    padding: isMobile ? '14px 12px' : '18px',
                                    textAlign: 'center'
                                }}
                            >
                                <div style={{ 
                                    display: 'flex', 
                                    justifyContent: 'center', 
                                    gap: '6px',
                                    flexWrap: 'wrap'
                                }}>
                                    <button
                                        onClick={() => openEditModal(p)}
                                        style={{
                                            background: '#F59E0B',
                                            color: '#fff',
                                            border: 'none',
                                            borderRadius: '10px',
                                            padding: '8px 12px',
                                            fontWeight: 'bold',
                                            fontSize: '13px',
                                            cursor: 'pointer',
                                            whiteSpace: 'nowrap'
                                        }}
                                    >
                                        ✏ تعديل
                                    </button>

                                    <button
                                        onClick={() => handleDelete(p.id)}
                                        style={{
                                            background: '#EF4444',
                                            color: '#fff',
                                            border: 'none',
                                            borderRadius: '10px',
                                            padding: '8px 12px',
                                            fontWeight: 'bold',
                                            fontSize: '13px',
                                            cursor: 'pointer',
                                            whiteSpace: 'nowrap'
                                        }}
                                    >
                                        🗑 حذف
                                    </button>
                                </div>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>

        {/* النافذة المنبثقة (Modal) للإضافة والتعديل */}
        {isModalOpen && (
            <div
                style={{
                    position: 'fixed',
                    inset: 0,
                    background: 'rgba(15,23,42,.55)',
                    backdropFilter: 'blur(4px)',
                    display: 'flex',
                    justifyContent: 'center',
                    alignItems: 'center',
                    zIndex: 1000,
                    padding: '16px', // حواف أمان على الموبايل لمنع التصاق الـ Modal بالشاشة
                    boxSizing: 'border-box'
                }}
            >
                <form
                    onSubmit={handleSubmit}
                    style={{
                        background: '#FFFFFF',
                        width: '100%',
                        maxWidth: '500px',
                        maxHeight: '90vh', // منع اختفاء الأزرار خارج الشاشة على الهواتف
                        overflowY: 'auto', // تمكين التمرير الداخلي في حال زيادة طول المدخلات على الشاشات القصيرة
                        borderRadius: '20px',
                        boxShadow: '0 20px 60px rgba(0,0,0,.25)',
                        display: 'flex',
                        flexDirection: 'column'
                    }}
                >
                    {/* Header */}
                    <div
                        style={{
                            padding: isMobile ? '16px 20px' : '22px 24px',
                            borderBottom: '1px solid #E2E8F0',
                            background: '#F8FAFC'
                        }}
                    >
                        <h3
                            style={{
                                margin: 0,
                                color: '#1E293B',
                                fontSize: isMobile ? '20px' : '24px'
                            }}
                        >
                            {editingProduct ? '✏ تعديل المنتج' : '➕ إضافة منتج جديد'}
                        </h3>

                        <p
                            style={{
                                margin: '6px 0 0',
                                color: '#64748B',
                                fontSize: '13px'
                            }}
                        >
                            أدخل بيانات المنتج ثم اضغط حفظ.
                        </p>
                    </div>

                    {/* Body */}
                    <div style={{ padding: isMobile ? '16px 20px' : '24px', flex: 1 }}>

                        <div style={{ marginBottom: '16px' }}>
                            <label
                                style={{
                                    display: 'block',
                                    marginBottom: '6px',
                                    fontWeight: 600,
                                    color: '#334155',
                                    fontSize: '14px'
                                }}
                            >
                                اسم المادة
                            </label>

                            <input
                                type="text"
                                value={name}
                                onChange={e => setName(e.target.value)}
                                required
                                style={{
                                    width: '100%',
                                    padding: '12px 14px',
                                    borderRadius: '10px',
                                    border: '1px solid #CBD5E1',
                                    fontSize: '15px',
                                    boxSizing: 'border-box',
                                    outline: 'none'
                                }}
                            />
                        </div>

                        <div style={{ marginBottom: '16px' }}>
                            <label
                                style={{
                                    display: 'block',
                                    marginBottom: '6px',
                                    fontWeight: 600,
                                    color: '#334155',
                                    fontSize: '14px'
                                }}
                            >
                                السعر (د.ع)
                            </label>

                            <input
                                type="number"
                                value={price}
                                onChange={e => setPrice(Number(e.target.value))}
                                required
                                style={{
                                    width: '100%',
                                    padding: '12px 14px',
                                    borderRadius: '10px',
                                    border: '1px solid #CBD5E1',
                                    fontSize: '15px',
                                    boxSizing: 'border-box',
                                    outline: 'none'
                                }}
                            />
                        </div>

                        <div style={{ marginBottom: '18px' }}>
                            <label
                                style={{
                                    display: 'block',
                                    marginBottom: '6px',
                                    fontWeight: 600,
                                    color: '#334155',
                                    fontSize: '14px'
                                }}
                            >
                                تصنيف المادة
                            </label>

                            <select
                                value={categoryId}
                                onChange={e => setCategoryId(Number(e.target.value))}
                                style={{
                                    width: '100%',
                                    padding: '12px 14px',
                                    borderRadius: '10px',
                                    border: '1px solid #CBD5E1',
                                    background: '#FFFFFF',
                                    fontSize: '15px',
                                    boxSizing: 'border-box',
                                    outline: 'none'
                                }}
                            >
                                {categories.map(c => (
                                    <option key={c.id} value={c.id}>
                                        {c.name}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div
                            style={{
                                display: 'flex',
                                gap: '12px',
                                marginBottom: '12px',
                                flexWrap: 'wrap'
                            }}
                        >
                            <label
                                style={{
                                    display: 'flex',
                                    alignItems: 'center',
                                    gap: '8px',
                                    cursor: 'pointer',
                                    background: '#F8FAFC',
                                    padding: '10px 12px',
                                    borderRadius: '10px',
                                    border: '1px solid #E2E8F0',
                                    fontSize: '14px',
                                    flex: '1 1 120px' // السماح بتوزيع العناصر بمرونة ومحاذاة مناسبة على الهاتف
                                }}
                            >
                                <input
                                    type="checkbox"
                                    checked={isAvailable}
                                    onChange={e => setIsAvailable(e.target.checked)}
                                />
                                متوفر للبيع
                            </label>

                            <label
                                style={{
                                    display: 'flex',
                                    alignItems: 'center',
                                    gap: '8px',
                                    cursor: 'pointer',
                                    background: '#F8FAFC',
                                    padding: '10px 12px',
                                    borderRadius: '10px',
                                    border: '1px solid #E2E8F0',
                                    fontSize: '14px',
                                    flex: '1 1 120px'
                                }}
                            >
                                <input
                                    type="checkbox"
                                    checked={isKitchenItem}
                                    onChange={e => setIsKitchenItem(e.target.checked)}
                                />
                                مادة مطبخ
                            </label>
                        </div>

                    </div>

                    {/* Footer */}
                    <div
                        style={{
                            display: 'flex',
                            gap: '12px',
                            padding: isMobile ? '16px 20px' : '20px 24px',
                            borderTop: '1px solid #E2E8F0',
                            background: '#F8FAFC'
                        }}
                    >
                        <button
                            type="submit"
                            style={{
                                flex: 1,
                                padding: '12px',
                                background: '#2563EB',
                                color: '#FFFFFF',
                                border: 'none',
                                borderRadius: '10px',
                                fontWeight: 'bold',
                                fontSize: '15px',
                                cursor: 'pointer'
                            }}
                        >
                            💾 حفظ البيانات
                        </button>

                        <button
                            type="button"
                            onClick={() => setIsModalOpen(false)}
                            style={{
                                flex: 1,
                                padding: '12px',
                                background: '#94A3B8',
                                color: '#FFFFFF',
                                border: 'none',
                                borderRadius: '10px',
                                fontWeight: 'bold',
                                fontSize: '15px',
                                cursor: 'pointer'
                            }}
                        >
                            إلغاء
                        </button>
                    </div>
                </form>
            </div>
        )}
    </div>
);
}