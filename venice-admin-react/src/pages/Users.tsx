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
    const [role, setRole] = useState(0);
    const [password, setPassword] = useState('');

    // State محلي لتحديد ما إذا كانت الشاشة شاشة هاتف
    const [isMobile, setIsMobile] = useState(false);

    useEffect(() => {
        loadUsers();

        // فحص حجم الشاشة عند التحميل وعند تغيير المقاس
        const handleResize = () => {
            setIsMobile(window.innerWidth < 768);
        };
        handleResize();
        window.addEventListener('resize', handleResize);
        return () => window.removeEventListener('resize', handleResize);
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
            ⏳ جاري تحميل قائمة المستخدمين...
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
                marginBottom: '25px',
                gap: '15px'
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
                    👥 إدارة الموظفين والصلاحيات
                </h2>

                <p
                    style={{
                        marginTop: '6px',
                        marginBottom: 0,
                        color: '#64748B',
                        fontSize: isMobile ? '14px' : '15px'
                    }}
                >
                    إدارة حسابات المستخدمين وصلاحيات الوصول للنظام
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
                    cursor: 'pointer',
                    fontSize: isMobile ? '14px' : '15px',
                    boxShadow: '0 8px 18px rgba(37,99,235,.25)',
                    textAlign: 'center',
                    whiteSpace: 'nowrap'
                }}
            >
                ➕ إضافة مستخدم جديد
            </button>
        </div>

        {/* Table Container (Responsive) */}
        <div
            style={{
                background: '#fff',
                borderRadius: '18px',
                overflowX: 'auto', // تمرير أفقي على الشاشات الصغيرة لمنع تشوه التصميم
                WebkitOverflowScrolling: 'touch',
                boxShadow: '0 10px 30px rgba(15,23,42,.08)',
                border: '1px solid #E2E8F0',
                width: '100%'
            }}
        >
            <table
                style={{
                    width: '100%',
                    borderCollapse: 'collapse',
                    textAlign: 'right',
                    minWidth: '600px' // يحافظ على تباعد الأعمدة على الموبايل
                }}
            >
                <thead>
                    <tr
                        style={{
                            background: '#F1F5F9',
                            color: '#334155'
                        }}
                    >
                        <th style={{ padding: isMobile ? '14px 12px' : '18px', fontWeight: 700 }}>الاسم الكامل</th>
                        <th style={{ padding: isMobile ? '14px 12px' : '18px', fontWeight: 700 }}>اسم المستخدم</th>
                        <th style={{ padding: isMobile ? '14px 12px' : '18px', fontWeight: 700 }}>الصلاحية</th>
                        <th style={{ padding: isMobile ? '14px 12px' : '18px', textAlign: 'center', fontWeight: 700 }}>
                            العمليات
                        </th>
                    </tr>
                </thead>

                <tbody>
                    {users.map(u => (
                        <tr
                            key={u.id}
                            style={{
                                borderTop: '1px solid #EEF2F7',
                                background: u.role === 0 ? '#FEF2F2' : '#fff'
                            }}
                        >
                            <td
                                style={{
                                    padding: isMobile ? '14px 12px' : '18px',
                                    fontWeight: 'bold',
                                    color: '#1E293B'
                                }}
                            >
                                {u.fullName}
                            </td>

                            <td
                                style={{
                                    padding: isMobile ? '14px 12px' : '18px',
                                    color: '#475569'
                                }}
                            >
                                {u.username}
                            </td>

                            <td style={{ padding: isMobile ? '14px 12px' : '18px' }}>
                                <span
                                    style={{
                                        display: 'inline-block',
                                        padding: '4px 10px',
                                        borderRadius: '999px',
                                        fontWeight: 600,
                                        fontSize: '12px',
                                        whiteSpace: 'nowrap',
                                        background:
                                            u.role === 1
                                                ? '#DBEAFE'
                                                : u.role === 2
                                                ? '#DCFCE7'
                                                : u.role === 3
                                                ? '#FEF3C7'
                                                : '#FEE2E2',
                                        color:
                                            u.role === 1
                                                ? '#1D4ED8'
                                                : u.role === 2
                                                ? '#15803D'
                                                : u.role === 3
                                                ? '#B45309'
                                                : '#B91C1C'
                                    }}
                                >
                                    {u.role === 1
                                        ? 'مدير نظام'
                                        : u.role === 2
                                        ? 'مدير'
                                        : u.role === 3
                                        ? 'كاشير'
                                        : 'حساب معطل'}
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
                                        onClick={() => openEditModal(u)}
                                        style={{
                                            background: '#F59E0B',
                                            color: '#fff',
                                            border: 'none',
                                            borderRadius: '10px',
                                            padding: '8px 12px',
                                            cursor: 'pointer',
                                            fontWeight: 'bold',
                                            fontSize: '13px',
                                            whiteSpace: 'nowrap'
                                        }}
                                    >
                                        ✏ تعديل
                                    </button>

                                    {u.role !== 0 && (
                                        <button
                                            onClick={() => handleDisable(u.id)}
                                            style={{
                                                background: '#EF4444',
                                                color: '#fff',
                                                border: 'none',
                                                borderRadius: '10px',
                                                padding: '8px 12px',
                                                cursor: 'pointer',
                                                fontWeight: 'bold',
                                                fontSize: '13px',
                                                whiteSpace: 'nowrap'
                                            }}
                                        >
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
            <div
                style={{
                    position: 'fixed',
                    inset: 0,
                    background: 'rgba(15,23,42,.45)',
                    backdropFilter: 'blur(4px)',
                    display: 'flex',
                    justifyContent: 'center',
                    alignItems: 'center',
                    zIndex: 1000,
                    padding: '16px', // أمان للشاشات الصغيرة لتجنب الالتصاق التام بالحواف
                    boxSizing: 'border-box'
                }}
            >
                <form
                    onSubmit={handleSubmit}
                    style={{
                        background: '#fff',
                        borderRadius: '20px',
                        width: '100%',
                        maxWidth: '470px',
                        maxHeight: '90vh', // تضمن عدم تمدد الفورم خارج شاشة الموبايل
                        overflowY: 'auto', // تفعيل التمرير الداخلي للفورم عند صغر الشاشة
                        padding: isMobile ? '20px' : '28px',
                        boxShadow: '0 25px 60px rgba(0,0,0,.25)',
                        boxSizing: 'border-box'
                    }}
                >
                    <h3
                        style={{
                            marginTop: 0,
                            marginBottom: '20px',
                            color: '#1E293B',
                            fontSize: isMobile ? '20px' : '24px'
                        }}
                    >
                        {editingUser
                            ? '✏ تعديل بيانات الموظف'
                            : '➕ إنشاء مستخدم جديد'}
                    </h3>

                    <div style={{ marginBottom: '16px' }}>
                        <label style={{ display: 'block', marginBottom: '6px', fontWeight: 600, fontSize: '14px' }}>
                            الاسم الكامل
                        </label>

                        <input
                            type="text"
                            value={fullName}
                            onChange={e => setFullName(e.target.value)}
                            required
                            style={{
                                width: '100%',
                                padding: '12px',
                                borderRadius: '10px',
                                border: '1px solid #CBD5E1',
                                fontSize: '15px',
                                boxSizing: 'border-box',
                                outline: 'none'
                            }}
                        />
                    </div>

                    <div style={{ marginBottom: '16px' }}>
                        <label style={{ display: 'block', marginBottom: '6px', fontWeight: 600, fontSize: '14px' }}>
                            اسم المستخدم
                        </label>

                        <input
                            type="text"
                            value={username}
                            onChange={e => setUsername(e.target.value)}
                            disabled={!!editingUser}
                            required
                            style={{
                                width: '100%',
                                padding: '12px',
                                borderRadius: '10px',
                                border: '1px solid #CBD5E1',
                                fontSize: '15px',
                                boxSizing: 'border-box',
                                outline: 'none',
                                background: editingUser ? '#F1F5F9' : '#fff'
                            }}
                        />
                    </div>

                    <div style={{ marginBottom: '16px' }}>
                        <label style={{ display: 'block', marginBottom: '6px', fontWeight: 600, fontSize: '14px' }}>
                            كلمة المرور {editingUser && '(اختياري)'}
                        </label>

                        <input
                            type="password"
                            value={password}
                            onChange={e => setPassword(e.target.value)}
                            required={!editingUser}
                            style={{
                                width: '100%',
                                padding: '12px',
                                borderRadius: '10px',
                                border: '1px solid #CBD5E1',
                                fontSize: '15px',
                                boxSizing: 'border-box',
                                outline: 'none'
                            }}
                        />
                    </div>

                    <div style={{ marginBottom: '20px' }}>
                        <label style={{ display: 'block', marginBottom: '6px', fontWeight: 600, fontSize: '14px' }}>
                            الصلاحية
                        </label>

                        <select
                            value={role}
                            onChange={e => setRole(Number(e.target.value))}
                            style={{
                                width: '100%',
                                padding: '12px',
                                borderRadius: '10px',
                                border: '1px solid #CBD5E1',
                                fontSize: '15px',
                                background: '#FFFFFF',
                                boxSizing: 'border-box',
                                outline: 'none'
                            }}
                        >
                            <option value={2}>مدير (Manager)</option>
                            <option value={1}>مدير نظام (Admin)</option>
                            <option value={3}>كاشير (Cashier)</option>
                            <option value={0}>معطل (Disabled)</option>
                        </select>
                    </div>

                    <div style={{ display: 'flex', gap: '12px' }}>
                        <button
                            type="submit"
                            style={{
                                flex: 1,
                                background: '#2563EB',
                                color: '#fff',
                                border: 'none',
                                borderRadius: '10px',
                                padding: '12px',
                                fontWeight: 'bold',
                                fontSize: '15px',
                                cursor: 'pointer'
                            }}
                        >
                            💾 حفظ
                        </button>

                        <button
                            type="button"
                            onClick={() => setIsModalOpen(false)}
                            style={{
                                flex: 1,
                                background: '#94A3B8',
                                color: '#fff',
                                border: 'none',
                                borderRadius: '10px',
                                padding: '12px',
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