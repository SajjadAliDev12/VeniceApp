import { useNavigate, Link } from 'react-router-dom';

export default function Navbar() {
    const navigate = useNavigate();
    const userString = localStorage.getItem('user');
    const user = userString ? JSON.parse(userString) : null;

    const handleLogout = () => {
        localStorage.clear(); // مسح التوكن وبيانات المستخدم تماماً
        navigate('/');
    };

    return (
        <nav style={{ background: '#37474F', padding: '15px 20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', color: 'white', direction: 'rtl', fontFamily: 'sans-serif' }}>
            <div style={{ display: 'flex', gap: '20px', alignItems: 'center' }}>
                <span style={{ fontWeight: 'bold', fontSize: '18px', marginLeft: '10px' }}>🛍️ البندقية</span>
                <Link to="/dashboard" style={{ color: 'white', textDecoration: 'none', fontWeight: 'bold' }}>📊 الداشبورد</Link>
                <Link to="/products" style={{ color: 'white', textDecoration: 'none', fontWeight: 'bold' }}>🍬 المنتجات</Link>
                {/* إظهار رابط المستخدمين للمدير فقط */}
                {user?.role === 1 && (
                    <Link to="/users" style={{ color: 'white', textDecoration: 'none', fontWeight: 'bold' }}>👥 المستخدمين</Link>
                )}
            </div>
            
            <div style={{ display: 'flex', alignItems: 'center', gap: '15px' }}>
                <span style={{ fontSize: '14px', background: '#455A64', padding: '5px 10px', borderRadius: '8px' }}>
                    👤 {user?.fullName || user?.username}
                </span>
                <button onClick={handleLogout} style={{ background: '#E53935', color: 'white', border: 'none', padding: '8px 16px', borderRadius: '8px', fontWeight: 'bold', cursor: 'pointer' }}>
                    🚪 خروج
                </button>
            </div>
        </nav>
    );
}