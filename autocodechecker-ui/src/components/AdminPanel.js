import React, { useState, useEffect } from 'react';
import Editor from "@monaco-editor/react";
import { fetchTasks, fetchTaskById, saveTaskToApi, deleteTaskFromApi, fetchTeacherResults, fetchTeacherGroups, fetchGroupStudents } from '../api';

const AdminPanel = ({ onTaskAdded }) => {
    const [tasks, setTasks] = useState([]);
    const [results, setResults] = useState([]);
    const [view, setView] = useState("tasks");
    const [groups, setGroups] = useState([]);
    const [search, setSearch] = useState("");
    const [groupFilter, setGroupFilter] = useState("");
    const [sortBy, setSortBy] = useState("date");
    const [selectedGroupStudents, setSelectedGroupStudents] = useState(null);

    const handleBtnHover = (e, color) => {
        e.target.style.background = color;
    };

    const handleBtnOut = (e, color) => {
        e.target.style.background = color;
    };

    const viewGroupStudents = async (groupId) => {
        try {
            const data = await fetchGroupStudents(groupId);
            setSelectedGroupStudents({
                groupId: groupId,
                list: data
            });
        } catch (e) {
            alert("Не вдалося завантажити список студентів");
        }
    };

    const [task, setTask] = useState({
        title: "",
        groupName: "",
        description: "### Умова задачі\n",
        initialCode: "using System;\nusing System.Collections.Generic;\n\npublic class Solution {\n    public int Execute(List<int> numbers) {\n        return 0;\n    }\n}",
        testCases: []
    });

    const [testIn, setTestIn] = useState("");
    const [testOut, setTestOut] = useState("");

    useEffect(() => {
        loadTasks();
        loadGroups();
        if (view === "journal") loadResults();
    }, [view, search, groupFilter, sortBy]);

    const loadGroups = async () => {
        const data = await fetchTeacherGroups();
        setGroups(data);
    };

    const [expandedFolders, setExpandedFolders] = useState({
        "Загальні завдання": true
    });

    const loadTasks = async () => setTasks(await fetchTasks());
    const loadResults = async () => {
        const data = await fetchTeacherResults(search, groupFilter, sortBy);
        setResults(data);
    };

    const handleEditClick = async (taskSummary) => {
        try {
            const id = taskSummary.Id || taskSummary.id;
            const fullTask = await fetchTaskById(id);
            setTask({
                ...fullTask,
                Id: id,
                title: fullTask.Title || fullTask.title || "",
                groupName: fullTask.GroupName || fullTask.groupName || "",
                description: fullTask.Description || fullTask.description || "",
                initialCode: fullTask.InitialCode || fullTask.initialCode || "",
                testCases: fullTask.TestCases || fullTask.testCases || []
            });
        } catch (error) {
            alert("Не вдалося завантажити дані задачі.");
        }
    };

    const handleSave = async () => {
        const title = task.title || task.Title;
        const currentTests = task.testCases || task.TestCases || [];
        if (!title || currentTests.length === 0) {
            alert("Заповніть назву та додайте тести!");
            return;
        }
        try {

            const payload = {
                Id: task.Id || task.id,
                Title: task.title || task.Title,
                GroupName: task.groupName || task.GroupName,
                Description: task.description || task.Description,
                InitialCode: task.initialCode || task.InitialCode,
                TestCases: task.testCases || task.TestCases || []
            };

            await saveTaskToApi(payload);

            alert((task.Id || task.id)
                ? "Задачу оновлено!"
                : "Задачу створено!");
            resetForm();
            loadTasks();
            onTaskAdded();
        } catch (error) {
            alert("Помилка при збереженні.");
        }
    };

    const handleDelete = async (id) => {
        if (window.confirm("Ви впевнені, що хочете видалити цю задачу?")) {
            await deleteTaskFromApi(id);
            loadTasks();
            onTaskAdded();
            if (task.Id === id || task.id === id) resetForm();
        }
    };

    const resetForm = () => {
        setTask({
            title: "",
            groupName: "",
            description: "### Умова задачі\n",
            initialCode: "using System;\nusing System.Collections.Generic;\n\npublic class Solution {\n    public int Execute(List<int> numbers) {\n        return 0;\n    }\n}",
            testCases: []
        });
    };

    const groupedTasks = tasks.reduce((acc, t) => {
        const gName = t.GroupName || t.groupName || "Загальні завдання";
        if (!acc[gName]) acc[gName] = [];
        acc[gName].push(t);
        return acc;
    }, {});

    const displayTests = task.testCases || task.TestCases || [];

    return (
        <div style={{ display: "flex", flexDirection: "column", height: "calc(100vh - 70px)", background: "#121212" }}>

            {/* ВЕРХНЯ ПАНЕЛЬ ПЕРЕМИКАННЯ (TABS) */}
            <div style={styles.tabBar}>
                <button
                    onClick={() => setView("tasks")}
                    style={{ ...styles.tabBtn, borderBottom: view === "tasks" ? "3px solid #4db8ff" : "3px solid transparent", color: view === "tasks" ? "#4db8ff" : "#888" }}
                >
                    🔧 Керування задачами
                </button>
                <button
                    onClick={() => setView("journal")}
                    style={{ ...styles.tabBtn, borderBottom: view === "journal" ? "3px solid #4db8ff" : "3px solid transparent", color: view === "journal" ? "#4db8ff" : "#888" }}
                >
                    📊 Журнал оцінок
                </button>
                <button
                    onClick={() => setView("groups")}
                    style={{ ...styles.tabBtn, borderBottom: view === "groups" ? "3px solid #8ce08c" : "3px solid transparent", color: view === "groups" ? "#8ce08c" : "#888" }}
                >
                    👥 Мої Групи
                </button>
            </div>

            {/* 1. ВКЛАДКА ЗАДАЧ */}
            {view === "tasks" && (
                <div style={{ display: "flex", flex: 1, overflow: "hidden", width: "100%" }}>
                    <div style={styles.sidebar}>
                        <h3 style={{ color: "#888", fontSize: "12px", letterSpacing: "1px" }}>СПИСОК ЗАДАЧ</h3>
                        <button
                            onClick={resetForm}
                            style={styles.newBtn}
                            onMouseOver={(e) => handleBtnHover(e, "#005a9e")}
                            onMouseOut={(e) => handleBtnOut(e, "#007acc")}
                        >
                            + Створити нову
                        </button>
                        <div style={{ overflowY: "auto" }}>
                            {Object.entries(groupedTasks).map(([groupName, groupTasks]) => (
                                <div key={groupName} style={{ marginBottom: "10px" }}>

                                    <div
                                        onClick={() =>
                                            setExpandedFolders({
                                                ...expandedFolders,
                                                [groupName]: !expandedFolders[groupName]
                                            })
                                        }
                                        style={{
                                            background: "#2a2a2a",
                                            padding: "8px",
                                            cursor: "pointer",
                                            borderRadius: "4px",
                                            fontSize: "12px",
                                            fontWeight: "bold",
                                            display: "flex",
                                            alignItems: "center",
                                            gap: "8px"
                                        }}
                                    >
                                        <span>
                                            {expandedFolders[groupName] ? "📂" : "📁"}
                                        </span>

                                        {groupName}
                                    </div>

                                    {expandedFolders[groupName] && (
                                        <div style={{ paddingLeft: "15px", marginTop: "5px" }}>
                                            {groupTasks.map(t => (
                                                <div
                                                    key={t.Id || t.id}
                                                    style={{
                                                        ...styles.taskItem,
                                                        borderLeft:
                                                            task.Id === (t.Id || t.id)
                                                                ? "4px solid #4db8ff"
                                                                : "none",
                                                        marginBottom: "5px"
                                                    }}
                                                >
                                                    <span
                                                        onClick={() => handleEditClick(t)}
                                                        style={{
                                                            ...styles.taskTitle,
                                                            cursor: "pointer",
                                                            flex: 1
                                                        }}
                                                    >
                                                        {t.Title || t.title}
                                                    </span>

                                                    <div style={styles.actions}>
                                                        <button
                                                            onClick={() => handleDelete(t.Id || t.id)}
                                                            style={styles.iconBtnDel}
                                                        >
                                                            ✕
                                                        </button>
                                                    </div>
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            ))}
                        </div>
                    </div>

                    <div style={styles.container}>
                        <div style={styles.content}>
                            <h2 style={{ color: "#4db8ff", marginTop: 0 }}>
                                {(task.Id || task.id) ? "📝 Редагування" : "⚙ Нова задача"}
                            </h2>
                            <div style={styles.grid}>
                                <div style={styles.card}>
                                    <h4 style={styles.cardLabel}>Основна інформація</h4>
                                    <input placeholder="Назва" style={styles.input} value={task.title || task.Title || ""} onChange={e => setTask({ ...task, title: e.target.value, Title: e.target.value })} />

                                    {/* Поле для групи */}
                                    <input
                                        placeholder="Назва"
                                        style={styles.input}
                                        value={task.title || task.Title || ""}
                                        onChange={e => setTask({ ...task, title: e.target.value, Title: e.target.value })}
                                    />

                                    {/* 2. Група з випадаючим списком */}
                                    <input
                                        list="group-options"
                                        placeholder="Призначити групі (або нова)"
                                        style={styles.input}
                                        value={task.groupName || task.GroupName || ""}
                                        onChange={e => setTask({ ...task, groupName: e.target.value, GroupName: e.target.value })}
                                    />
                                    <datalist id="group-options">
                                        {groups.map(g => (
                                            <option key={g.Id || g.id} value={g.Name || g.name} />
                                        ))}
                                    </datalist>

                                    <textarea placeholder="Опис (Markdown)" style={styles.textarea} value={task.description || task.Description || ""} onChange={e => setTask({ ...task, description: e.target.value, Description: e.target.value })} />
                                </div>
                                <div style={styles.card}>
                                    <h4 style={styles.cardLabel}>Тест-кейси</h4>
                                    <div style={styles.testInputGroup}>
                                        <input placeholder="In (5|6)" value={testIn} style={styles.input} onChange={e => setTestIn(e.target.value)} />
                                        <input placeholder="Out" value={testOut} style={styles.input} onChange={e => setTestOut(e.target.value)} />
                                        <button onClick={() => {
                                            const currentTests = task.testCases || task.TestCases || [];
                                            const newTests = [...currentTests, { inputs: testIn.split("|"), expectedOutput: testOut }];
                                            setTask({ ...task, testCases: newTests, TestCases: newTests });
                                            setTestIn(""); setTestOut("");
                                        }} style={styles.addBtn}>+</button>
                                    </div>
                                    <div style={styles.testList}>
                                        {displayTests.map((tc, i) => (
                                            <div key={i} style={styles.testRow}>
                                                <span>{(tc.inputs || tc.Inputs || []).join(" | ")} → {tc.expectedOutput || tc.ExpectedOutput}</span>
                                                <button onClick={() => {
                                                    const filtered = displayTests.filter((_, idx) => idx !== i);
                                                    setTask({ ...task, testCases: filtered, TestCases: filtered });
                                                }} style={styles.delBtn}>✕</button>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            </div>
                            <div style={styles.card}>
                                <h4 style={styles.cardLabel}>Шаблон коду</h4>
                                <Editor height="220px" defaultLanguage="csharp" theme="vs-dark" value={task.initialCode || task.InitialCode || ""} onChange={(v) => setTask({ ...task, initialCode: v, InitialCode: v })} />
                            </div>
                            <button
                                onClick={handleSave}
                                style={styles.saveBtn}
                                onMouseOver={(e) => handleBtnHover(e, "#218838")}
                                onMouseOut={(e) => handleBtnOut(e, "#28a745")}
                            >
                                {task.Id ? "💾 Оновити задачу" : "🚀 Опублікувати задачу"}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {/* 2. ВКЛАДКА ЖУРНАЛУ */}
            {view === "journal" && (
                <div style={{ flex: 1, padding: "30px", overflowY: "auto" }}>
                    <div style={styles.card}>
                        <h2 style={{ color: "#4db8ff", marginTop: 0 }}>📊 Журнал успішності</h2>

                        {/* Панель фільтрів */}
                        <div style={styles.filterBar}>
                            <input
                                placeholder="Пошук (студент або задача)..."
                                style={{ ...styles.input, marginBottom: 0, flex: 2 }}
                                onChange={e => setSearch(e.target.value)}
                            />
                            <select style={{ ...styles.input, marginBottom: 0, flex: 1 }} onChange={e => setGroupFilter(e.target.value)}>
                                <option value="">Усі групи</option>
                                {groups.map(g => <option key={g.Id || g.id} value={g.Name || g.name}>{g.Name || g.name}</option>)}
                            </select>
                            <select style={{ ...styles.input, marginBottom: 0, flex: 1 }} onChange={e => setSortBy(e.target.value)}>
                                <option value="date">Спочатку нові</option>
                                <option value="score">За оцінкою</option>
                                <option value="student">За ім'ям</option>
                            </select>
                        </div>

                        <table style={styles.table}>
                            <thead>
                                <tr style={styles.tableHead}>
                                    <th style={styles.th}>Студент</th>
                                    <th style={styles.th}>Група</th>
                                    <th style={styles.th}>Завдання</th>
                                    <th style={styles.th}>Оцінка</th>
                                    <th style={styles.th}>Дата</th>
                                </tr>
                            </thead>
                            <tbody>
                                {results.length === 0 && <tr><td colSpan="5" style={{ padding: "20px", textAlign: "center", color: "#555" }}>Даних не знайдено</td></tr>}
                                {results.map((r, i) => (
                                    <tr key={i} style={styles.tableRow}>
                                        <td style={styles.td}>{r.StudentName || r.studentName}</td>
                                        <td style={{ ...styles.td, color: "#888" }}>{r.GroupName || r.groupName || "Без групи"}</td>
                                        <td style={styles.td}>{r.TaskTitle || r.taskTitle}</td>
                                        <td style={{ ...styles.td, color: (r.Score ?? r.score) === 100 ? "#8ce08c" : "#e08c8c", fontWeight: "bold" }}>
                                            {r.Score ?? r.score}%
                                        </td>
                                        <td style={{ ...styles.td, color: "#666" }}>
                                            {new Date(r.SubmittedAt || r.submittedAt).toLocaleString()}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}

            {/* 3. ВКЛАДКА ГРУП */}
            {view === "groups" && (
                <div style={{ flex: 1, padding: "30px", overflowY: "auto" }}>
                    <h2 style={{ color: "#8ce08c" }}>Управління групами</h2>

                    <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))", gap: "20px" }}>
                        {groups.map(g => (
                            <div key={g.Id || g.id} style={styles.card}>
                                <h3 style={{ margin: "0 0 10px 0", color: "#4db8ff" }}>{g.Name || g.name}</h3>

                                <div style={{ background: "#252526", padding: "10px", borderRadius: "5px", marginBottom: "10px" }}>
                                    <span style={{ fontSize: "12px", color: "#888" }}>КОД ПРИЄДНАННЯ:</span>
                                    <div style={{ fontSize: "20px", fontWeight: "bold", color: "#8ce08c", letterSpacing: "2px" }}>
                                        {g.InviteCode || g.inviteCode}
                                    </div>
                                </div>

                                <div style={{ display: "flex", gap: "5px", marginBottom: "10px" }}>
                                    <button
                                        onClick={() => {
                                            navigator.clipboard.writeText(g.InviteCode || g.inviteCode);
                                            alert("Код скопійовано!");
                                        }}
                                        style={{ flex: 1, background: "#333", color: "#ccc", border: "1px solid #444", padding: "8px", cursor: "pointer", borderRadius: "4px", fontSize: "12px" }}
                                    >
                                        📋 Копіювати
                                    </button>

                                    <button
                                        onClick={() => viewGroupStudents(g.Id || g.id)}
                                        style={{ flex: 1, background: "#007acc", color: "white", border: "none", padding: "8px", cursor: "pointer", borderRadius: "4px", fontSize: "12px" }}
                                    >
                                        👥 Студенти ({g.StudentsCount || g.studentsCount || 0})
                                    </button>
                                </div>
                            </div>
                        ))}
                    </div>

                    {/* СЕКЦІЯ ПЕРЕГЛЯДУ СТУДЕНТІВ */}
                    {selectedGroupStudents && (
                        <div style={{ marginTop: "30px", background: "#1e1e1e", padding: "20px", borderRadius: "10px", border: "1px solid #4db8ff" }}>
                            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "15px" }}>
                                <h3 style={{ margin: 0 }}>Учасники групи:</h3>
                                <button
                                    onClick={() => setSelectedGroupStudents(null)}
                                    style={{ background: "#444", color: "white", border: "none", padding: "5px 15px", borderRadius: "4px", cursor: "pointer" }}
                                >
                                    Закрити
                                </button>
                            </div>

                            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: "10px" }}>
                                {selectedGroupStudents.list.length === 0 ? (
                                    <p style={{ color: "#888" }}>У цій групі ще немає студентів.</p>
                                ) : (
                                    selectedGroupStudents.list.map(s => (
                                        <div key={s.Id || s.id} style={{ background: "#252526", padding: "10px", borderRadius: "5px", border: "1px solid #333" }}>
                                            <div style={{ fontWeight: "bold" }}>{s.FullName || s.fullName}</div>
                                            <div style={{ fontSize: "12px", color: "#888" }}>{s.Email || s.email}</div>
                                        </div>
                                    ))
                                )}
                            </div>
                        </div>
                    )}
                </div>
            )}

        </div>
    );
};

const styles = {
    // Головна обгортка для всього компонента
    mainWrapper: {
        display: "flex",
        flexDirection: "column",
        height: "calc(100vh - 70px)",
        background: "#121212",
        overflow: "hidden"
    },
    // Верхня панель перемикання (Задачі / Журнал / Групи)
    tabBar: {
        display: "flex",
        background: "#1a1a1a",
        padding: "0 20px",
        gap: "10px",
        borderBottom: "1px solid #333",
        flexShrink: 0
    },
    tab: { padding: "15px", background: "none", border: "none", color: "#888", cursor: "pointer" },
    activeTab: { padding: "15px", background: "none", border: "none", color: "#4db8ff", borderBottom: "2px solid #4db8ff", cursor: "pointer" },

    // Лівий сайдбар (Список задач)
    sidebar: {
        width: "320px",
        minWidth: "320px",
        background: "#181818",
        borderRight: "1px solid #333",
        padding: "20px",
        overflowY: "auto",
        display: "flex",
        flexDirection: "column"
    },
    container: {
        flex: 1,
        padding: "30px",
        overflowY: "auto",
        background: "#121212",
        display: "flex",
        flexDirection: "column"
    },

    tabBtn: {
        background: "none",
        border: "none",
        padding: "15px 20px",
        cursor: "pointer",
        fontWeight: "bold",
        fontSize: "14px",
        color: "#888",
        transition: "all 0.3s ease",
        outline: "none",
        display: "flex",
        alignItems: "center",
        gap: "8px"
    },


    content: { maxWidth: "1000px", width: "100%", margin: "0 auto" },

    // Картки та поля
    grid: { display: "grid", gridTemplateColumns: "1fr 1fr", gap: "20px", marginBottom: "20px" },
    card: {
        background: "#1e1e1e",
        padding: "20px",
        borderRadius: "12px",
        border: "1px solid #333",
        boxShadow: "0 10px 30px rgba(0,0,0,0.5)"
    },
    cardLabel: { margin: "0 0 10px 0", color: "#666", fontSize: "12px", textTransform: "uppercase" },
    input: { width: "100%", padding: "12px", marginBottom: "15px", background: "#2d2d2d", color: "white", border: "1px solid #444", borderRadius: "5px", boxSizing: "border-box" },
    textarea: { width: "100%", height: "150px", background: "#2d2d2d", color: "white", border: "1px solid #444", borderRadius: "5px", resize: "none", boxSizing: "border-box" },

    // Елементи списку задач
    newBtn: { width: "100%", padding: "12px", background: "linear-gradient(135deg, #007acc 0%, #005a9e 100%)", color: "white", border: "none", borderRadius: "5px", cursor: "pointer", marginBottom: "20px", fontWeight: "bold", transition: "all 0.3s ease", boxShadow: "0 4px 6px rgba(0,0,0,0.2)" },
    taskList: { display: "flex", flexDirection: "column", gap: "10px" },
    taskItem: { background: "#222", padding: "12px", borderRadius: "6px", display: "flex", justifyContent: "space-between", alignItems: "center", border: "1px solid #333" },
    taskTitle: { fontSize: "14px", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis", maxWidth: "160px" },

    // Кнопки
    actions: { display: "flex", gap: "5px" },
    iconBtn: {
        background: "#2d2d2d",
        border: "1px solid #444",
        color: "#ccc",
        cursor: "pointer",
        padding: "6px 10px",
        borderRadius: "4px",
        transition: "all 0.2s"
    },
    iconBtnDel: { background: "#422", border: "none", color: "#f44", cursor: "pointer", padding: "6px 10px", borderRadius: "4px" },
    testInputGroup: { display: "flex", gap: "10px", marginBottom: "10px" },
    addBtn: {
        background: "#007acc",
        color: "white",
        border: "none",
        padding: "0 20px",
        borderRadius: "5px",
        cursor: "pointer",
        transition: "background 0.2s"
    },
    testList: { maxHeight: "150px", overflowY: "auto", background: "#1a1a1a", padding: "10px", borderRadius: "5px" },
    testRow: { display: "flex", justifyContent: "space-between", fontSize: "13px", padding: "8px 0", borderBottom: "1px solid #222" },
    delBtn: { background: "none", border: "none", color: "#f44", cursor: "pointer" },
    saveBtn: {
        width: "100%",
        padding: "15px",
        background: "#28a745",
        color: "white",
        border: "none",
        borderRadius: "8px",
        fontWeight: "bold",
        cursor: "pointer",
        marginTop: "10px",
        fontSize: "16px",
        transition: "background 0.3s ease, transform 0.1s active",
        boxShadow: "0 4px 10px rgba(40, 167, 69, 0.2)"
    },

    tabIcon: {
        fontSize: "16px",
        opacity: 0.7
    },

    // Таблиця журналу
    table: { width: "100%", borderCollapse: "collapse" },
    tableHead: { borderBottom: "2px solid #333", textAlign: "left" },
    th: { padding: "12px", color: "#888", fontSize: "13px" },
    tableRow: { borderBottom: "1px solid #222" },
    td: { padding: "12px", fontSize: "14px" }
};

export default AdminPanel;