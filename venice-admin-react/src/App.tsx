import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';

import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Products from './pages/Products';
import Users from './pages/Users';

import Navbar from './components/Navbar';

function ProtectedLayout({ children }: { children: JSX.Element }) {
    const token = localStorage.getItem('token');

    if (!token) {
        return <Navigate to="/" replace />;
    }

    return (
        <div
            style={{
                minHeight: '100vh',
                background: '#f4f6f9',
                direction: 'rtl'
            }}
        >
            <Navbar />

            <main
                style={{
                    padding: '25px',
                    maxWidth: '1600px',
                    margin: '0 auto'
                }}
            >
                {children}
            </main>
        </div>
    );
}

export default function App() {
    return (
        <Router>
            <Routes>
                {/* صفحة تسجيل الدخول */}
                <Route path="/" element={<Login />} />

                {/* الصفحات المحمية */}
                <Route
                    path="/dashboard"
                    element={
                        <ProtectedLayout>
                            <Dashboard />
                        </ProtectedLayout>
                    }
                />

                <Route
                    path="/products"
                    element={
                        <ProtectedLayout>
                            <Products />
                        </ProtectedLayout>
                    }
                />

                <Route
                    path="/users"
                    element={
                        <ProtectedLayout>
                            <Users />
                        </ProtectedLayout>
                    }
                />

                {/* أي رابط غير معروف */}
                <Route path="*" element={<Navigate to="/dashboard" replace />} />
            </Routes>
        </Router>
    );
}