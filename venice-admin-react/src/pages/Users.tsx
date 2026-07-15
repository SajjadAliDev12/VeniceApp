import React, { useEffect, useState } from 'react';
import { getUsers, createUser, updateUser, disableUser } from '../services/api';
import type { UserRow } from '../models/UserRow';
export default function Users() {
    const [users, setUsers] = useState<UserRow[]>([]);
    const [loading, setLoading] = useState(true);
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [editingUser, setEditingUser] = useState<UserRow | null>(null);

    // الحقول
    const [username, setUsername] = useState('');
    const [fullName, setFullName] = useState('');
    const [emailAddress, setEmailAddress] = useState('');
    const [role, setRole] = useState(2); // الافتراضي Cashier
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
        setPassword(''); // تترك فارغة إلا في حال رغب المدير بتغيير الباسورد
        setIsModalOpen(true);
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        const payload: any = { username, fullName, emailAddress, role };
        if (password) payload.password = password; // إرسال الباسورد فقط إذا كُتب

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

    if (loading) return <p style={{ textAlign: 'center', padding: '20px' }}>جاري تحميل قائمة المستخدمين...</p>;

    return (
        <div style={{ padding: '20px', direction: 'rtl', fontFamily: 'sans-serif', background: '#F2F5F8', minHeight: '100vh' }}>
            <div style={{ display: 'flex', justifyContent: 'between', alignItems: 'center', marginBottom: '20px', gap: '10px' }}>
                <h2 style={{ color: '#37474F', margin: 0 }}>👥 إدارة الموظفين والصلاحيات</h2>
                <button onClick={openAddModal} style={{ padding: '10px 20px', background: '#7E57C2', color: 'white', border: 'none', borderRadius: '12px', fontWeight: 'bold', cursor: 'pointer' }}>➕ إضافة مستخدم جديد</button>
            </div>

            <div style={{ background: 'white', borderRadius: '16px', boxShadow: '0 4px 15px rgba(0,0,0,0.05)', overflowX: 'auto', padding: '10px' }}>
                <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'right' }}>
                    <thead>
                        <tr style={{ background: '#37474F', color: 'white' }}>
                            <th style={{ padding: '12px' }}>الاسم الكامل</th>
                            <th style={{ padding: '12px' }}>اسم المستخدم</th>
                            <th style={{ padding: '12px' }}>الصلاحية</th>
                            <th style={{ padding: '12px', textAlign: 'center' }}>العمليات</th>
                        </tr>
                    </thead>
                    <tbody>
                        {users.map(u => (
                            <tr key={u.id} style={{ borderBottom: '1px solid #ECEFF1', background: u.role === 3 ? '#FFEBEE' : 'transparent' }}>
                                <td style={{ padding: '12px', fontWeight: 'bold' }}>{u.fullName}</td>
                                <td style={{ padding: '12px' }}>{u.username}</td>
                                <td style={{ padding: '12px' }}>
                                    <span style={{ padding: '4px 8px', borderRadius: '6px', fontSize: '12px', fontWeight: 'bold', background: u.role === 1 ? '#E8F5E9' : u.role === 2 ? '#E3F2FD' : '#FFEBEE', color: u.role === 1 ? '#2E7D32' : u.role === 2 ? '#1565C0' : '#C62828' }}>
                                        {u.role === 1 ? 'مدير نظام' : u.role === 2 ? 'كاشير' : 'حساب معطل'}
                                    </span>
                                </td>
                                <td style={{ padding: '12px', textAlign: 'center' }}>
                                    <button onClick={() => openEditModal(u)} style={{ padding: '6px 12px', background: '#FB8C00', color: 'white', border: 'none', borderRadius: '8px', marginLeft: '5px', cursor: 'pointer' }}>📝 تعديل</button>
                                    {u.role !== 3 && (
                                        <button onClick={() => handleDisable(u.id)} style={{ padding: '6px 12px', background: '#E53935', color: 'white', border: 'none', borderRadius: '8px', cursor: 'pointer' }}>🚫 تعطيل</button>
                                    )}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>

            {/* نافذة الإضافة والتعديل */}
            {isModalOpen && (
                <div style={{ position: 'fixed', top: 0, left: 0, right: 0, bottom: 0, background: 'rgba(0,0,0,0.5)', display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 1000 }}>
                    <form onSubmit={handleSubmit} style={{ background: 'white', padding: '24px', borderRadius: '18px', width: '90%', maxWidth: '400px' }}>
                        <h3 style={{ color: '#37474F', marginBottom: '15px' }}>{editingUser ? '📝 تعديل حساب موظف' : '➕ إنشاء حساب جديد'}</h3>
                        
                        <div style={{ marginBottom: '12px' }}>
                            <label style={{ display: 'block', marginBottom: '4px', color: '#546E7A' }}>الاسم الكامل</label>
                            <input type="text" value={fullName} onChange={e => setFullName(e.target.value)} style={{ width: '100%', padding: '10px', borderRadius: '8px', border: '1px solid #CFD8DC' }} required />
                        </div>

                        <div style={{ marginBottom: '12px' }}>
                            <label style={{ display: 'block', marginBottom: '4px', color: '#546E7A' }}>اسم المستخدم (Login Name)</label>
                            <input type="text" value={username} onChange={e => setUsername(e.target.value)} style={{ width: '100%', padding: '10px', borderRadius: '8px', border: '1px solid #CFD8DC' }} required disabled={!!editingUser} />
                        </div>

                        <div style={{ marginBottom: '12px' }}>
                            <label style={{ display: 'block', marginBottom: '4px', color: '#546E7A' }}>كلمة المرور {editingUser && '(اكتبها فقط إذا أردت تغييرها)'}</label>
                            <input type="password" value={password} onChange={e => setPassword(e.target.value)} style={{ width: '100%', padding: '10px', borderRadius: '8px', border: '1px solid #CFD8DC' }} required={!editingUser} />
                        </div>

                        <div style={{ marginBottom: '15px' }}>
                            <label style={{ display: 'block', marginBottom: '4px', color: '#546E7A' }}>الصلاحية</label>
                            <select value={role} onChange={e => setRole(Number(e.target.value))} style={{ width: '100%', padding: '10px', borderRadius: '8px', border: '1px solid #CFD8DC', background: 'white' }}>
                                <option value={2}>كاشير (Cashier)</option>
                                <option value={1}>مدير نظام (Admin)</option>
                            </select>
                        </div>

                        <div style={{ display: 'flex', gap: '10px' }}>
                            <button type="submit" style={{ flex: 1, padding: '10px', background: '#7E57C2', color: 'white', border: 'none', borderRadius: '8px', fontWeight: 'bold', cursor: 'pointer' }}>حفظ البيانات</button>
                            <button type="button" onClick={() => setIsModalOpen(false)} style={{ flex: 1, padding: '10px', background: '#90A4AE', color: 'white', border: 'none', borderRadius: '8px', fontWeight: 'bold', cursor: 'pointer' }}>إلغاء</button>
                        </div>
                    </form>
                </div>
            )}
        </div>
    );
}