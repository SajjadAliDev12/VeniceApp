import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Products from './pages/Products';
import Users from './pages/Users'; // 🌟 استيراد الصفحة الجديدة
import Navbar from './components/Navbar'; // 🌟 استيراد الـ Navbar المشترك

// مكوّن الحماية يضمن وجود التوكن ويضيف الـ Navbar تلقائياً للصفحات الداخلية
function ProtectedLayout({ children }: { children: JSX.Element }) {
    const token = localStorage.getItem('token');
    if (!token) return <Navigate to="/" replace />;
    
    return (
        <>
            <Navbar /> {/* شريط التنقل يظهر هنا دائماً فوق الصفحات الداخلية */}
            {children}
        </>
    );
}

export default function App() {
    return (
        <Router>
            <Routes>
                {/* صفحة الدخول بدون شريط تنقل */}
                <Route path="/" element={<Login />} />

                {/* الصفحات الداخلية المحمية تظهر تحت الـ Layout المشترك تلقائياً */}
                <Route path="/dashboard" element={<ProtectedLayout><Dashboard /></ProtectedLayout>} />
                <Route path="/products" element={<ProtectedLayout><Products /></ProtectedLayout>} />
                <Route path="/users" element={<ProtectedLayout><Users /></ProtectedLayout>} />
            </Routes>
        </Router>
    );
}