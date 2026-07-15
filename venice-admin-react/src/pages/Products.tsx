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

    if (loading) return <p style={{ textAlign: 'center', padding: '20px' }}>جاري تحميل قائمة المنتجات...</p>;

    return (
        <div style={{ padding: '20px', direction: 'rtl', fontFamily: 'sans-serif', background: '#F2F5F8', minHeight: '100vh' }}>
            <div style={{ display: 'flex', justifyContent: 'between', alignItems: 'center', marginBottom: '20px', flexWrap: 'wrap', gap: '10px' }}>
                <h2 style={{ color: '#37474F', margin: 0 }}>🛍️ إدارة قوائم المنتجات</h2>
                <button onClick={openAddModal} style={{ padding: '10px 20px', background: '#7E57C2', color: 'white', border: 'none', borderRadius: '12px', fontWeight: 'bold', cursor: 'pointer' }}>➕ إضافة منتج جديد</button>
            </div>

            {/* ستايل متجاوب: جدول للكمبيوتر يتحول تلقائياً لكروت مرنة على الشاشات الصغيرة للموبايل */}
            <div style={{ background: 'white', borderRadius: '16px', boxShadow: '0 4px 15px rgba(0,0,0,0.05)', overflowX: 'auto', padding: '10px' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right' }}>
                    <thead>
                        <tr style={{ background: '#37474F', color: 'white' }}>
                            <th style={{ padding: '12px' }}>اسم المادة</th>
                            <th style={{ padding: '12px' }}>التصنيف</th>
                            <th style={{ padding: '12px' }}>السعر</th>
                            <th style={{ padding: '12px' }}>الحالة</th>
                            <th style={{ padding: '12px', textAlign: 'center' }}>العمليات</th>
                        </tr>
                    </thead>
                    <tbody>
                        {products.map(p => (
                            <tr key={p.id} style={{ borderBottom: '1px solid #ECEFF1', background: p.isAvailable ? 'transparent' : '#FFEBEE' }}>
                                <td style={{ padding: '12px', fontWeight: 'bold', color: '#263238' }}>{p.name}</td>
                                <td style={{ padding: '12px', color: '#546E7A' }}>{p.categoryName}</td>
                                <td style={{ padding: '12px', color: '#2E7D32', fontWeight: 'bold' }}>{p.price.toLocaleString()} د.ع</td>
                                <td style={{ padding: '12px' }}>
                                    <span style={{ padding: '4px 8px', borderRadius: '6px', fontSize: '12px', fontWeight: 'bold', background: p.isAvailable ? '#E8F5E9' : '#FFEBEE', color: p.isAvailable ? '#2E7D32' : '#C62828' }}>
                                        {p.isAvailable ? 'متوفر' : 'غير متوفر'}
                                    </span>
                                </td>
                                <td style={{ padding: '12px', textAlign: 'center' }}>
                                    <button onClick={() => openEditModal(p)} style={{ padding: '6px 12px', background: '#FB8C00', color: 'white', border: 'none', borderRadius: '8px', marginLeft: '5px', cursor: 'pointer' }}>📝 تعديل</button>
                                    <button onClick={() => handleDelete(p.id)} style={{ padding: '6px 12px', background: '#E53935', color: 'white', border: 'none', borderRadius: '8px', cursor: 'pointer' }}>🗑️ حذف</button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {/* النافذة المنبثقة (Modal) للإضافة والتعديل */}
            {isModalOpen && (
                <div style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 1000 }}>
                    <form onSubmit={handleSubmit} style={{ background: 'white', padding: '24px', borderRadius: '18px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 20px rgba(0,0,0,0.15)' }}>
                        <h3 style={{ color: '#37474F', marginBottom: '15px' }}>{editingProduct ? '📝 تعديل المنتج' : '➕ إضافة منتج جديد'}</h3>
                        
                        <div style={{ marginBottom: '12px' }}>
                            <label style={{ display: 'block', marginBottom: '4px', color: '#546E7A' }}>اسم المادة</label>
                            <input type="text" value={name} onChange={e => setName(e.target.value)} style={{ width: '100%', padding: '10px', borderRadius: '8px', border: '1px solid #CFD8DC' }} required />
                        </div>

                        <div style={{ marginBottom: '12px' }}>
                            <label style={{ display: 'block', marginBottom: '4px', color: '#546E7A' }}>السعر (د.ع)</label>
                            <input type="number" value={price} onChange={e => setPrice(Number(e.target.value))} style={{ width: '100%', padding: '10px', borderRadius: '8px', border: '1px solid #CFD8DC' }} required />
                        </div>

                        <div style={{ marginBottom: '12px' }}>
                            <label style={{ display: 'block', marginBottom: '4px', color: '#546E7A' }}>تصنيف المادة</label>
                            <select value={categoryId} onChange={e => setCategoryId(Number(e.target.value))} style={{ width: '100%', padding: '10px', borderRadius: '8px', border: '1px solid #CFD8DC', background: 'white' }}>
                                {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
                            </select>
                        </div>

                        <div style={{ marginBottom: '15px', display: 'flex', gap: '20px' }}>
                            <label style={{ display: 'flex', alignItems: 'center', gap: '5px', cursor: 'pointer' }}>
                                <input type="checkbox" checked={isAvailable} onChange={e => setIsAvailable(e.target.checked)} /> متوفر للبيع
                            </label>
                            <label style={{ display: 'flex', alignItems: 'center', gap: '5px', cursor: 'pointer' }}>
                                <input type="checkbox" checked={isKitchenItem} onChange={e => setIsKitchenItem(e.target.checked)} /> مادة مطبخ
                            </label>
                        </div>

                        <div style={{ display: 'flex', gap: '10px' }}>
                            <button type="submit" style={{ flex: 1, padding: '10px', background: '#7E57C2', color: 'white', border: 'none', borderRadius: '8px', fontWeight: 'bold', cursor: 'pointer' }}>حفظ</button>
                            <button type="button" onClick={() => setIsModalOpen(false)} style={{ flex: 1, padding: '10px', background: '#90A4AE', color: 'white', border: 'none', borderRadius: '8px', fontWeight: 'bold', cursor: 'pointer' }}>إلغاء</button>
                        </div>
                    </form>
                </div>
            )}
        </div>
    );
}