import React, { useState, useEffect } from 'react';
import Editor from "@monaco-editor/react";
import { saveTaskToApi, fetchTasks, fetchTaskById, deleteTaskFromApi } from '../api';

const AdminPanel = ({ onTaskAdded }) => {
    const [tasks, setTasks] = useState([]);

    const [task, setTask] = useState({
        title: "",
        description: "### Умова задачі\n",
        initialCode: "using System;\nusing System.Collections.Generic;\n\npublic class Solution {\n    public int Execute(List<int> numbers) {\n        return 0;\n    }\n}",
        testCases: []
    });

    const [testIn, setTestIn] = useState("");
    const [testOut, setTestOut] = useState("");

    useEffect(() => {
        loadTasks();
    }, []);

    const loadTasks = async () => {
        const data = await fetchTasks();
        setTasks(data || []);
    };

    const handleEditClick = async (taskSummary) => {
        try {
            const id = taskSummary.Id || taskSummary.id;
            const fullTask = await fetchTaskById(id);

            setTask({
                ...fullTask,
                Id: id,
                title: fullTask.Title || fullTask.title || "",
                description: fullTask.Description || fullTask.description || "",
                initialCode: fullTask.InitialCode || fullTask.initialCode || "",
                testCases: fullTask.TestCases || fullTask.testCases || []
            });
        } catch (error) {
            console.error("Помилка завантаження задачі:", error);
            alert("Не вдалося завантажити повні дані задачі.");
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
            await saveTaskToApi(task);
            alert((task.Id || task.id) ? "Задачу оновлено!" : "Задачу створено!");
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
            description: "### Умова задачі\n",
            initialCode: "using System;\nusing System.Collections.Generic;\n\npublic class Solution {\n    public int Execute(List<int> numbers) {\n        return 0;\n    }\n}",
            testCases: []
        });
    };

    const displayTests = task.testCases || task.TestCases || [];

    return (
        <div style={styles.mainWrapper}>
            {/* Ліва панель: Список існуючих задач */}
            <div style={styles.sidebar}>
                <h3 style={{ color: "#888", fontSize: "14px" }}>КЕРУВАННЯ ЗАДАЧАМИ</h3>
                <button onClick={resetForm} style={styles.newBtn}>+ Створити нову</button>
                <div style={styles.taskList}>
                    {tasks.map(t => (
                        <div key={t.Id || t.id} style={{
                            ...styles.taskItem,
                            borderLeft: (task.Id === t.Id || task.id === t.id) ? "4px solid #4db8ff" : "none"
                        }}>
                            <span style={styles.taskTitle}>{t.Title || t.title}</span>
                            <div style={styles.actions}>
                                <button onClick={() => handleEditClick(t)} style={styles.iconBtn}>✎</button>
                                <button onClick={() => handleDelete(t.Id || t.id)} style={styles.iconBtnDel}>✕</button>
                            </div>
                        </div>
                    ))}
                </div>
            </div>

            {/* Права панель: Форма редагування */}
            <div style={styles.container}>
                <div style={styles.content}>
                    <h2 style={{ color: "#4db8ff" }}>
                        {(task.Id || task.id) ? "📝 Редагування задачі" : "⚙ Налаштування нової задачі"}
                    </h2>
                    <div style={styles.grid}>
                        <div style={styles.card}>
                            <h4>Основна інформація</h4>
                            <input
                                placeholder="Назва"
                                style={styles.input}
                                value={task.title || task.Title || ""}
                                onChange={e => setTask({ ...task, title: e.target.value, Title: e.target.value })}
                            />
                            <textarea
                                placeholder="Опис (Markdown)"
                                style={styles.textarea}
                                value={task.description || task.Description || ""}
                                onChange={e => setTask({ ...task, description: e.target.value, Description: e.target.value })}
                            />
                        </div>
                        <div style={styles.card}>
                            <h4>Тест-кейси</h4>
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
                        <h4>Стартовий шаблон коду</h4>
                        <Editor
                            height="250px"
                            defaultLanguage="csharp"
                            theme="vs-dark"
                            value={task.initialCode || task.InitialCode || ""}
                            onChange={(v) => setTask({ ...task, initialCode: v, InitialCode: v })}
                        />
                    </div>
                    <button onClick={handleSave} style={styles.saveBtn}>
                        {(task.Id || task.id) ? "💾 Оновити задачу" : "🚀 Опублікувати задачу"}
                    </button>
                </div>
            </div>
        </div>
    );
};

const styles = {
    mainWrapper: { display: "flex", height: "calc(100vh - 70px)", background: "#121212" },
    sidebar: { width: "300px", background: "#181818", borderRight: "1px solid #333", padding: "20px", overflowY: "auto" },
    newBtn: { width: "100%", padding: "10px", background: "#444", color: "white", border: "none", borderRadius: "5px", cursor: "pointer", marginBottom: "20px" },
    taskList: { display: "flex", flexDirection: "column", gap: "10px" },
    taskItem: { background: "#222", padding: "10px", borderRadius: "5px", display: "flex", justifyContent: "space-between", alignItems: "center" },
    taskTitle: { fontSize: "14px", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis", maxWidth: "180px" },
    actions: { display: "flex", gap: "5px" },
    iconBtn: { background: "#333", border: "none", color: "white", cursor: "pointer", padding: "5px 8px", borderRadius: "3px" },
    iconBtnDel: { background: "#422", border: "none", color: "#f44", cursor: "pointer", padding: "5px 8px", borderRadius: "3px" },

    container: { flex: 1, padding: "20px", overflowY: "auto" },
    content: { maxWidth: "1100px", margin: "0 auto" },
    grid: { display: "grid", gridTemplateColumns: "1fr 1fr", gap: "20px", marginBottom: "20px" },
    card: { background: "#1e1e1e", padding: "15px", borderRadius: "10px", border: "1px solid #333" },
    input: { width: "100%", padding: "10px", marginBottom: "10px", background: "#2d2d2d", color: "white", border: "1px solid #444", borderRadius: "5px", boxSizing: "border-box" },
    textarea: { width: "100%", height: "150px", background: "#2d2d2d", color: "white", border: "1px solid #444", borderRadius: "5px", resize: "none" },
    testInputGroup: { display: "flex", gap: "10px" },
    addBtn: { background: "#007acc", color: "white", border: "none", padding: "0 15px", borderRadius: "5px", cursor: "pointer" },
    testList: { maxHeight: "150px", overflowY: "auto", background: "#1a1a1a", padding: "10px", borderRadius: "5px" },
    testRow: { display: "flex", justifyContent: "space-between", fontSize: "13px", padding: "5px 0", borderBottom: "1px solid #333" },
    delBtn: { background: "none", border: "none", color: "#f44", cursor: "pointer" },
    saveBtn: { width: "100%", padding: "15px", background: "#28a745", color: "white", border: "none", borderRadius: "8px", fontWeight: "bold", cursor: "pointer", marginTop: "10px" }
};

export default AdminPanel;