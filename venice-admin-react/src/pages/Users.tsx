import React, { useEffect, useState } from 'react';
import { getUsers, createUser, updateUser, disableUser } from '../services/api';
import type { UserRow } from '../models/UserRow';
import '../components/ResponsiveTable.css'; // 🌟 استدعاء ملف الأنماط الموحد

export default function Users() {
    const [users, setUsers] = useState<UserRow[]>([]);
    const [loading, setLoading] = useState(true);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editingUser, setEditingUser] = useState<UserRow | null>(null);

    // الحقول
    const [username, setUsername] = useState('');
    const [fullName, setFullName] = useState('');
    const [emailAddress, setEmailAddress] = useState('');
    const [role, setRole] = useState(0);
    const [password, setPassword] = useState('');

    useEffect(() => {
        loadUsers();
    }, []);

    const loadUsers = async () => {
        try {
            const data = await getUsers();
            setUsers(data);
            setLoading(false);
        } catch (err) {
            console.error(err);
            setLoading(false);
        }
    };

    const openAddModal = () => {
        setEditingUser(null);
        setUsername('');
        setFullName('');
        setEmailAddress('');
        setRole(2);
        setPassword('');
        setIsModalOpen(true);
    };

    const openEditModal = (u: UserRow) => {
        setEditingUser(u);
        setUsername(u.username);
        setFullName(u.fullName);
        setEmailAddress(u.emailAddress || '');
        setRole(u.role);
        setPassword('');
        setIsModalOpen(true);
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        const payload: any = { username, fullName, emailAddress, role };
        if (password) payload.password = password;

        try {
            if (editingUser) {
                await updateUser(editingUser.id, payload);
            } else {
                await createUser(payload);
            }
            setIsModalOpen(false);
            loadUsers();
        } catch (err) {
            alert('فشلت العملية، تحقق من عدم تكرار اسم المستخدم');
        }
    };

    const handleDisable = async (id: number) => {
        if (window.confirm('هل أنت متأكد من تعطيل هذا الحساب؟ لن يتمكن من تسجيل الدخول.')) {
            try {
                await disableUser(id);
                loadUsers();
            } catch (err) {
                console.error(err);
            }
        }
    };

    if (loading)
        return (
            <div className="page-container" style={{ display: 'flex', justifyContent: 'center', alignItems: 'center' }}>
                ⏳ جاري تحميل قائمة المستخدمين...
            </div>
        );

    return (
        <div className="page-container">
            {/* Header */}
            <div className="page-header">
                <div>
                    <h2 className="page-title">👥 إدارة الموظفين والصلاحيات</h2>
                    <p className="page-subtitle">إدارة حسابات المستخدمين وصلاحيات الوصول للنظام</p>
                </div>
                <button onClick={openAddModal} className="btn-primary">
                    ➕ إضافة مستخدم جديد
                </button>
            </div>

            {/* الجدول (سيعمل كجدول على الديسكتوب وكـ Cards تلقائياً على الموبايل بفضل الـ CSS) */}
            <div className="table-container">
                <table className="data-table">
                    <thead>
                        <tr>
                            <th>الاسم الكامل</th>
                            <th>اسم المستخدم</th>
                            <th>الصلاحية</th>
                            <th style={{ textAlign: 'center' }}>العمليات</th>
                        </tr>
                    </thead>
                    <tbody>
                        {users.map(u => (
                            <tr key={u.id} style={{ background: u.role === 0 ? '#FEF2F2' : undefined }}>
                                <td data-label="الاسم الكامل" style={{ fontWeight: 'bold', color: '#1E293B' }}>
                                    {u.fullName}
                                </td>
                                <td data-label="اسم المستخدم" style={{ color: '#475569' }}>
                                    {u.username}
                                </td>
                                <td data-label="الصلاحية">
                                    <span
                                        className="badge"
                                        style={{
                                            background: u.role === 1 ? '#DBEAFE' : u.role === 2 ? '#DCFCE7' : u.role === 3 ? '#FEF3C7' : '#FEE2E2',
                                            color: u.role === 1 ? '#1D4ED8' : u.role === 2 ? '#15803D' : u.role === 3 ? '#B45309' : '#B91C1C'
                                        }}
                                    >
                                        {u.role === 1 ? 'مدير نظام' : u.role === 2 ? 'مدير' : u.role === 3 ? 'كاشير' : 'حساب معطل'}
                                    </span>
                                </td>
                                <td className="actions-cell">
                                    <div style={{ display: 'flex', gap: '8px', width: '100%', justifyContent: 'center' }}>
                                        <button onClick={() => openEditModal(u)} className="btn-warning">
                                            ✏ تعديل
                                        </button>
                                        {u.role !== 0 && (
                                            <button onClick={() => handleDisable(u.id)} className="btn-danger">
                                                🚫 تعطيل
                                            </button>
                                        )}
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
                            {editingUser ? '✏ تعديل بيانات الموظف' : '➕ إنشاء مستخدم جديد'}
                        </h3>

                        <div className="form-group">
                            <label className="form-label">الاسم الكامل</label>
                            <input
                                type="text"
                                className="form-input"
                                value={fullName}
                                onChange={e => setFullName(e.target.value)}
                                required
                            />
                        </div>

                        <div className="form-group">
                            <label className="form-label">اسم المستخدم</label>
                            <input
                                type="text"
                                className="form-input"
                                value={username}
                                onChange={e => setUsername(e.target.value)}
                                disabled={!!editingUser}
                                required
                                style={{ background: editingUser ? '#F1F5F9' : '#fff' }}
                            />
                        </div>

                        <div className="form-group">
                            <label className="form-label">كلمة المرور {editingUser && '(اختياري)'}</label>
                            <input
                                type="password"
                                className="form-input"
                                value={password}
                                onChange={e => setPassword(e.target.value)}
                                required={!editingUser}
                            />
                        </div>

                        <div className="form-group" style={{ marginBottom: '24px' }}>
                            <label className="form-label">الصلاحية</label>
                            <select
                                className="form-select"
                                value={role}
                                onChange={e => setRole(Number(e.target.value))}
                            >
                                <option value={2}>مدير (Manager)</option>
                                <option value={1}>مدير نظام (Admin)</option>
                                <option value={3}>كاشير (Cashier)</option>
                                <option value={0}>معطل (Disabled)</option>
                            </select>
                        </div>

                        <div style={{ display: 'flex', gap: '12px' }}>
                            <button type="submit" className="btn-primary" style={{ flex: 1 }}>
                                💾 حفظ
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