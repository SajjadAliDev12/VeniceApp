import React, { useEffect, useState } from 'react';
import { getProducts, getCategories, addProduct, updateProduct, deleteProduct } from '../services/api';
import type { Product, Category } from '../models/products';
import '../components/ResponsiveTable.css'; // 🌟 استدعاء ملف الأنماط الموحد

export default function Products() {
    const [products, setProducts] = useState<Product[]>([]);
    const [categories, setCategories] = useState<Category[]>([]);
    const [loading, setLoading] = useState(true);
    
    // إعدادات الـ Modal
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editingProduct, setEditingProduct] = useState<Product | null>(null);
    
    // قيم الحقول المدخلة
    const [name, setName] = useState('');
    const [price, setPrice] = useState(0);
    const [categoryId, setCategoryId] = useState(0);
    const [isAvailable, setIsAvailable] = useState(true);
    const [isKitchenItem, setIsKitchenItem] = useState(false);

    useEffect(() => {
        loadData();
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
            <div className="page-container" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
                ⏳ جاري تحميل قائمة المنتجات...
            </div>
        );

    return (
        <div className="page-container">
            {/* Header */}
            <div className="page-header">
                <div>
                    <h2 className="page-title">🛍️ إدارة قوائم المنتجات</h2>
                    <p className="page-subtitle">إدارة المنتجات والأسعار والتصنيفات داخل النظام</p>
                </div>
                <button onClick={openAddModal} className="btn-primary">
                    ➕ إضافة منتج جديد
                </button>
            </div>

            {/* الجدول (سيعمل كجدول على الديسكتوب وكـ Cards تلقائياً على الموبايل بفضل الـ CSS) */}
            <div className="table-container">
                <table className="data-table">
                    <thead>
                        <tr>
                            <th>اسم المادة</th>
                            <th>التصنيف</th>
                            <th>السعر</th>
                            <th>الحالة</th>
                            <th style={{ textAlign: 'center' }}>العمليات</th>
                        </tr>
                    </thead>
                    <tbody>
                        {products.map(p => (
                            <tr key={p.id} style={{ background: !p.isAvailable ? '#FEF2F2' : undefined }}>
                                <td data-label="اسم المادة" style={{ fontWeight: 'bold', color: '#1E293B' }}>
                                    {p.name}
                                </td>
                                <td data-label="التصنيف" style={{ color: '#64748B' }}>
                                    {p.categoryName}
                                </td>
                                <td data-label="السعر" style={{ color: '#15803D', fontWeight: 'bold', whiteSpace: 'nowrap' }}>
                                    {p.price.toLocaleString()} د.ع
                                </td>
                                <td data-label="الحالة">
                                    <span
                                        className="badge"
                                        style={{
                                            background: p.isAvailable ? '#DCFCE7' : '#FEE2E2',
                                            color: p.isAvailable ? '#15803D' : '#B91C1C'
                                        }}
                                    >
                                        {p.isAvailable ? 'متوفر' : 'غير متوفر'}
                                    </span>
                                </td>
                                <td className="actions-cell">
                                    <div style={{ display: 'flex', gap: '8px', width: '100%', justifyContent: 'center' }}>
                                        <button onClick={() => openEditModal(p)} className="btn-warning">
                                            ✏ تعديل
                                        </button>
                                        <button onClick={() => handleDelete(p.id)} className="btn-danger">
                                            🗑 حذف
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {/* Modal */}
            {isModalOpen && (
                <div className="modal-overlay">
                    <form onSubmit={handleSubmit} className="modal-card">
                        <h3 style={{ marginTop: 0, marginBottom: '20px', color: '#1E293B' }}>
                            {editingProduct ? '✏ تعديل المنتج' : '➕ إضافة منتج جديد'}
                        </h3>

                        <div className="form-group">
                            <label className="form-label">اسم المادة</label>
                            <input
                                type="text"
                                className="form-input"
                                value={name}
                                onChange={e => setName(e.target.value)}
                                required
                            />
                        </div>

                        <div className="form-group">
                            <label className="form-label">السعر (د.ع)</label>
                            <input
                                type="number"
                                className="form-input"
                                value={price}
                                onChange={e => setPrice(Number(e.target.value))}
                                required
                            />
                        </div>

                        <div className="form-group">
                            <label className="form-label">تصنيف المادة</label>
                            <select
                                className="form-select"
                                value={categoryId}
                                onChange={e => setCategoryId(Number(e.target.value))}
                            >
                                {categories.map(c => (
                                    <option key={c.id} value={c.id}>
                                        {c.name}
                                    </option>
                                ))}
                            </select>
                        </div>

                        <div style={{ display: 'flex', gap: '12px', marginBottom: '24px', flexWrap: 'wrap' }}>
                            <label
                                style={{
                                    display: 'flex',
                                    alignItems: 'center',
                                    gap: '8px',
                                    cursor: 'pointer',
                                    background: '#F8FAFC',
                                    padding: '12px',
                                    borderRadius: '10px',
                                    border: '1px solid #E2E8F0',
                                    fontSize: '14px',
                                    flex: '1 1 120px'
                                }}
                            >
                                <input
                                    type="checkbox"
                                    checked={isAvailable}
                                    onChange={e => setIsAvailable(e.target.checked)}
                                    style={{ width: '18px', height: '18px' }}
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
                                    padding: '12px',
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
                                    style={{ width: '18px', height: '18px' }}
                                />
                                مادة مطبخ
                            </label>
                        </div>

                        <div style={{ display: 'flex', gap: '12px' }}>
                            <button type="submit" className="btn-primary" style={{ flex: 1 }}>
                                💾 حفظ البيانات
                            </button>
                            <button
                                type="button"
                                onClick={() => setIsModalOpen(false)}
                                className="btn-danger"
                                style={{ flex: 1, background: '#94A3B8' }}
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