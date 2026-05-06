import React, { useState, useEffect } from 'react';
import StudentView from './components/StudentView';
import AdminPanel from './components/AdminPanel';
import Auth from './components/Auth';
import { fetchTasks } from './api';

function App() {
    const [user, setUser] = useState(() => {
        const savedUser = localStorage.getItem("user");
        return savedUser ? JSON.parse(savedUser) : null;
    });

    const [tasks, setTasks] = useState([]);

    const refreshTasks = async () => {
        if (!user) return;
        try {
            const data = await fetchTasks();
            setTasks(data);
        } catch (err) {
            if (err.status === 401) {
                handleLogout();
            }
            console.error("Помилка завантаження задач:", err);
        }
    };

    useEffect(() => {
        if (user) {
            refreshTasks();
        }
    }, [user]);

    const handleLogout = () => {
        localStorage.removeItem("user");
        setUser(null);
        setTasks([]);
    };

    if (!user) {
        return <Auth onLogin={(userData) => setUser(userData)} />;
    }

    return (
        <div style={{ display: "flex", flexDirection: "column", height: "100vh", backgroundColor: "#1e1e1e", color: "white" }}>
            {/* Шапка сайту */}
            <header style={styles.header}>
                <div>
                    <b style={{ color: "#4db8ff", fontSize: "20px" }}>AutoCodeChecker Pro</b>
                    <span style={{ marginLeft: "20px", color: "#888", fontSize: "14px" }}>
                        👤 {user.User.FullName} ({user.User.Role === 1 ? "Викладач" : "Студент"})
                    </span>
                </div>
                <div>
                    <button onClick={handleLogout} style={styles.logoutBtn}>Вихід</button>
                </div>
            </header>

            {/* Контент залежно від ролі: 1 - Teacher, 0 - Student */}
            <main style={{ flex: 1, overflow: "hidden" }}>
                {user.User.Role === 1 ? (
                    <AdminPanel onTaskAdded={refreshTasks} />
                ) : (
                    <StudentView tasks={tasks} />
                )}
            </main>
        </div>
    );
}

const styles = {
    header: {
        padding: "10px 20px",
        background: "#333",
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
        borderBottom: "1px solid #444"
    },
    logoutBtn: {
        padding: "6px 12px",
        cursor: "pointer",
        border: "1px solid #f44",
        borderRadius: "4px",
        background: "transparent",
        color: "#f44",
        fontWeight: "bold"
    }
};

export default App;