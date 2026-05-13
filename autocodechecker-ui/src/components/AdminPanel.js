import React, { useState, useEffect } from 'react';
import Editor from "@monaco-editor/react";
import {
    fetchTasks, fetchTaskById, saveTaskToApi, deleteTaskFromApi, fetchTeacherResults, fetchTeacherGroups, fetchGroupStudents,
    updateGroup, deleteGroup, removeStudentFromGroup } from '../api';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import html2pdf from 'html2pdf.js';

const AdminPanel = ({ onTaskAdded }) => {
    const [tasks, setTasks] = useState([]);
    const [results, setResults] = useState([]);
    const [view, setView] = useState("tasks");
    const [groups, setGroups] = useState([]);
    const [search, setSearch] = useState("");
    const [groupFilter, setGroupFilter] = useState("");
    const [sortBy, setSortBy] = useState("date");
    const [selectedGroupStudents, setSelectedGroupStudents] = useState(null);
    const [selectedCode, setSelectedCode] = useState(null);

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

            let formattedDeadline = "";
            const rawDate = fullTask.Deadline || fullTask.deadline;

            if (rawDate) {
                const date = new Date(rawDate);

                const year = date.getFullYear();
                const month = String(date.getMonth() + 1).padStart(2, '0');
                const day = String(date.getDate()).padStart(2, '0');
                const hours = String(date.getHours()).padStart(2, '0');
                const minutes = String(date.getMinutes()).padStart(2, '0');

                formattedDeadline = `${year}-${month}-${day}T${hours}:${minutes}`;
            }

            setTask({
                ...fullTask,
                id: id,
                title: fullTask.Title || "",
                groupName: fullTask.GroupName || "",
                deadline: formattedDeadline,
                maxPoints: fullTask.MaxPoints || 100,
                description: fullTask.Description || "",
                initialCode: fullTask.InitialCode || "",
                testCases: fullTask.TestCases || []
            });
        } catch (error) {
            alert("Помилка завантаження");
        }
    };

    const handleSave = async () => {
        const titleValue = task.title || task.Title;
        const currentTests = task.testCases || task.TestCases || [];
        const groupNameValue = task.groupName || task.GroupName;
        const deadlineValue = task.deadline || task.Deadline;

        if (!titleValue || currentTests.length === 0) {
            alert("Заповніть назву та додайте хоча б один тест!");
            return;
        }

        try {
            const payload = {
                Title: titleValue,
                Description: task.description || task.Description || "",
                InitialCode: task.initialCode || task.InitialCode || "",
                MaxPoints: parseInt(task.maxPoints) || 100,
                TestCases: currentTests.map(tc => ({
                    Inputs: tc.inputs || tc.Inputs,
                    ExpectedOutput: tc.expectedOutput || tc.ExpectedOutput
                })),
                GroupName: groupNameValue && groupNameValue.trim() !== "" ? groupNameValue : null,
                Deadline: deadlineValue && deadlineValue.trim() !== "" ? new Date(deadlineValue).toISOString() : null
            };

            const dataForApi = { ...payload, Id: task.id || task.Id };

            await saveTaskToApi(dataForApi);

            alert((task.id || task.Id) ? "Задачу оновлено! ✅" : "Задачу створено! 🚀");

            resetForm();
            loadTasks();
            loadGroups();
            if (onTaskAdded) onTaskAdded();

        } catch (error) {
            console.error("Save error details:", error);
            const errorMsg = error.message || "Помилка при збереженні (400 Bad Request)";
            alert(errorMsg);
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

    const exportToPDF = () => {
        const filteredResults = groupFilter
            ? results.filter(r => (r.GroupName || r.groupName) === groupFilter)
            : results;

        if (filteredResults.length === 0) return alert("Дані відсутні");

        const reportElement = document.createElement('div');
        reportElement.innerHTML = `
            <div style="padding: 20px; font-family: sans-serif;">
                <h2>Звіт успішності: ${groupFilter || "Загальний"}</h2>
                <table style="width: 100%; border-collapse: collapse;">
                    <thead>
                        <tr style="background: #eee;">
                            <th style="border:1px solid #ddd; padding:8px;">Студент</th>
                            <th style="border:1px solid #ddd; padding:8px;">Завдання</th>
                            <th style="border:1px solid #ddd; padding:8px;">Оцінка (%)</th>
                            <th style="border:1px solid #ddd; padding:8px;">Бал (/${filteredResults[0]?.MaxPoints})</th>
                            <th style="border:1px solid #ddd; padding:8px;">Статус</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${filteredResults.map(r => `
                            <tr>
                                <td style="border:1px solid #ddd; padding:8px;">${r.StudentName}</td>
                                <td style="border:1px solid #ddd; padding:8px;">${r.TaskTitle}</td>
                                <td style="border:1px solid #ddd; padding:8px; text-align:center;">${r.Score}%</td>
                                <td style="border:1px solid #ddd; padding:8px; text-align:center; font-weight:bold;">
                                    ${calculatePoints(r.Score, r.MaxPoints)}
                                </td>
                                <td style="border:1px solid #ddd; padding:8px; text-align:center; color: ${r.IsLate ? 'red' : 'green'};">
                                    ${r.IsLate ? 'ПІЗНО' : 'ВЧАСНО'}
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
        const opt = { margin: 10, filename: 'report.pdf', image: { type: 'jpeg', quality: 0.98 }, html2canvas: { scale: 2 }, jsPDF: { unit: 'mm', format: 'a4' } };
        html2pdf().from(reportElement).set(opt).save();
    };

    const calculatePoints = (percent, max) => {
        const p = percent ?? 0;
        const m = max ?? 100;
        return Math.round((p * m) / 100 * 10) / 10;
    };

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
                                    
                                    {/* Назва */}
                                    <input
                                        placeholder="Назва"
                                        style={styles.input}
                                        value={task.title}
                                        onChange={e => setTask({ ...task, title: e.target.value })}
                                    />

                                    {/* Група */}
                                    <input
                                        list="group-options"
                                        placeholder="Призначити групі (або нова)"
                                        style={styles.input}
                                        value={task.groupName}
                                        onChange={e => setTask({ ...task, groupName: e.target.value })}
                                    />
                                    <datalist id="group-options">
                                        {groups.map(g => (
                                            <option key={g.Id || g.id} value={g.Name || g.name} />
                                        ))}
                                    </datalist>

                                    {/* Опис */}
                                    <textarea
                                        placeholder="Опис (Markdown)"
                                        style={styles.textarea}
                                        value={task.description}
                                        onChange={e => setTask({ ...task, description: e.target.value })}
                                    />

                                    {/* Дедлайн */}
                                    <h4 style={styles.cardLabel}>Дедлайн (опційно)</h4>
                                    <input
                                        type="datetime-local"
                                        style={styles.input}
                                        value={task.deadline || ""}
                                        onChange={e => setTask({ ...task, deadline: e.target.value })}
                                    />
                                    <input
                                        type="text"
                                        placeholder="Макс. балів"
                                        style={styles.input}
                                        value={task.maxPoints || 100}
                                        onChange={e => {
                                            const val = e.target.value.replace(/\D/g, '');
                                            setTask({ ...task, maxPoints: val });
                                        }}
                                    />

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
                        <button onClick={exportToPDF} style={{ ...styles.saveBtn, width: "auto", padding: "10px 20px" }}>
                            📄 Експорт у PDF
                        </button>
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
                                    <th style={styles.th}>Завдання</th>
                                    <th style={styles.th}>Оцінка (%)</th>
                                    <th style={styles.th}>Бал</th>
                                    <th style={styles.th}>Статус</th>
                                    <th style={styles.th}>Дія</th>
                                </tr>
                            </thead>
                            <tbody>
                                {results.length === 0 && <tr><td colSpan="5" style={{ padding: "20px", textAlign: "center", color: "#555" }}>Даних не знайдено</td></tr>}
                                {results.map((r, i) => (
                                    <tr key={i} style={styles.tableRow}>
                                        <td style={styles.td}>{r.StudentName}</td>
                                        <td style={styles.td}>{r.TaskTitle}</td>
                                        <td style={styles.td}>{r.Score}%</td>
                                        <td style={{ ...styles.td, fontWeight: "bold", color: "#4db8ff" }}>
                                            {calculatePoints(r.Score, r.MaxPoints)} / {r.MaxPoints}
                                        </td>
                                        <td style={{ ...styles.td, color: r.IsLate ? "#ff4444" : "#8ce08c" }}>
                                            {r.IsLate ? "⚠️ ПІЗНО" : "✅ ВЧАСНО"}
                                        </td>
                                        <td style={styles.td}>
                                            <button
                                                onClick={() => setSelectedCode(r.SubmittedCode)}
                                                style={{ ...styles.iconBtn, width: "auto", padding: "5px 10px" }}
                                            >
                                                👁 Перегляд коду
                                            </button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>
            )}
            {selectedCode && (
                <div style={modalOverlay}>
                    <div style={modalContent}>
                        <div style={{ display: "flex", justifyContent: "space-between", marginBottom: "10px" }}>
                            <h3>Код студента</h3>
                            <button onClick={() => setSelectedCode(null)} style={{ background: "none", border: "none", color: "white", cursor: "pointer", fontSize: "20px" }}>✕</button>
                        </div>
                        <div style={{ height: "400px", border: "1px solid #444", borderRadius: "5px", overflow: "hidden" }}>
                            <Editor
                                height="100%"
                                defaultLanguage="csharp"
                                theme="vs-dark"
                                value={selectedCode}
                                options={{ readOnly: true, minimap: { enabled: false } }}
                            />
                        </div>
                    </div>
                </div>
            )}

            {/* 3. ВКЛАДКА ГРУП */}
            {view === "groups" && (
                <div style={{ flex: 1, padding: "30px", overflowY: "auto" }}>
                    <h2 style={{ color: "#8ce08c" }}>👥 Управління групами</h2>

                    <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))", gap: "20px" }}>
                        {groups.map(g => (
                            <div key={g.Id || g.id} style={styles.card}>
                                {/* ЗАГОЛОВОК ГРУПИ ТА КНОПКИ КЕРУВАННЯ */}
                                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", marginBottom: "10px" }}>
                                    <h3 style={{ margin: 0, color: "#4db8ff" }}>{g.Name || g.name}</h3>
                                    <div style={{ display: "flex", gap: "5px" }}>
                                        <button
                                            onClick={async () => {
                                                const newName = prompt("Введіть нову назву групи:", g.Name || g.name);
                                                if (newName && newName !== (g.Name || g.name)) {
                                                    await updateGroup(g.Id || g.id, { Name: newName });
                                                    loadGroups();
                                                }
                                            }}
                                            style={{ background: "#333", border: "none", color: "#ccc", cursor: "pointer", padding: "2px 6px", borderRadius: "3px" }}
                                            title="Редагувати назву"
                                        >✎</button>
                                        <button
                                            onClick={async () => {
                                                if (window.confirm(`Видалити групу "${g.Name || g.name}"? Студенти втратять доступ до завдань цієї групи.`)) {
                                                    await deleteGroup(g.Id || g.id);
                                                    loadGroups();
                                                    if (selectedGroupStudents?.groupId === (g.Id || g.id)) setSelectedGroupStudents(null);
                                                }
                                            }}
                                            style={{ background: "#333", border: "none", color: "#f44", cursor: "pointer", padding: "2px 6px", borderRadius: "3px" }}
                                            title="Видалити групу"
                                        >✕</button>
                                    </div>
                                </div>

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

                    {/* СЕКЦІЯ ПЕРЕГЛЯДУ СТУДЕНТІВ З КНОПКОЮ ВИДАЛЕННЯ */}
                    {selectedGroupStudents && (
                        <div style={{ marginTop: "30px", background: "#1e1e1e", padding: "20px", borderRadius: "10px", border: "1px solid #4db8ff" }}>
                            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "15px" }}>
                                <h3 style={{ margin: 0 }}>Учасники групи ({selectedGroupStudents.list.length}):</h3>
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
                                        <div key={s.Id || s.id} style={{ background: "#252526", padding: "10px", borderRadius: "5px", border: "1px solid #333", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                            <div>
                                                <div style={{ fontWeight: "bold" }}>{s.FullName || s.fullName}</div>
                                                <div style={{ fontSize: "11px", color: "#888" }}>{s.Email || s.email}</div>
                                            </div>
                                            <button
                                                onClick={async () => {
                                                    if (window.confirm(`Виключити студента ${s.FullName || s.fullName} з групи?`)) {
                                                        await removeStudentFromGroup(selectedGroupStudents.groupId, s.Id || s.id);
                                                        viewGroupStudents(selectedGroupStudents.groupId);
                                                        loadGroups();
                                                    }
                                                }}
                                                style={{ background: "none", border: "none", color: "#f44", cursor: "pointer", fontSize: "11px" }}
                                            >
                                                Видалити
                                            </button>
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

const modalOverlay = { position: "fixed", top: 0, left: 0, width: "100%", height: "100%", background: "rgba(0,0,0,0.8)", display: "flex", justifyContent: "center", alignItems: "center", zIndex: 1000 };
const modalContent = { background: "#1e1e1e", padding: "20px", borderRadius: "10px", width: "80%", maxWidth: "800px", border: "1px solid #333" };

const styles = {
    mainWrapper: {
        display: "flex",
        flexDirection: "column",
        height: "calc(100vh - 70px)",
        background: "#121212",
        overflow: "hidden"
    },

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

    iconBtnStyle: {
        background: "#333",
        border: "none",
        color: "#aaa",
        cursor: "pointer",
        padding: "4px 8px",
        borderRadius: "4px",
        fontSize: "14px"
    },

    content: { maxWidth: "1000px", width: "100%", margin: "0 auto" },

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

    newBtn: { width: "100%", padding: "12px", background: "linear-gradient(135deg, #007acc 0%, #005a9e 100%)", color: "white", border: "none", borderRadius: "5px", cursor: "pointer", marginBottom: "20px", fontWeight: "bold", transition: "all 0.3s ease", boxShadow: "0 4px 6px rgba(0,0,0,0.2)" },
    taskList: { display: "flex", flexDirection: "column", gap: "10px" },
    taskItem: { background: "#222", padding: "12px", borderRadius: "6px", display: "flex", justifyContent: "space-between", alignItems: "center", border: "1px solid #333" },
    taskTitle: { fontSize: "14px", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis", maxWidth: "160px" },

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

    table: { width: "100%", borderCollapse: "collapse" },
    tableHead: { borderBottom: "2px solid #333", textAlign: "left" },
    th: { padding: "12px", color: "#888", fontSize: "13px" },
    tableRow: { borderBottom: "1px solid #222" },
    td: { padding: "12px", fontSize: "14px" }
};

export default AdminPanel;