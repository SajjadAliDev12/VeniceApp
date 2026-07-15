import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { login } from '../services/api';

export default function Login() {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const navigate = useNavigate();

    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            const data = await login(username, password);
            localStorage.setItem('token', data.token);
            localStorage.setItem('user', JSON.stringify(data.user));
            navigate('/dashboard');
        } catch (err) {
            setError('خطأ في اسم المستخدم أو كلمة المرور');
        }
    };

    return (
        <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh', background: '#F2F5F8', direction: 'rtl' }}>
            <form onSubmit={handleLogin} style={{ background: 'white', padding: '30px', borderRadius: '16px', boxShadow: '0 4px 15px rgba(0,0,0,0.05)', width: '320px' }}>
                <h3 style={{ textAlign: 'center', marginBottom: '20px', color: '#37474F' }}>🔐 دخول الإدارة</h3>
                {error && <p style={{ color: '#E53935', fontSize: '14px', textAlign: 'center' }}>{error}</p>}
                <div style={{ marginBottom: '15px' }}>
                    <label style={{ display: 'block', marginBottom: '5px', color: '#546E7A' }}>اسم المستخدم</label>
                    <input type="text" value={username} onChange={e => setUsername(e.target.value)} style={{ width: '100%', padding: '10px', borderRadius: '8px', border: '1px solid #CFD8DC' }} required />
                </div>
                <div style={{ marginBottom: '20px' }}>
                    <label style={{ display: 'block', marginBottom: '5px', color: '#546E7A' }}>كلمة المرور</label>
                    <input type="password" value={password} onChange={e => setPassword(e.target.value)} style={{ width: '100%', padding: '10px', borderRadius: '8px', border: '1px solid #CFD8DC' }} required />
                </div>
                <button type="submit" style={{ width: '100%', padding: '12px', background: '#7E57C2', color: 'white', border: 'none', borderRadius: '8px', fontWeight: 'bold', cursor: 'pointer' }}>دخول</button>
            </form>
        </div>
    );
}