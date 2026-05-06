import React, { useState, useEffect } from 'react';
import StudentView from './components/StudentView';
import AdminPanel from './components/AdminPanel';
import { fetchTasks } from './api';

function App() {
    const [tasks, setTasks] = useState([]);
    const [view, setView] = useState("student"); // "student" або "admin"

    // Функція для оновлення списку задач із сервера
    const refreshTasks = async () => {
        try {
            const data = await fetchTasks();
            setTasks(data);
        } catch (error) {
            console.error("Помилка завантаження задач:", error);
        }
    };

    // Завантажуємо задачі один раз при старті додатку
    useEffect(() => {
        refreshTasks();
    }, []);

    return (
        <div style={appContainerStyle}>
            {/* Головна шапка сайту */}
            <header style={headerStyle}>
                <div style={{ display: "flex", alignItems: "center", gap: "10px" }}>
                    <span style={{ fontSize: "24px" }}>🚀</span>
                    <b style={{ color: "#4db8ff", fontSize: "20px", letterSpacing: "1px" }}>
                        AutoCodeChecker Pro
                    </b>
                </div>

                <nav style={{ display: "flex", gap: "10px" }}>
                    <button
                        onClick={() => setView("student")}
                        style={{ ...btnStyle, background: view === "student" ? "#007acc" : "#444" }}
                    >
                        👨‍🎓 Студент
                    </button>
                    <button
                        onClick={() => setView("admin")}
                        style={{ ...btnStyle, background: view === "admin" ? "#007acc" : "#444" }}
                    >
                        ⚙️ Адмінка
                    </button>
                </nav>
            </header>

            {/* Контентна область: перемикання між Студентом та Адмінкою */}
            <main style={{ flex: 1, overflow: "hidden", display: "flex", flexDirection: "column" }}>
                {view === "student" ? (
                    <StudentView tasks={tasks} />
                ) : (
                    <AdminPanel onTaskAdded={refreshTasks} />
                )}
            </main>
        </div>
    );
}

// --- Стилі ---

const appContainerStyle = {
    display: "flex",
    flexDirection: "column",
    height: "100vh",
    backgroundColor: "#121212", // Темний фон для всього додатку
    color: "white",
    fontFamily: "'Segoe UI', Roboto, Helvetica, Arial, sans-serif"
};

const headerStyle = {
    padding: "12px 25px",
    background: "#1e1e1e",
    display: "flex",
    justifyContent: "space-between",
    alignItems: "center",
    borderBottom: "1px solid #333",
    boxShadow: "0 2px 10px rgba(0,0,0,0.3)",
    zIndex: 10
};

const btnStyle = {
    padding: "8px 18px",
    cursor: "pointer",
    border: "none",
    borderRadius: "6px",
    color: "white",
    fontWeight: "600",
    transition: "all 0.2s ease",
    fontSize: "14px",
    display: "flex",
    alignItems: "center",
    gap: "5px"
};

export default App;