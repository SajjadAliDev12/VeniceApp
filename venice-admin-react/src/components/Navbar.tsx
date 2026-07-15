import { useNavigate, Link } from 'react-router-dom';
import { useEffect, useState } from 'react';

export default function Navbar() {
    const navigate = useNavigate();

    const userString = localStorage.getItem('user');
    const user = userString ? JSON.parse(userString) : null;

    const [isMobile, setIsMobile] = useState(window.innerWidth < 768);
    const [menuOpen, setMenuOpen] = useState(false);

    useEffect(() => {
        const resize = () => {
            setIsMobile(window.innerWidth < 768);

            if (window.innerWidth >= 768) {
                setMenuOpen(false);
            }
        };

        window.addEventListener("resize", resize);

        return () => window.removeEventListener("resize", resize);
    }, []);

    const handleLogout = () => {
        localStorage.clear();
        navigate('/');
    };

    const closeMenu = () => {
        setMenuOpen(false);
    };

    const linkStyle = {
        color: '#37474F',
        textDecoration: 'none',
        fontWeight: 600,
        fontSize: '15px',
        padding: '10px 16px',
        borderRadius: '10px',
        transition: '.25s'
    };

    const mobileLinkStyle = {
        display: 'block',
        padding: '16px 20px',
        textDecoration: 'none',
        color: '#37474F',
        fontWeight: 'bold',
        fontSize: '16px',
        borderBottom: '1px solid #ECEFF1'
    };

    return (
    <>
        <nav
            style={{
                background: "#ffffff",
                borderBottom: "1px solid #E5E7EB",
                boxShadow: "0 4px 18px rgba(0,0,0,.06)",
                padding: "14px 24px",
                display: "flex",
                justifyContent: "space-between",
                alignItems: "center",
                direction: "rtl",
                position: "sticky",
                top: 0,
                zIndex: 1000,
                fontFamily: "Cairo,sans-serif"
            }}
        >
            {/* الشعار */}
            <div
                style={{
                    display: "flex",
                    alignItems: "center",
                    gap: 14
                }}
            >
                <div
                    style={{
                        width: 46,
                        height: 46,
                        borderRadius: 12,
                        background: "linear-gradient(135deg,#4CAF50,#2E7D32)",
                        display: "flex",
                        justifyContent: "center",
                        alignItems: "center",
                        color: "#fff",
                        fontSize: 22
                    }}
                >
                    🍬
                </div>

                <div>
                    <div
                        style={{
                            fontWeight: "bold",
                            fontSize: 20,
                            color: "#263238"
                        }}
                    >
                        حلويات البندقية
                    </div>

                    {!isMobile && (
                        <div
                            style={{
                                fontSize: 12,
                                color: "#90A4AE"
                            }}
                        >
                            Sales Management System
                        </div>
                    )}
                </div>
            </div>

            {/* القائمة في الكمبيوتر */}
            {!isMobile && (
                <>
                    <div
                        style={{
                            display: "flex",
                            gap: 8
                        }}
                    >
                        <Link to="/dashboard" style={linkStyle}>
                            📊 الداشبورد
                        </Link>

                        <Link to="/products" style={linkStyle}>
                            🍬 المنتجات
                        </Link>

                        {user?.role === 1 && (
                            <Link to="/users" style={linkStyle}>
                                👥 المستخدمين
                            </Link>
                        )}
                    </div>

                    <div
                        style={{
                            display: "flex",
                            alignItems: "center",
                            gap: 18
                        }}
                    >
                        <div
                            style={{
                                display: "flex",
                                alignItems: "center",
                                gap: 10,
                                background: "#F4F6F9",
                                padding: "8px 16px",
                                borderRadius: 30
                            }}
                        >
                            <div
                                style={{
                                    width: 38,
                                    height: 38,
                                    borderRadius: "50%",
                                    background: "#2196F3",
                                    display: "flex",
                                    justifyContent: "center",
                                    alignItems: "center",
                                    color: "#fff"
                                }}
                            >
                                👤
                            </div>

                            <div>
                                <div
                                    style={{
                                        fontWeight: "bold",
                                        color: "#37474F"
                                    }}
                                >
                                    {user?.fullName || user?.username}
                                </div>

                                <div
                                    style={{
                                        fontSize: 12,
                                        color: "#90A4AE"
                                    }}
                                >
                                    {user?.role === 1
                                        ? "مدير النظام"
                                        : user?.role === 2
                                        ? "مدير"
                                        : "مستخدم"}
                                </div>
                            </div>
                        </div>

                        <button
                            onClick={handleLogout}
                            style={{
                                background: "#EF5350",
                                color: "#fff",
                                border: "none",
                                borderRadius: 12,
                                padding: "10px 20px",
                                fontWeight: "bold",
                                cursor: "pointer"
                            }}
                        >
                            🚪 تسجيل الخروج
                        </button>
                    </div>
                </>
            )}

            {/* زر القائمة في الموبايل */}
            {isMobile && (
                <button
                    onClick={() => setMenuOpen(true)}
                    style={{
                        background: "transparent",
                        border: "none",
                        fontSize: 30,
                        cursor: "pointer",
                        color: "#37474F"
                    }}
                >
                    ☰
                </button>
            )}
        </nav>

        {/* Overlay */}
        {isMobile && menuOpen && (
            <div
                onClick={closeMenu}
                style={{
                    position: "fixed",
                    inset: 0,
                    background: "rgba(0,0,0,.45)",
                    zIndex: 1998
                }}
            />
        )}

        {/* Drawer */}
        <div
            style={{
                position: "fixed",
                top: 0,
                right: menuOpen ? 0 : "-300px",
                width: 280,
                height: "100%",
                background: "#fff",
                transition: ".3s",
                boxShadow: "-5px 0 20px rgba(0,0,0,.18)",
                zIndex: 1999,
                overflowY: "auto"
            }}
        >
            <div
                style={{
                    padding: 24,
                    background: "#2E7D32",
                    color: "#fff"
                }}
            >
                <div style={{ fontSize: 22 }}>🍬</div>

                <div
                    style={{
                        marginTop: 10,
                        fontWeight: "bold",
                        fontSize: 20
                    }}
                >
                    حلويات البندقية
                </div>

                <div
                    style={{
                        marginTop: 6,
                        opacity: .85
                    }}
                >
                    {user?.fullName || user?.username}
                </div>
            </div>

            <Link
                to="/dashboard"
                onClick={closeMenu}
                style={mobileLinkStyle}
            >
                📊 الداشبورد
            </Link>

            <Link
                to="/products"
                onClick={closeMenu}
                style={mobileLinkStyle}
            >
                🍬 المنتجات
            </Link>

            {user?.role === 1 && (
                <Link
                    to="/users"
                    onClick={closeMenu}
                    style={mobileLinkStyle}
                >
                    👥 المستخدمين
                </Link>
            )}

            <div style={{ padding: 20 }}>
                <button
                    onClick={handleLogout}
                    style={{
                        width: "100%",
                        padding: 14,
                        border: "none",
                        borderRadius: 12,
                        background: "#EF5350",
                        color: "#fff",
                        fontWeight: "bold",
                        cursor: "pointer"
                    }}
                >
                    🚪 تسجيل الخروج
                </button>
            </div>
        </div>
    </>
);
}